using System.Collections.Generic;
using RataBlanca.Audio;
using RataBlanca.UI;
using TMPro;
using UnityEngine;

namespace RataBlanca.Mapa
{
    /// <summary>
    /// Sección "Mapa": Rat City / Bio-jaula reconstruido con formas (no es
    /// una imagen). Al abrir la sección la jaula y las zonas aparecen en
    /// secuencia; al pasar el mouse una zona se ilumina y crece un poco; al
    /// hacer clic queda seleccionada y, si tiene descripción, se abre una
    /// ficha en el centro de la jaula. Escape o Volver cierran la ficha.
    /// Todo sale de MapData (Assets/Data/MapaRatCity).
    /// </summary>
    public class MapSection : Section
    {
        [Header("Datos")]
        public MapData data;

        [Header("Partes (las arma el constructor)")]
        [Tooltip("Rect del tamaño del dibujo original (800×800) que se escala para caber.")]
        public RectTransform composition;
        public TMP_FontAsset labelFont;
        public TMP_FontAsset cardTitleFont;
        public TMP_FontAsset cardBodyFont;

        [Header("Ficha de la zona")]
        public Rect infoCard = new Rect(275f, 118f, 262f, 372f);
        public Color cardFill = new Color32(0xE0, 0xE0, 0xE0, 0xF5);
        public Color cardBorder = new Color32(0x57, 0x57, 0x57, 0xFF);
        public float cardDuration = 0.3f;

        class ZoneView
        {
            public RectTransform rect;
            public ShapeGraphic shape;
            public TMP_Text label;
            public float hover;
            public bool hovered;
        }

        readonly List<ZoneView> _zones = new List<ZoneView>();
        ShapeGraphic _cage;
        CanvasGroup _card;
        TMP_Text _cardTitle;
        TMP_Text _cardBody;
        int _selected = -1;
        float _appearT;
        float _cardT;
        int _builtVersion = -1;

        // ------------------------------------------------------------------
        void Awake() => Build();

        protected override void OnOpened()
        {
            _selected = -1;
            _appearT = 0f;
            _cardT = 0f;
            Render(0f);
        }

        public override bool HandleBack()
        {
            if (_selected < 0) return false;
            Select(-1);
            return true;
        }

