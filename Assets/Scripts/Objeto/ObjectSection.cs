using System.Collections.Generic;
using RataBlanca.Audio;
using RataBlanca.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.Objeto
{
    /// <summary>
    /// Sección "Objeto": vista de despiece del turbante de Madame White.
    ///
    /// Estado inicial: se ven el turbante y el rubí; las dos zonas (rubí y
    /// tela) brillan con un pulso suave. Al pasar el mouse el brillo sube y
    /// suena el hover.
    ///  - Clic en la tela  → aparece el corte de la tela y se dibujan sus 3
    ///    flechas, una tras otra, cada una con su texto.
    ///  - Clic en el rubí  → aparece la lupa (línea + anillo), luego el
    ///    círculo con el rubí transparente y la flecha al componente.
    /// Cada zona se abre y se cierra por separado (otro clic la cierra).
    /// Escape o Volver cierran lo que esté abierto con la animación al revés.
    ///
    /// Cada vista funciona como una "línea de tiempo": abrir la recorre hacia
    /// adelante y cerrar hacia atrás. Por eso, si se hace clic a mitad de la
    /// animación, simplemente se devuelve desde donde iba.
    /// Todo lo editable está en el archivo de datos (ExplodedViewData).
    /// </summary>
    public class ObjectSection : Section
    {
        [Header("Datos (textos, flechas, colores, tiempos)")]
        public ExplodedViewData data;

        [Header("Zonas interactivas")]
        [Tooltip("Turbante: hacer clic aquí abre la tela.")]
        public HotZone clothZone;
        [Tooltip("Rubí: hacer clic aquí abre la lupa.")]
        public HotZone rubyZone;
        [Tooltip("La propia imagen de la tela (clic para cerrarla).")]
        public HotZone clothPieceZone;
        [Tooltip("El propio círculo de la lupa (clic para cerrarla).")]
        public HotZone zoomPieceZone;

        [Header("Brillos")]
        public Image clothGlow;
        public Image rubyGlow;

        [Header("Vista de la tela")]
        public CanvasGroup clothView;
        public List<ArrowGraphic> clothArrows = new List<ArrowGraphic>();
        public List<TMP_Text> clothLabels = new List<TMP_Text>();

        [Header("Vista del rubí")]
        public CanvasGroup zoomView;
        public ArrowGraphic magnifierLine;
        public Image ring;
        public List<ArrowGraphic> rubyArrows = new List<ArrowGraphic>();
        public List<TMP_Text> rubyLabels = new List<TMP_Text>();

        // ------------------------------------------------------------------
        class Zone
        {
            public bool target;     // true = abriendo / abierta
            public float time;      // posición en la línea de tiempo (s)
            public float duration;  // duración total al abrir
            public float hover;     // 0..1 suavizado
            public int count;       // flechas activas
        }

        readonly Zone _cloth = new Zone();
        readonly Zone _ruby = new Zone();
        int _appliedVersion = -1;
        bool _wired;

        // ------------------------------------------------------------------
        void Awake()
        {
            Wire();
            ApplyData();
            ResetViews();
        }

        void Wire()
        {
            if (_wired) return;
            _wired = true;
            if (clothZone != null) { clothZone.Clicked += ToggleCloth; clothZone.HoverChanged += OnHover; }
            if (rubyZone != null) { rubyZone.Clicked += ToggleRuby; rubyZone.HoverChanged += OnHover; }
            if (clothPieceZone != null) clothPieceZone.Clicked += ToggleCloth;
            if (zoomPieceZone != null) zoomPieceZone.Clicked += ToggleRuby;
        }

        static void OnHover(bool entered)
        {
            if (entered && AudioManager.Instance != null) AudioManager.Instance.PlayHover();
        }

        protected override void OnOpened() => ResetViews();
        protected override void OnClosing() { }

        public override bool HandleBack()
        {
            if (!_cloth.target && !_ruby.target) return false;
            _cloth.target = false;
            _ruby.target = false;
            PlayCollapse();
            return true;
        }

        // ------------------------------------------------------------------
        public void ToggleCloth()
        {
            _cloth.target = !_cloth.target;
            if (_cloth.target) PlayExpand(); else PlayCollapse();
        }

        public void ToggleRuby()
        {
            _ruby.target = !_ruby.target;
            if (_ruby.target) PlayExpand(); else PlayCollapse();
        }

        static void PlayExpand() { if (AudioManager.Instance != null) AudioManager.Instance.PlayExpand(); }
        static void PlayCollapse() { if (AudioManager.Instance != null) AudioManager.Instance.PlayCollapse(); }

        void ResetViews()
        {
            _cloth.target = _ruby.target = false;
            _cloth.time = _ruby.time = 0f;
            _cloth.hover = _ruby.hover = 0f;
            if (data == null) return;
            RenderCloth(0f);
            RenderRuby(0f);
            UpdateGlow(0f);
        }

        // ------------------------------------------------------------------
        void Update()
        {
            if (data == null) return;
            if (data.version != _appliedVersion) ApplyData();

            float dt = Time.unscaledDeltaTime;
            Advance(_cloth, dt);
            Advance(_ruby, dt);
            RenderCloth(_cloth.time);
            RenderRuby(_ruby.time);
            UpdateGlow(dt);
        }

        void Advance(Zone z, float dt)
        {
            if (z.target) z.time = Mathf.Min(z.duration, z.time + dt);
            else z.time = Mathf.Max(0f, z.time - dt * data.closeSpeed);
        }

        // ------------------------------------------------------------------
        // Líneas de tiempo
        // ------------------------------------------------------------------
        float CalloutsDuration(int count) =>
            count <= 0 ? 0f : (count - 1) * (data.arrowDuration + data.arrowStagger) + data.arrowDuration + data.textFadeDuration;

        float ClothDuration() => data.appearDuration + CalloutsDuration(_cloth.count);

        float RubyZoomStart() => data.magnifierLineDuration * 0.6f;
        float RubyDuration() => RubyZoomStart() + data.appearDuration + CalloutsDuration(_ruby.count);

        void RenderCloth(float t)
        {
            float appear = Phase(t, 0f, data.appearDuration);
            SetPiece(clothView, appear);
            RenderCallouts(clothArrows, clothLabels, _cloth.count, t - data.appearDuration);
        }

        void RenderRuby(float t)
        {
            float line = Phase(t, 0f, data.magnifierLineDuration);
            if (magnifierLine != null) magnifierLine.Progress = UITween.EaseOutCubic(line);
            if (ring != null)
            {
                Color c = data.magnifierLineColor;
                c.a *= data.showRing ? UITween.EaseOutCubic(line) : 0f;
                ring.color = c;
                ring.enabled = c.a > 0.001f;
            }

            float appear = Phase(t, RubyZoomStart(), data.appearDuration);
            SetPiece(zoomView, appear);
            RenderCallouts(rubyArrows, rubyLabels, _ruby.count, t - RubyZoomStart() - data.appearDuration);
        }

        void RenderCallouts(List<ArrowGraphic> arrows, List<TMP_Text> labels, int count, float t)
        {
            for (int i = 0; i < count; i++)
            {
                float start = i * (data.arrowDuration + data.arrowStagger);
                float a = Phase(t, start, data.arrowDuration);
                float l = Phase(t, start + data.arrowDuration * 0.75f, data.textFadeDuration);
                if (i < arrows.Count && arrows[i] != null)
                {
                    arrows[i].Progress = UITween.EaseOutCubic(a);
                    arrows[i].enabled = a > 0f;
                }
                if (i < labels.Count && labels[i] != null)
                {
                    labels[i].alpha = l;
                    labels[i].enabled = l > 0f;
                }
            }
        }

        void SetPiece(CanvasGroup piece, float k)
        {
            if (piece == null) return;
            bool visible = k > 0f;
            if (piece.gameObject.activeSelf != visible) piece.gameObject.SetActive(visible);
            if (!visible) return;
            piece.alpha = UITween.EaseOutCubic(k);
            float s = Mathf.LerpUnclamped(data.appearStartScale, 1f, EaseOutBack(k));
            piece.transform.localScale = new Vector3(s, s, 1f);
            piece.blocksRaycasts = k >= 1f;
        }

        static float Phase(float t, float start, float duration)
        {
            if (duration <= 0f) return t >= start ? 1f : 0f;
            return Mathf.Clamp01((t - start) / duration);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.20158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        // ------------------------------------------------------------------
        // Brillo
        // ------------------------------------------------------------------
        void UpdateGlow(float dt)
        {
            float k = 1f - Mathf.Exp(-data.hoverResponse * dt);
            _cloth.hover = Mathf.Lerp(_cloth.hover, clothZone != null && clothZone.IsHovered ? 1f : 0f, k);
            _ruby.hover = Mathf.Lerp(_ruby.hover, rubyZone != null && rubyZone.IsHovered ? 1f : 0f, k);

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * data.pulseSpeed * Mathf.PI * 2f);
            float pulse = Mathf.Lerp(data.glowMin, data.glowMax, wave);

            SetGlow(clothGlow, data.clothGlowColor, _cloth, pulse);
            SetGlow(rubyGlow, data.rubyGlowColor, _ruby, pulse);
        }

        void SetGlow(Image glow, Color color, Zone z, float pulse)
        {
            if (glow == null) return;
            float idle = Mathf.Lerp(pulse, data.glowHover, z.hover);
            float openAmount = z.duration > 0f ? Mathf.Clamp01(z.time / Mathf.Max(0.01f, data.appearDuration)) : 0f;
            color.a = Mathf.Lerp(idle, data.glowWhenOpen, openAmount);
            glow.color = color;
        }

        // ------------------------------------------------------------------
        // Datos → escena
        // ------------------------------------------------------------------
        void ApplyData()
        {
            if (data == null) return;
            _appliedVersion = data.version;

            _cloth.count = EnsureCount(clothArrows, clothLabels, data.clothCallouts.Count);
            _ruby.count = EnsureCount(rubyArrows, rubyLabels, data.rubyCallouts.Count);

            for (int i = 0; i < _cloth.count; i++) ApplyCallout(clothArrows[i], clothLabels[i], data.clothCallouts[i]);
            for (int i = 0; i < _ruby.count; i++) ApplyCallout(rubyArrows[i], rubyLabels[i], data.rubyCallouts[i]);

            if (magnifierLine != null)
            {
                magnifierLine.StartPoint = data.magnifierLineStart;
                magnifierLine.EndPoint = data.magnifierLineEnd;
                magnifierLine.Thickness = data.magnifierLineThickness;
                magnifierLine.ShowHead = false;
                magnifierLine.Dashed = true;
                magnifierLine.DashLength = data.dashLength;
                magnifierLine.DashGap = data.dashGap;
                magnifierLine.color = data.magnifierLineColor;
            }

            if (ring != null)
            {
                var rt = ring.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(data.ringCenter.x, -data.ringCenter.y);
                rt.sizeDelta = new Vector2(data.ringDiameter, data.ringDiameter);
            }

            _cloth.duration = ClothDuration();
            _ruby.duration = RubyDuration();
            _cloth.time = Mathf.Min(_cloth.time, _cloth.duration);
            _ruby.time = Mathf.Min(_ruby.time, _ruby.duration);
        }

        /// <summary>
        /// Garantiza que haya tantas flechas/textos como entradas en los datos.
        /// Si agregas una entrada en el Inspector, se clona la última flecha.
        /// </summary>
        static int EnsureCount(List<ArrowGraphic> arrows, List<TMP_Text> labels, int wanted)
        {
            if (arrows.Count == 0 || labels.Count == 0) return 0;
            while (arrows.Count < wanted)
            {
                var a = Instantiate(arrows[arrows.Count - 1], arrows[arrows.Count - 1].transform.parent);
                a.name = "Flecha" + (arrows.Count + 1);
                arrows.Add(a);
            }
            while (labels.Count < wanted)
            {
                var l = Instantiate(labels[labels.Count - 1], labels[labels.Count - 1].transform.parent);
                l.name = "Texto" + (labels.Count + 1);
                labels.Add(l);
            }
            // Las que sobran se ocultan
            for (int i = wanted; i < arrows.Count; i++) if (arrows[i] != null) { arrows[i].Progress = 0f; arrows[i].enabled = false; }
            for (int i = wanted; i < labels.Count; i++) if (labels[i] != null) { labels[i].alpha = 0f; labels[i].enabled = false; }
            return Mathf.Min(wanted, Mathf.Min(arrows.Count, labels.Count));
        }

        void ApplyCallout(ArrowGraphic arrow, TMP_Text label, Callout c)
        {
            if (arrow != null)
            {
                arrow.StartPoint = c.start;
                arrow.EndPoint = c.end;
                arrow.Thickness = data.arrowThickness;
                arrow.HeadLength = data.arrowHeadLength;
                arrow.HeadWidth = data.arrowHeadWidth;
                arrow.ShowHead = true;
                arrow.Dashed = false;
                arrow.color = data.arrowColor;
            }
            if (label == null) return;

            float alpha = label.alpha;
            label.text = c.text;
            label.fontSize = data.textSize;
            label.color = data.textColor;
            label.alpha = alpha;
            label.textWrappingMode = TextWrappingModes.Normal;

            Vector2 pivot;
            Vector2 offset;
            TextAlignmentOptions align;
            float gap = data.labelGap;
            switch (c.labelSide)
            {
                case LabelSide.Izquierda: pivot = new Vector2(1f, 0.5f); offset = new Vector2(-gap, 0f); align = TextAlignmentOptions.MidlineRight; break;
                case LabelSide.Abajo: pivot = new Vector2(0.5f, 1f); offset = new Vector2(0f, gap); align = TextAlignmentOptions.Top; break;
                case LabelSide.Arriba: pivot = new Vector2(0.5f, 0f); offset = new Vector2(0f, -gap); align = TextAlignmentOptions.Bottom; break;
                default: pivot = new Vector2(0f, 0.5f); offset = new Vector2(gap, 0f); align = TextAlignmentOptions.MidlineLeft; break;
            }

            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(c.start.x + offset.x, -(c.start.y + offset.y));
            rt.sizeDelta = new Vector2(c.labelWidth, data.textSize * 2.6f);
            label.alignment = align;
        }
    }
}
