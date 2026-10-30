using System;
using System.Collections;
using UnityEngine;

namespace RataBlanca.UI
{
    /// <summary>Las secciones de la barra lateral.</summary>
    public enum SectionId { Mapa, Misterios, Personajes, Objeto, Configuracion }

    /// <summary>
    /// Una pantalla del área de contenido (Mapa, Misterios, etc.).
    /// Se muestra con un desvanecido y un pequeño deslizamiento.
    /// Las secciones con vistas internas (por ejemplo Objeto) heredan de esta
    /// clase y sobrescriben HandleBack() para cerrar primero lo suyo cuando
    /// se presiona Escape o Volver.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class Section : MonoBehaviour
    {
        public SectionId id;

        [Header("Animación")]
        public float duration = 0.3f;
        [Tooltip("Cuánto sube el contenido al aparecer (px).")]
        public float slideDistance = 24f;

        CanvasGroup _group;
        RectTransform _rect;
        Vector2 _basePosition;
        Coroutine _animation;
        bool _initialized;

        public bool IsOpen { get; private set; }

        void Init()
        {
            if (_initialized) return;
            _group = GetComponent<CanvasGroup>();
            _rect = (RectTransform)transform;
            _basePosition = _rect.anchoredPosition;
            _initialized = true;
        }

        /// <summary>Oculta la sección sin animación (al cargar la escena).</summary>
        public void HideImmediate()
        {
            Init();
            IsOpen = false;
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void Open()
        {
            Init();
            IsOpen = true;
            gameObject.SetActive(true);
            _group.interactable = true;
            _group.blocksRaycasts = true;
            Play(1f, null);
            OnOpened();
        }

        public void Close(Action onClosed = null)
        {
            Init();
            if (!gameObject.activeSelf)
            {
                onClosed?.Invoke();
                return;
            }
            IsOpen = false;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            OnClosing();
            Play(0f, () =>
            {
                gameObject.SetActive(false);
                onClosed?.Invoke();
            });
        }

        /// <summary>
        /// Escape / Volver. Devuelve true si la sección lo resolvió por dentro
        /// (por ejemplo, cerrando una vista abierta) y no hay que cerrarla.
        /// </summary>
        public virtual bool HandleBack() => false;

        protected virtual void OnOpened() { }
        protected virtual void OnClosing() { }

        void Play(float target, Action onDone)
        {
            if (_animation != null) StopCoroutine(_animation);
            _animation = StartCoroutine(Animate(target, onDone));
        }

        IEnumerator Animate(float target, Action onDone)
        {
            float fromAlpha = _group.alpha;
            Vector2 hidden = _basePosition + new Vector2(0f, -slideDistance);
            Vector2 fromPos = _rect.anchoredPosition;
            Vector2 toPos = target > 0.5f ? _basePosition : hidden;
            if (target > 0.5f && fromAlpha <= 0.001f) fromPos = hidden;

            yield return UITween.Run(duration, k =>
            {
                _group.alpha = Mathf.Lerp(fromAlpha, target, k);
                _rect.anchoredPosition = Vector2.Lerp(fromPos, toPos, k);
            });
            _animation = null;
            onDone?.Invoke();
        }
    }
}
