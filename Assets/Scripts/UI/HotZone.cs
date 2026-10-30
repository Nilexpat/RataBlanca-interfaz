using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Zona interactiva sobre una imagen (por ejemplo el rubí o la tela).
    /// Solo responde donde la imagen no es transparente, así el hueco del
    /// turbante no cuenta como clic. Para eso la textura debe tener activado
    /// "Read/Write" (el constructor de escenas lo configura).
    /// </summary>
    public class HotZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Tooltip("Transparencia mínima que cuenta como parte de la imagen (0-1).")]
        [Range(0f, 1f)] public float alphaThreshold = 0.3f;

        public event Action Clicked;
        public event Action<bool> HoverChanged;

        public bool IsHovered { get; private set; }

        void Awake()
        {
            var image = GetComponent<Image>();
            if (image != null && image.sprite != null && image.sprite.texture != null && image.sprite.texture.isReadable)
                image.alphaHitTestMinimumThreshold = alphaThreshold;
        }

        void OnDisable() => SetHover(false);

        public void OnPointerEnter(PointerEventData eventData) => SetHover(true);
        public void OnPointerExit(PointerEventData eventData) => SetHover(false);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Clicked?.Invoke();
        }

        void SetHover(bool value)
        {
            if (IsHovered == value) return;
            IsHovered = value;
            HoverChanged?.Invoke(value);
        }
    }
}
