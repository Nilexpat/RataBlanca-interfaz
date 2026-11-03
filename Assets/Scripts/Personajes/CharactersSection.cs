using System.Collections;
using RataBlanca.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RataBlanca.Personajes
{
    /// <summary>
    /// Sección "Personajes": carrusel en bucle (frames "Desktop - 1/8/9/10").
    /// Botones &lt; y &gt; (o las flechas del teclado) cambian de personaje:
    /// el actual sale deslizándose y el nuevo entra desde el otro lado.
    /// Después del último vuelve al primero y antes del primero va al último.
    /// Los datos salen de CharacterRoster (Assets/Data/Personajes).
    /// </summary>
    public class CharactersSection : Section
    {
        public CharacterRoster roster;

        [Header("Partes (las arma el constructor)")]
        [Tooltip("Contenedor del personaje; su pivote (abajo al centro) es donde se para.")]
        public RectTransform portraitHolder;
        public Image portrait;
        public CanvasGroup portraitGroup;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public CanvasGroup textGroup;
        public Button previousButton;
        public Button nextButton;

        int _index;
        Coroutine _switch;
        Vector2 _holderBase;
        float _slideOffset;
        bool _ready;
        int _shownVersion = -1;

        void Awake()
        {
            if (portraitHolder != null) _holderBase = portraitHolder.anchoredPosition;
            if (previousButton != null) previousButton.onClick.AddListener(Previous);
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            _ready = true;
            Apply(_index);
        }

        protected override void OnOpened()
        {
            if (!_ready) return;
            if (_switch != null) StopCoroutine(_switch);
            _switch = StartCoroutine(Entrance());
        }

        int Count => roster != null ? roster.characters.Count : 0;

        public void Next() => Switch(+1);
        public void Previous() => Switch(-1);

        void Switch(int direction)
        {
            if (Count == 0) return;
            int target = ((_index + direction) % Count + Count) % Count;   // en bucle
            if (_switch != null) StopCoroutine(_switch);
            _switch = StartCoroutine(SwitchRoutine(target, direction));
        }

        void Update()
        {
            if (roster == null) return;
            if (roster.version != _shownVersion) Apply(_index);

            if (Keyboard.current != null && IsOpen)
            {
                if (Keyboard.current.rightArrowKey.wasPressedThisFrame) Next();
                else if (Keyboard.current.leftArrowKey.wasPressedThisFrame) Previous();
            }

            // Vaivén suave del personaje mientras está quieto
            if (portraitHolder != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * roster.idleBobSpeed * Mathf.PI * 2f) * roster.idleBobHeight;
                portraitHolder.anchoredPosition = _holderBase + new Vector2(_slideOffset, Mathf.Max(0f, bob));
            }
        }

        // ------------------------------------------------------------------
        IEnumerator Entrance()
        {
            Apply(_index);
            SetAlpha(0f);
            _slideOffset = 0f;
            float t = 0f;
            float d = Mathf.Max(0.05f, roster.inDuration);
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / d);
                float k = UITween.EaseOutCubic(t);
                SetAlpha(k);
                float s = Mathf.LerpUnclamped(0.9f, 1f, EaseOutBack(t));
                if (portraitHolder != null) portraitHolder.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _switch = null;
        }

        IEnumerator SwitchRoutine(int target, int direction)
        {
            // Salida
            float startAlpha = portraitGroup != null ? portraitGroup.alpha : 1f;
            float startOffset = _slideOffset;
            float t = 0f;
            float dOut = Mathf.Max(0.01f, roster.outDuration);
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / dOut);
                float k = t * t;
                _slideOffset = Mathf.Lerp(startOffset, -direction * roster.slideDistance, k);
                SetAlpha(Mathf.Lerp(startAlpha, 0f, k));
                yield return null;
            }

            _index = target;
            Apply(_index);

            // Entrada desde el otro lado
            t = 0f;
            float dIn = Mathf.Max(0.01f, roster.inDuration);
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / dIn);
                _slideOffset = Mathf.LerpUnclamped(direction * roster.slideDistance, 0f, EaseOutBack(t));
                SetAlpha(UITween.EaseOutCubic(t));
                yield return null;
            }
            _slideOffset = 0f;
            _switch = null;
        }

        void SetAlpha(float a)
        {
            if (portraitGroup != null) portraitGroup.alpha = a;
            if (textGroup != null) textGroup.alpha = a;
        }

        void Apply(int index)
        {
            if (roster == null) return;
            _shownVersion = roster.version;
            if (Count == 0) return;
            index = Mathf.Clamp(index, 0, Count - 1);
            var c = roster.characters[index];

            if (nameText != null) nameText.text = c.name;
            if (descriptionText != null) descriptionText.text = c.description;
            if (portrait != null)
            {
                portrait.sprite = c.portrait;
                portrait.enabled = c.portrait != null;
                if (c.portrait != null)
                {
                    Vector2 size = c.portrait.rect.size * roster.portraitScale;
                    portrait.rectTransform.sizeDelta = size;
                }
            }
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.4f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
