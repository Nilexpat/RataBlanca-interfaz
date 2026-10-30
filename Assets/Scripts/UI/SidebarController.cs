using System.Collections;
using RataBlanca.Audio;
using UnityEngine;

namespace RataBlanca.UI
{
    /// <summary>
    /// Expande y contrae la barra lateral con animación. Al mismo tiempo
    /// mueve el borde izquierdo del área de contenido: con la barra abierta el
    /// contenido se corre a la derecha para hacerle espacio, y con la barra
    /// cerrada vuelve a quedar centrado en la pantalla (como en los frames
    /// "Desktop - 4" y "Desktop - 7" de Figma).
    /// </summary>
    public class SidebarController : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("La barra lateral (anclada a la izquierda, de alto completo).")]
        public RectTransform sidebar;
        [Tooltip("El área de contenido (estirada a toda la pantalla).")]
        public RectTransform content;
        [Tooltip("Ícono de flecha del botón que abre/cierra la barra.")]
        public RectTransform toggleIcon;

        [Header("Medidas y animación")]
        [Tooltip("Ancho de la barra en Figma: 428 px.")]
        public float expandedWidth = 428f;
        public float duration = 0.4f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public bool startExpanded = true;
        public bool playSounds = true;

        public bool IsExpanded { get; private set; }

        float _amount;            // 0 = cerrada, 1 = abierta
        Coroutine _animation;

        void Start()
        {
            IsExpanded = startExpanded;
            Apply(startExpanded ? 1f : 0f);
        }

        public void Toggle() => SetExpanded(!IsExpanded);
        public void Expand() => SetExpanded(true);
        public void Collapse() => SetExpanded(false);

        public void SetExpanded(bool expanded, bool animate = true)
        {
            if (expanded == IsExpanded && _animation == null) return;
            IsExpanded = expanded;

            if (playSounds && AudioManager.Instance != null)
            {
                if (expanded) AudioManager.Instance.PlayExpand();
                else AudioManager.Instance.PlayCollapse();
            }

            if (_animation != null) StopCoroutine(_animation);
            float target = expanded ? 1f : 0f;
            if (!animate || !isActiveAndEnabled)
            {
                Apply(target);
                return;
            }
            _animation = StartCoroutine(Animate(target));
        }

        IEnumerator Animate(float target)
        {
            float from = _amount;
            // La duración se acorta si la animación anterior quedó a medias.
            float time = duration * Mathf.Abs(target - from);
            yield return UITween.Run(time, k => Apply(Mathf.LerpUnclamped(from, target, k)), curve);
            _animation = null;
        }

        void Apply(float amount)
        {
            _amount = amount;
            if (sidebar != null)
            {
                Vector2 p = sidebar.anchoredPosition;
                p.x = Mathf.Lerp(-expandedWidth, 0f, amount);
                sidebar.anchoredPosition = p;
            }
            if (content != null)
            {
                Vector2 min = content.offsetMin;
                min.x = expandedWidth * amount;
                content.offsetMin = min;
            }
            if (toggleIcon != null)
            {
                // El chevron apunta hacia abajo: -90° = "‹" (cerrar), +90° = "›" (abrir).
                toggleIcon.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(90f, -90f, amount));
            }
        }
    }
}
