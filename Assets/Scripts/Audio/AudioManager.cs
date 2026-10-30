using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace RataBlanca.Audio
{
    /// <summary>Los tres canales de volumen que se controlan desde Configuración.</summary>
    public enum AudioChannel { General = 0, Feedback = 1, Ambiente = 2 }

    /// <summary>
    /// Administra todos los sonidos de la app. Vive en un prefab
    /// (Assets/Prefabs/AudioManager) que está en las dos escenas; solo se
    /// queda una copia y pasa de una escena a otra sin cortar el ambiente.
    ///
    /// Los clips se arrastran en el Inspector del prefab. Si asignas un
    /// AudioMixer con los parámetros expuestos (VolGeneral, VolFeedback,
    /// VolAmbiente) los volúmenes van por el mezclador; si no, se aplican
    /// directamente a las fuentes de audio. Las dos formas funcionan.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sonidos de interfaz (arrastra aquí tus archivos)")]
        [Tooltip("Al pasar el mouse por un botón.")]
        public AudioClip hoverClip;
        [Tooltip("Al hacer clic.")]
        public AudioClip clickClip;
        [Tooltip("Al expandir un bloque o una vista.")]
        public AudioClip expandClip;
        [Tooltip("Al contraer un bloque o una vista.")]
        public AudioClip collapseClip;

        [Header("Sonido ambiente (en bucle)")]
        public AudioClip ambientClip;

        [Header("Volumen propio de cada sonido")]
        [Range(0f, 1f)] public float hoverVolume = 0.5f;
        [Range(0f, 1f)] public float clickVolume = 0.9f;
        [Range(0f, 1f)] public float expandVolume = 0.9f;
        [Range(0f, 1f)] public float collapseVolume = 0.9f;
        [Range(0f, 1f)] public float ambientVolume = 0.6f;

        [Header("Mezclador (opcional)")]
        public AudioMixer mixer;
        public string generalParameter = "VolGeneral";
        public string feedbackParameter = "VolFeedback";
        public string ambientParameter = "VolAmbiente";
        [Tooltip("Nombre del grupo del mezclador para los sonidos de interfaz.")]
        public string feedbackGroupName = "Feedback";
        [Tooltip("Nombre del grupo del mezclador para el ambiente.")]
        public string ambientGroupName = "Ambiente";

        [Header("Volúmenes por defecto (primera vez)")]
        [Range(0f, 1f)] public float defaultGeneral = 0.8f;
        [Range(0f, 1f)] public float defaultFeedback = 0.8f;
        [Range(0f, 1f)] public float defaultAmbient = 0.6f;

        [Tooltip("Tiempo mínimo entre dos sonidos de hover, para que no se amontonen.")]
        public float hoverCooldown = 0.06f;

        AudioSource _fxSource;
        AudioSource _ambientSource;
        readonly float[] _volumes = new float[3];
        bool _useMixer;
        bool _ambientStarted;
        bool _waitingForInteraction;
        float _lastHoverTime = -1f;

        static string PrefsKey(AudioChannel c) => "rb.volumen." + c;

        // ------------------------------------------------------------------
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _fxSource = gameObject.AddComponent<AudioSource>();
            _fxSource.playOnAwake = false;

            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.playOnAwake = false;
            _ambientSource.loop = true;

            _volumes[0] = PlayerPrefs.GetFloat(PrefsKey(AudioChannel.General), defaultGeneral);
            _volumes[1] = PlayerPrefs.GetFloat(PrefsKey(AudioChannel.Feedback), defaultFeedback);
            _volumes[2] = PlayerPrefs.GetFloat(PrefsKey(AudioChannel.Ambiente), defaultAmbient);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // El mezclador solo acepta cambios desde Start (no en Awake).
            _useMixer = mixer != null && mixer.GetFloat(generalParameter, out _);
            if (mixer != null && !_useMixer)
                Debug.LogWarning($"[AudioManager] El mezclador no tiene el parámetro expuesto '{generalParameter}'. Se usan los volúmenes directos.");

            if (_useMixer)
            {
                var fx = mixer.FindMatchingGroups(feedbackGroupName);
                if (fx.Length > 0) _fxSource.outputAudioMixerGroup = fx[0];
                var amb = mixer.FindMatchingGroups(ambientGroupName);
                if (amb.Length > 0) _ambientSource.outputAudioMixerGroup = amb[0];
            }

            ApplyAllVolumes();

            // Los navegadores bloquean el audio hasta la primera interacción.
#if UNITY_WEBGL && !UNITY_EDITOR
            _waitingForInteraction = true;
#else
            StartAmbient();
#endif
        }

        void Update()
        {
            if (!_waitingForInteraction) return;
            bool interacted =
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            if (interacted)
            {
                _waitingForInteraction = false;
                StartAmbient();
            }
        }

        // ------------------------------------------------------------------
        // Reproducción
        // ------------------------------------------------------------------
        public void PlayHover()
        {
            if (Time.unscaledTime - _lastHoverTime < hoverCooldown) return;
            _lastHoverTime = Time.unscaledTime;
            PlayOneShot(hoverClip, hoverVolume);
        }

        public void PlayClick() => PlayOneShot(clickClip, clickVolume);
        public void PlayExpand() => PlayOneShot(expandClip, expandVolume);
        public void PlayCollapse() => PlayOneShot(collapseClip, collapseVolume);

        public void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null || _fxSource == null) return;
            _fxSource.PlayOneShot(clip, volume);
        }

        public void StartAmbient()
        {
            _ambientStarted = true;
            if (ambientClip == null || _ambientSource == null) return;
            if (_ambientSource.isPlaying && _ambientSource.clip == ambientClip) return;
            _ambientSource.clip = ambientClip;
            _ambientSource.Play();
        }

        /// <summary>Cambia el sonido ambiente en tiempo de ejecución.</summary>
        public void SetAmbientClip(AudioClip clip)
        {
            ambientClip = clip;
            if (_ambientStarted) StartAmbient();
        }

        // ------------------------------------------------------------------
        // Volúmenes
        // ------------------------------------------------------------------
        public float GetVolume(AudioChannel channel) => _volumes[(int)channel];

        public void SetVolume(AudioChannel channel, float value, bool save = true)
        {
            value = Mathf.Clamp01(value);
            _volumes[(int)channel] = value;
            if (save)
            {
                PlayerPrefs.SetFloat(PrefsKey(channel), value);
                PlayerPrefs.Save();
            }
            ApplyAllVolumes();
        }

        /// <summary>Lee un volumen guardado sin necesitar el AudioManager (por ejemplo, en el editor).</summary>
        public static float LoadSavedVolume(AudioChannel channel, float fallback = 0.8f)
            => PlayerPrefs.GetFloat(PrefsKey(channel), fallback);

        void ApplyAllVolumes()
        {
            if (_fxSource == null) return;

            if (_useMixer)
            {
                mixer.SetFloat(generalParameter, ToDecibels(_volumes[0]));
                mixer.SetFloat(feedbackParameter, ToDecibels(_volumes[1]));
                mixer.SetFloat(ambientParameter, ToDecibels(_volumes[2]));
                _fxSource.volume = 1f;
                _ambientSource.volume = ambientVolume;
            }
            else
            {
                // Sin mezclador: General = volumen global, los otros por fuente.
                AudioListener.volume = _volumes[0];
                _fxSource.volume = _volumes[1];
                _ambientSource.volume = ambientVolume * _volumes[2];
            }
        }

        /// <summary>Convierte 0..1 a decibeles (0 → -80 dB, 1 → 0 dB).</summary>
        static float ToDecibels(float linear)
            => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
}
