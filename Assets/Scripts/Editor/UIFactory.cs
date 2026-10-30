using RataBlanca.Audio;
using RataBlanca.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.EditorTools
{
    /// <summary>
    /// Funciones para crear elementos de uGUI desde código, usando las
    /// medidas de Figma (origen arriba a la izquierda, Y hacia abajo).
    /// </summary>
    public static class UIFactory
    {
        public static TMP_FontAsset DefaultFont;

        // ------------------------------------------------------------------
        // Rect transforms
        // ------------------------------------------------------------------
        public static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Estira el rect sobre todo el padre con márgenes (izq, der, arriba, abajo).</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Posición estilo Figma: x, y desde la esquina superior izquierda del padre.</summary>
        public static RectTransform TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Centrado horizontalmente; y desde arriba.</summary>
        public static RectTransform TopCenter(RectTransform rt, float offsetX, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(offsetX, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Center(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        // ------------------------------------------------------------------
        // Gráficos
        // ------------------------------------------------------------------
        public static Image MakeImage(RectTransform rt, Color color, Sprite sprite = null, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        public static TextMeshProUGUI MakeText(RectTransform rt, string text, TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, bool wrap = false)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            else if (DefaultFont != null) t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static CanvasGroup MakeGroup(GameObject go)
        {
            var g = go.GetComponent<CanvasGroup>();
            return g != null ? g : go.AddComponent<CanvasGroup>();
        }

        // ------------------------------------------------------------------
        // Botones
        // ------------------------------------------------------------------
        /// <summary>
        /// Botón de solo texto (estilo Figma). El objeto raíz recibe los clics
        /// (con una imagen invisible) y el texto es un hijo que se anima.
        /// </summary>
        public static Button TextButton(RectTransform root, string text, TMP_FontAsset font, float size,
            Color normal, Color hover, Color selected, TextAlignmentOptions alignment, float labelPaddingLeft,
            float hoverOffsetX, out HoverText hoverText)
        {
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0);   // invisible, pero recibe el mouse
            hit.raycastTarget = true;

            var labelRt = Stretch(MakeRect("Texto", root), labelPaddingLeft, 0, 0, 0);
            var label = MakeText(labelRt, text, font, size, normal, alignment);

            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            hoverText = root.gameObject.AddComponent<HoverText>();
            hoverText.label = label;
            hoverText.normalColor = normal;
            hoverText.hoverColor = hover;
            hoverText.selectedColor = selected;
            hoverText.hoverOffsetX = hoverOffsetX;

            root.gameObject.AddComponent<UISound>();
            return button;
        }

        // ------------------------------------------------------------------
        // Slider (Configuración)
        // ------------------------------------------------------------------
        public static Slider MakeSlider(RectTransform root, Color track, Color fill, Color handle,
            Sprite rounded, Sprite circle)
        {
            var slider = root.gameObject.AddComponent<Slider>();

            var bg = MakeRect("Fondo", root);
            bg.anchorMin = new Vector2(0f, 0.5f);
            bg.anchorMax = new Vector2(1f, 0.5f);
            bg.pivot = new Vector2(0.5f, 0.5f);
            bg.sizeDelta = new Vector2(0f, 10f);
            bg.anchoredPosition = Vector2.zero;
            var bgImg = MakeImage(bg, track, rounded, true);
            bgImg.pixelsPerUnitMultiplier = 3.2f;

            var fillArea = MakeRect("Relleno", root);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.pivot = new Vector2(0.5f, 0.5f);
            fillArea.sizeDelta = new Vector2(0f, 10f);
            fillArea.anchoredPosition = Vector2.zero;
            var fillRt = MakeRect("Barra", fillArea);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = MakeImage(fillRt, fill, rounded);
            fillImg.pixelsPerUnitMultiplier = 3.2f;

            var handleArea = Stretch(MakeRect("AreaManija", root), 14f, 14f, 0f, 0f);
            var handleRt = MakeRect("Manija", handleArea);
            handleRt.anchorMin = new Vector2(0f, 0.5f);
            handleRt.anchorMax = new Vector2(0f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(28f, 28f);
            var handleImg = MakeImage(handleRt, handle, circle, true);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            var nav = slider.navigation;
            nav.mode = Navigation.Mode.None;
            slider.navigation = nav;

            root.gameObject.AddComponent<UISound>().playClick = false;
            return slider;
        }

        // ------------------------------------------------------------------
        public static void Persist(UnityEngine.Events.UnityEvent evt, UnityEngine.Events.UnityAction action)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(evt, action);
        }

        public static void MarkDirty(Object o) => EditorUtility.SetDirty(o);
    }
}
