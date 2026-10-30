using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RataBlanca.UI
{
    /// <summary>
    /// Botón de solo texto (como en el diseño de Figma): al pasar el mouse el
    /// texto cambia de color y se desplaza un poco; al estar seleccionado
    /// (la sección abierta) se queda con el color activo.
    /// Va en el objeto del botón; el texto es un hijo, así el área de clic no
    /// se mueve con la animación.
    /// </summary>
    public class HoverText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public TMP_Text label;

        [Header("Colores")]
        public Color normalColor = new Color32(0x6D, 0x48, 0x48, 0xFF);
        public Color hoverColor = new Color32(0x40, 0x12, 0x12, 0xFF);
        public Color selectedColor = new Color32(0x40, 0x12, 0x12, 0xFF);

        [Header("Movimiento")]
        [Tooltip("Desplazamiento horizontal del texto al pasar el mouse o al estar seleccionado.")]
        public float hoverOffsetX = 10f;
        [Tooltip("Escala del texto mientras se mantiene presionado.")]
        public float pressedScale = 0.96f;
        [Tooltip("Qué tan rápido responde la animación.")]
        public float speed = 14f;

        bool _hover;
        bool _pressed;
        bool _selected;
        Vector2 _basePosition;
        float _offset;
        bool _initialized;

        public bool Selected
        {
            get => _selected;
            set => _selected = value;
        }

        void Awake() => Init();

        void Init()
        {
            if (_initialized || label == null) return;
            _basePosition = label.rectTransform.anchoredPosition;
            label.color = normalColor;
            _initialized = true;
        }

        void OnDisable()
        {
            _hover = false;
            _pressed = false;
        }

        void Update()
        {
            if (!_initialized) { Init(); if (!_initialized) return; }

            float k = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
            Color target = _selected ? selectedColor : (_hover ? hoverColor : normalColor);
            label.color = Color.Lerp(label.color, target, k);

            float targetOffset = (_hover || _selected) ? hoverOffsetX : 0f;
            _offset = Mathf.Lerp(_offset, targetOffset, k);
            label.rectTransform.anchoredPosition = _basePosition + new Vector2(_offset, 0f);

            float targetScale = _pressed ? pressedScale : 1f;
            float s = Mathf.Lerp(label.rectTransform.localScale.x, targetScale, k);
            label.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        public void OnPointerEnter(PointerEventData eventData) => _hover = true;
        public void OnPointerExit(PointerEventData eventData) { _hover = false; _pressed = false; }
        public void OnPointerDown(PointerEventData eventData) => _pressed = true;
        public void OnPointerUp(PointerEventData eventData) => _pressed = false;
    }
}
