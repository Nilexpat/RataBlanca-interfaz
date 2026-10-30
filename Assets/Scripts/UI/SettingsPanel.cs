using RataBlanca.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Pantalla de Configuración: tres sliders (General, Feedback y Ambiente)
    /// conectados al AudioManager. Los valores se guardan en PlayerPrefs.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public Slider generalSlider;
        public Slider feedbackSlider;
        public Slider ambientSlider;

        [Header("Textos con el porcentaje (opcionales)")]
        public TMP_Text generalValue;
        public TMP_Text feedbackValue;
        public TMP_Text ambientValue;

        void Start()
        {
            Bind(generalSlider, generalValue, AudioChannel.General);
            Bind(feedbackSlider, feedbackValue, AudioChannel.Feedback);
            Bind(ambientSlider, ambientValue, AudioChannel.Ambiente);
        }

        void OnEnable()
        {
            // Al volver a abrir la sección se muestran los valores actuales.
            Refresh(generalSlider, generalValue, AudioChannel.General);
            Refresh(feedbackSlider, feedbackValue, AudioChannel.Feedback);
            Refresh(ambientSlider, ambientValue, AudioChannel.Ambiente);
        }

        static float Current(AudioChannel channel) =>
            AudioManager.Instance != null
                ? AudioManager.Instance.GetVolume(channel)
                : AudioManager.LoadSavedVolume(channel);

        static void Refresh(Slider slider, TMP_Text label, AudioChannel channel)
        {
            if (slider == null) return;
            float v = Current(channel);
            slider.SetValueWithoutNotify(v);
            if (label != null) label.text = Mathf.RoundToInt(v * 100f) + " %";
        }

        void Bind(Slider slider, TMP_Text label, AudioChannel channel)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            Refresh(slider, label, channel);
            slider.onValueChanged.AddListener(v =>
            {
                if (label != null) label.text = Mathf.RoundToInt(v * 100f) + " %";
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetVolume(channel, v);
                    // Al mover Feedback se escucha un clic para comparar el nivel.
                    if (channel == AudioChannel.Feedback) AudioManager.Instance.PlayHover();
                }
            });
        }
    }
}