        // ------------------------------------------------------------------
        void Build()
        {
            if (data == null || composition == null) return;
            _builtVersion = data.version;

            for (int i = composition.childCount - 1; i >= 0; i--)
            {
                var child = composition.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            _zones.Clear();

            composition.sizeDelta = data.size;
            var fit = composition.GetComponent<FitToParent>();
            if (fit != null) fit.designSize = data.size;

            // Jaula
            _cage = NewShape("Jaula", data.cage, ShapeGraphic.Shape.Rectangulo, data.cageFill, data.cageBorder, data.cageBorderWidth);
            _cage.raycastTarget = false;

            // Zonas
            for (int i = 0; i < data.zones.Count; i++)
            {
                var z = data.zones[i];
                var shape = NewShape("Zona " + z.name, z.rect,
                    z.shape == MapShape.Elipse ? ShapeGraphic.Shape.Elipse : ShapeGraphic.Shape.Rectangulo,
                    data.zoneFill, data.zoneBorder, data.zoneBorderWidth);
                shape.raycastTarget = true;

                var label = NewText("Nombre", shape.rectTransform, z.name, labelFont, data.labelSize, data.textColor, TextAlignmentOptions.Center);
                Stretch(label.rectTransform, 4f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 10f;
                label.fontSizeMax = data.labelSize;

                var view = new ZoneView { rect = shape.rectTransform, shape = shape, label = label };
                int index = i;
                var events = shape.gameObject.AddComponent<MapZoneEvents>();
                events.Entered = () =>
                {
                    view.hovered = true;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayHover();
                };
                events.Exited = () => view.hovered = false;
                events.Clicked = () => Select(_selected == index ? -1 : index);
                _zones.Add(view);
            }

            // Ficha (encima de todo)
            var cardShape = NewShape("Ficha", infoCard, ShapeGraphic.Shape.Rectangulo, cardFill, cardBorder, 2f);
            cardShape.raycastTarget = true;
            _card = cardShape.gameObject.AddComponent<CanvasGroup>();
            _cardTitle = NewText("Titulo", cardShape.rectTransform, "", cardTitleFont, 30f, data.textColor, TextAlignmentOptions.Top);
            Stretch(_cardTitle.rectTransform, 18f);
            _cardTitle.rectTransform.offsetMin = new Vector2(18f, infoCard.height - 70f);
            _cardBody = NewText("Descripcion", cardShape.rectTransform, "", cardBodyFont, 20f, data.textColor, TextAlignmentOptions.TopLeft);
            Stretch(_cardBody.rectTransform, 18f);
            _cardBody.rectTransform.offsetMax = new Vector2(-18f, -68f);
            _cardBody.textWrappingMode = TextWrappingModes.Normal;
            _cardBody.enableAutoSizing = true;
            _cardBody.fontSizeMin = 13f;
            _cardBody.fontSizeMax = 20f;

            Render(_appearT);
        }

        // ------------------------------------------------------------------
        void Select(int index)
        {
            if (index == _selected) return;
            _selected = index;

            if (index >= 0)
            {
                _cardTitle.text = data.zones[index].name;
                _cardBody.text = data.zones[index].description;
            }
            if (AudioManager.Instance != null)
            {
                if (index >= 0) AudioManager.Instance.PlayExpand();
                else AudioManager.Instance.PlayCollapse();
            }
        }

        bool HasDescription(int i) =>
            i >= 0 && i < data.zones.Count && !string.IsNullOrWhiteSpace(data.zones[i].description);

        // ------------------------------------------------------------------
        void Update()
        {
            if (data == null) return;
            if (data.version != _builtVersion) Build();

            float dt = Time.unscaledDeltaTime;
            _appearT += dt;
            _cardT = Mathf.MoveTowards(_cardT, HasDescription(_selected) ? 1f : 0f, dt / Mathf.Max(0.01f, cardDuration));

            float k = 1f - Mathf.Exp(-12f * dt);
            foreach (var z in _zones) z.hover = Mathf.Lerp(z.hover, z.hovered ? 1f : 0f, k);

            Render(_appearT);
        }

        void Render(float t)
        {
            if (_cage == null) return;
            float appear = Mathf.Max(0.01f, data.appearDuration);

            float cageK = Mathf.Clamp01(t / appear);
            _cage.color = new Color(1f, 1f, 1f, UITween.EaseOutCubic(cageK));
            float cs = Mathf.Lerp(0.97f, 1f, UITween.EaseOutCubic(cageK));
            _cage.rectTransform.localScale = new Vector3(cs, cs, 1f);

            for (int i = 0; i < _zones.Count; i++)
            {
                var z = _zones[i];
                float zk = Mathf.Clamp01((t - appear * 0.3f - i * data.zoneStagger) / appear);
                float pop = Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(zk));
                float s = pop * Mathf.Lerp(1f, data.hoverScale, z.hover);
                z.rect.localScale = new Vector3(s, s, 1f);

                bool selected = i == _selected;
                Color fill = selected ? data.zoneSelected : Color.Lerp(data.zoneFill, data.zoneHover, z.hover);
                if (z.shape.FillColor != fill) z.shape.FillColor = fill;
                z.shape.color = new Color(1f, 1f, 1f, UITween.EaseOutCubic(zk));
                Color text = selected ? data.selectedTextColor : data.textColor;
                text.a *= UITween.EaseOutCubic(zk);
                z.label.color = text;
                z.shape.raycastTarget = zk >= 1f;
            }

            if (_card != null)
            {
                float ck = UITween.EaseOutCubic(_cardT);
                _card.alpha = ck;
                _card.blocksRaycasts = _cardT > 0.5f;
                float s = Mathf.Lerp(0.92f, 1f, ck);
                _card.transform.localScale = new Vector3(s, s, 1f);
                _card.gameObject.SetActive(_cardT > 0f);
            }
        }

        // ------------------------------------------------------------------
        ShapeGraphic NewShape(string name, Rect r, ShapeGraphic.Shape form, Color fill, Color border, float borderWidth)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(composition, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(r.x + r.width * 0.5f, -(r.y + r.height * 0.5f));
            rt.sizeDelta = new Vector2(r.width, r.height);
            var shape = go.AddComponent<ShapeGraphic>();
            shape.Form = form;
            shape.FillColor = fill;
            shape.BorderColor = border;
            shape.BorderWidth = borderWidth;
            return shape;
        }

        TMP_Text NewText(string name, RectTransform parent, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt, float margin)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
