using System;
using System.Collections.Generic;
using System.Globalization;
using RataBlanca.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.Graficos
{
    /// <summary>
    /// Dibuja un gráfico (torta, barras o líneas) a partir de un ChartData,
    /// con elementos de uGUI (nada de imágenes de la gráfica), y lo anima con
    /// SetProgress(0..1): las barras crecen, la torta se llena y las líneas se
    /// dibujan punto por punto. Se reconstruye solo si cambian los datos.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ChartView : MonoBehaviour
    {
        public ChartData data;

        [Header("Estilo")]
        public TMP_FontAsset font;
        public Color textColor = new Color32(0x28, 0x28, 0x28, 0xFF);
        public Color valueTextColor = Color.white;
        [Tooltip("Sprite circular para los puntos de las líneas y la leyenda.")]
        public Sprite dotSprite;

        readonly List<Action<float>> _animators = new List<Action<float>>();
        float _progress;
        int _builtVersion = -1;
        ChartData _builtData;
        RectTransform _rect;

        public float Progress => _progress;

        void Awake() => _rect = (RectTransform)transform;

        void Update()
        {
            // Si se editan los datos con el Play activo, se redibuja.
            if (data != null && _builtData == data && data.version != _builtVersion) Build();
        }

        // ==================================================================
        public void Build()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                // Se desactiva antes de destruir para que los gráficos se
                // desconecten del Canvas mientras aún tienen su CanvasRenderer.
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            _animators.Clear();
            _builtData = data;
            if (data == null) return;
            _builtVersion = data.version;

            switch (data.type)
            {
                case ChartType.Torta: BuildPie(); break;
                case ChartType.BarrasVerticales: BuildVerticalBars(); break;
                case ChartType.BarrasHorizontales: BuildHorizontalBars(); break;
                case ChartType.Lineas: BuildLines(); break;
            }
            SetProgress(_progress);
        }

        /// <summary>0 = gráfico vacío, 1 = gráfico completo.</summary>
        public void SetProgress(float p)
        {
            _progress = Mathf.Clamp01(p);
            float k = data != null && data.curve != null && data.curve.length > 0 ? data.curve.Evaluate(_progress) : _progress;
            foreach (var a in _animators) a(k);
        }

        // ==================================================================
        // Barras verticales
        // ==================================================================
        void BuildVerticalBars()
        {
            float W = _rect.rect.width, H = _rect.rect.height;
            const float pad = 16f;
            float left = pad + (HasText(data.yAxisLabel) ? 24f : 0f);
            float bottom = pad + 24f + (HasText(data.xAxisLabel) ? 22f : 0f);
            float top = pad + 8f;
            float plotW = W - left - pad;
            float plotH = H - bottom - top;
            int n = data.entries.Count;
            if (n == 0 || plotW <= 0f || plotH <= 0f) return;

            float max = data.yMax > 0f ? data.yMax : MaxEntry() * 1.06f;
            if (max <= 0f) max = 1f;

            Box("Base", data.axisColor * new Color(1, 1, 1, 0.5f), new Vector2(left, bottom - 1f), new Vector2(plotW, 2f), Vector2.zero);

            float slot = plotW / n;
            float barW = Mathf.Min(slot * 0.56f, 90f);
            for (int i = 0; i < n; i++)
            {
                var e = data.entries[i];
                float x = left + slot * (i + 0.5f);
                float full = plotH * Mathf.Clamp01(e.value / max);
                var bar = Box("Barra" + i, e.color, new Vector2(x, bottom), new Vector2(barW, 0f), new Vector2(0.5f, 0f));
                Label("Categoria" + i, e.label, 14f, textColor, TextAlignmentOptions.Top,
                    new Vector2(x, bottom - 6f), new Vector2(slot, 22f), new Vector2(0.5f, 1f));

                TMP_Text value = null;
                bool inside = full > 36f;
                if (data.showValues)
                    value = Label("Valor" + i, Format(e.value), 17f, inside ? valueTextColor : textColor,
                        TextAlignmentOptions.Center, Vector2.zero, new Vector2(slot, 24f), new Vector2(0.5f, inside ? 1f : 0f));

                int index = i;
                _animators.Add(k =>
                {
                    float local = Stagger(k, index, n);
                    float h = full * local;
                    bar.sizeDelta = new Vector2(barW, h);
                    if (value != null)
                    {
                        value.rectTransform.anchoredPosition = inside ? new Vector2(x, bottom + h - 6f) : new Vector2(x, bottom + h + 4f);
                        value.alpha = Mathf.Clamp01((local - 0.55f) * 2.5f);
                    }
                });
            }
            AxisTitles(W, H, pad, left, bottom, plotW, plotH);
        }

        // ==================================================================
        // Barras horizontales (pegadas, como en el diseño)
        // ==================================================================
        void BuildHorizontalBars()
        {
            float W = _rect.rect.width, H = _rect.rect.height;
            const float pad = 16f;
            const float labelSpace = 150f;
            float bottom = pad + (HasText(data.xAxisLabel) ? 22f : 0f);
            float plotW = W - pad * 2f - labelSpace;
            float plotH = H - pad - bottom;
            int n = data.entries.Count;
            if (n == 0 || plotW <= 0f || plotH <= 0f) return;

            float max = data.yMax > 0f ? data.yMax : MaxEntry();
            if (max <= 0f) max = 1f;
            float rowH = Mathf.Min(plotH / n, 64f);
            float startY = bottom + (plotH + rowH * n) * 0.5f;   // centrado vertical

            for (int i = 0; i < n; i++)
            {
                var e = data.entries[i];
                float y = startY - (i + 1) * rowH;
                float full = plotW * Mathf.Clamp01(e.value / max);
                var bar = Box("Barra" + i, e.color, new Vector2(pad, y), new Vector2(0f, rowH), Vector2.zero);
                var category = Label("Categoria" + i, e.label, 14f, textColor, TextAlignmentOptions.MidlineLeft,
                    Vector2.zero, new Vector2(labelSpace - 8f, rowH), new Vector2(0f, 0.5f));
                TMP_Text value = data.showValues
                    ? Label("Valor" + i, Format(e.value), 16f, valueTextColor, TextAlignmentOptions.MidlineRight,
                        Vector2.zero, new Vector2(80f, rowH), new Vector2(1f, 0.5f))
                    : null;

                int index = i;
                _animators.Add(k =>
                {
                    float local = Stagger(k, index, n);
                    float w = full * local;
                    bar.sizeDelta = new Vector2(w, rowH);
                    category.rectTransform.anchoredPosition = new Vector2(pad + w + 10f, y + rowH * 0.5f);
                    category.alpha = Mathf.Clamp01(local * 3f);
                    if (value != null)
                    {
                        value.rectTransform.anchoredPosition = new Vector2(pad + w - 10f, y + rowH * 0.5f);
                        value.alpha = w > 46f ? Mathf.Clamp01((local - 0.6f) * 2.5f) : 0f;
                    }
                });
            }
            if (HasText(data.xAxisLabel))
                Label("TituloX", data.xAxisLabel, 13f, textColor, TextAlignmentOptions.Bottom,
                    new Vector2(pad + plotW * 0.5f, pad), new Vector2(plotW, 20f), new Vector2(0.5f, 0f));
        }

        // ==================================================================
        // Torta / dona
        // ==================================================================
        void BuildPie()
        {
            float W = _rect.rect.width, H = _rect.rect.height;
            const float pad = 16f;
            int n = data.entries.Count;
            if (n == 0) return;

            float legendW = data.showLegend ? Mathf.Min(230f, W * 0.42f) : 0f;
            float areaW = W - pad * 2f - legendW;
            float diameter = Mathf.Min(areaW, H - pad * 2f);
            var center = new Vector2(pad + areaW * 0.5f, H * 0.5f);

            float sum = 0f;
            foreach (var e in data.entries) sum += Mathf.Max(0f, e.value);
            if (sum <= 0f) sum = 1f;

            var pieRt = Node("Torta");
            pieRt.pivot = new Vector2(0.5f, 0.5f);
            pieRt.anchoredPosition = center;
            pieRt.sizeDelta = new Vector2(diameter, diameter);
            var pie = pieRt.gameObject.AddComponent<PieGraphic>();
            pie.InnerRatio = data.innerRadius;
            pie.StartAngle = data.startAngle;
            pie.Gap = data.sliceGap;
            var slices = new List<PieGraphic.Slice>();
            foreach (var e in data.entries) slices.Add(new PieGraphic.Slice(Mathf.Max(0f, e.value) / sum, e.color));
            pie.SetSlices(slices);

            // Textos sobre las porciones
            var labels = new List<(TMP_Text text, float mid)>();
            float acc = 0f;
            float radius = diameter * 0.5f;
            float labelRadius = radius * (1f + data.innerRadius) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float frac = Mathf.Max(0f, data.entries[i].value) / sum;
                float mid = acc + frac * 0.5f;
                float span = frac * 360f;
                acc += frac;
                if (data.pieLabels == PieLabelMode.Ninguno || span < data.minLabelAngle) continue;

                string text = data.pieLabels == PieLabelMode.Porcentaje
                    ? (frac * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%"
                    : Format(data.entries[i].value);
                float a = (mid * 360f + data.startAngle) * Mathf.Deg2Rad;
                Vector2 pos = center + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * labelRadius;
                var t = Label("Porcentaje" + i, text, 15f, valueTextColor, TextAlignmentOptions.Center,
                    pos, new Vector2(80f, 22f), new Vector2(0.5f, 0.5f));
                labels.Add((t, mid));
            }

            // Leyenda
            var legend = new List<TMP_Text>();
            var legendDots = new List<Image>();
            if (data.showLegend)
            {
                const float row = 30f;
                float lx = W - pad - legendW + 12f;
                float ly = H * 0.5f + row * n * 0.5f - row * 0.5f;
                for (int i = 0; i < n; i++)
                {
                    var sq = Box("Leyenda" + i, data.entries[i].color, new Vector2(lx, ly - i * row), new Vector2(16f, 16f), new Vector2(0f, 0.5f));
                    legendDots.Add(sq.GetComponent<Image>());
                    legend.Add(Label("LeyendaTexto" + i, data.entries[i].label, 16f, textColor, TextAlignmentOptions.MidlineLeft,
                        new Vector2(lx + 26f, ly - i * row), new Vector2(legendW - 40f, row), new Vector2(0f, 0.5f)));
                }
            }

            _animators.Add(k =>
            {
                pie.Fill = k;
                float s = Mathf.Lerp(0.88f, 1f, EaseOutBack(Mathf.Clamp01(k * 1.6f)));
                pieRt.localScale = new Vector3(s, s, 1f);
                foreach (var item in labels) item.text.alpha = Mathf.Clamp01((k - item.mid) * 6f);
                for (int i = 0; i < legend.Count; i++)
                {
                    float a = Stagger(k, i, legend.Count);
                    legend[i].alpha = a;
                    var c = legendDots[i].color; c.a = a; legendDots[i].color = c;
                }
            });
        }

        // ==================================================================
        // Líneas
        // ==================================================================
        void BuildLines()
        {
            float W = _rect.rect.width, H = _rect.rect.height;
            const float pad = 16f;
            int seriesCount = data.series.Count;
            bool legend = data.showLegend && seriesCount > 0;
            float legendH = legend ? seriesCount * 20f + 4f : 0f;

            float left = pad + (HasText(data.yAxisLabel) ? 24f : 0f) + 40f;
            float bottom = pad + 22f + (HasText(data.xAxisLabel) ? 22f : 0f);
            float top = pad + legendH + 10f;
            float right = pad + 10f;
            float plotW = W - left - right;
            float plotH = H - bottom - top;
            if (plotW <= 0f || plotH <= 0f) return;

            // Rangos
            float maxY = 0f, minX = float.MaxValue, maxX = float.MinValue;
            foreach (var s in data.series)
                foreach (var p in s.points) { maxY = Mathf.Max(maxY, p.y); minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); }
            float yMax = data.yMax > 0f ? data.yMax : NiceCeil(maxY);
            float yStep = data.yStep > 0f ? data.yStep : yMax / 4f;
            float xMin = data.xMax > data.xMin ? data.xMin : (minX == float.MaxValue ? 0f : minX);
            float xMax = data.xMax > data.xMin ? data.xMax : (maxX == float.MinValue ? 1f : maxX);
            float xStep = data.xStep > 0f ? data.xStep : (xMax - xMin) / 5f;
            if (yMax <= 0f) yMax = 1f;
            if (xMax <= xMin) xMax = xMin + 1f;

            // Ejes
            Box("EjeY", data.axisColor, new Vector2(left - 1f, bottom), new Vector2(2f, plotH), Vector2.zero);
            Box("EjeX", data.axisColor, new Vector2(left, bottom - 1f), new Vector2(plotW, 2f), Vector2.zero);

            int guard = 0;
            for (float v = 0f; v <= yMax + yStep * 0.01f && guard < 50; v += yStep, guard++)
                Label("MarcaY", Format(v, false), 12f, textColor, TextAlignmentOptions.MidlineRight,
                    new Vector2(left - 6f, bottom + plotH * v / yMax), new Vector2(40f, 18f), new Vector2(1f, 0.5f));
            guard = 0;
            for (float v = xMin; v <= xMax + xStep * 0.01f && guard < 60; v += xStep, guard++)
                Label("MarcaX", Format(v, false), 12f, textColor, TextAlignmentOptions.Top,
                    new Vector2(left + plotW * (v - xMin) / (xMax - xMin), bottom - 5f), new Vector2(34f, 18f), new Vector2(0.5f, 1f));

            // Series
            for (int si = 0; si < seriesCount; si++)
            {
                var s = data.series[si];
                var lineRt = Node("Linea" + si);
                lineRt.anchoredPosition = new Vector2(left, bottom);
                lineRt.sizeDelta = new Vector2(plotW, plotH);
                var line = lineRt.gameObject.AddComponent<LineGraphic>();
                line.color = s.color;
                line.Thickness = 3f;

                var pts = new List<Vector2>();
                foreach (var p in s.points)
                    pts.Add(new Vector2((p.x - xMin) / (xMax - xMin) * plotW, Mathf.Clamp01(p.y / yMax) * plotH));
                line.SetPoints(pts);

                var dots = new List<RectTransform>();
                for (int i = 0; i < pts.Count; i++)
                {
                    var d = Box("Punto" + si + "_" + i, s.color, new Vector2(left, bottom) + pts[i], new Vector2(10f, 10f), new Vector2(0.5f, 0.5f));
                    d.GetComponent<Image>().sprite = dotSprite;
                    dots.Add(d);
                }

                _animators.Add(k =>
                {
                    line.Progress = k;
                    for (int i = 0; i < dots.Count; i++)
                    {
                        float a = Mathf.Clamp01((k - line.FractionAt(i)) / 0.06f + (i == 0 && k > 0f ? 1f : 0f));
                        float sc = EaseOutBack(a);
                        dots[i].localScale = new Vector3(sc, sc, 1f);
                    }
                });
            }

            // Leyenda (arriba a la izquierda)
            if (legend)
            {
                for (int si = 0; si < seriesCount; si++)
                {
                    float y = H - pad - 10f - si * 20f;
                    var d = Box("LeyendaPunto" + si, data.series[si].color, new Vector2(left, y), new Vector2(10f, 10f), new Vector2(0f, 0.5f));
                    d.GetComponent<Image>().sprite = dotSprite;
                    Label("LeyendaTexto" + si, data.series[si].name, 13f, textColor, TextAlignmentOptions.MidlineLeft,
                        new Vector2(left + 16f, y), new Vector2(plotW - 16f, 20f), new Vector2(0f, 0.5f));
                }
            }
            AxisTitles(W, H, pad, left - 40f, bottom, plotW + 40f, plotH, left, plotW);
        }

        // ==================================================================
        // Utilidades
        // ==================================================================
        void AxisTitles(float W, float H, float pad, float left, float bottom, float plotW, float plotH,
            float xCenterLeft = -1f, float xCenterWidth = -1f)
        {
            if (HasText(data.yAxisLabel))
            {
                var y = Label("TituloY", data.yAxisLabel, 13f, textColor, TextAlignmentOptions.Center,
                    new Vector2(pad + 9f, bottom + plotH * 0.5f), new Vector2(plotH, 20f), new Vector2(0.5f, 0.5f));
                y.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f);
                FitText(y, 9f, 13f);
            }
            if (HasText(data.xAxisLabel))
            {
                float cl = xCenterLeft >= 0f ? xCenterLeft : left;
                float cw = xCenterWidth >= 0f ? xCenterWidth : plotW;
                var x = Label("TituloX", data.xAxisLabel, 13f, textColor, TextAlignmentOptions.Bottom,
                    new Vector2(cl + cw * 0.5f, pad), new Vector2(cw, 20f), new Vector2(0.5f, 0f));
                FitText(x, 9f, 13f);
            }
        }

        /// <summary>Reduce el texto si no cabe en su caja.</summary>
        static void FitText(TMP_Text t, float min, float max)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
            t.overflowMode = TextOverflowModes.Truncate;
        }

        RectTransform Node(string name)
        {
            // El CanvasRenderer se crea desde el inicio: lo necesitan todos los
            // elementos gráficos (barras, textos, torta, líneas).
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            return rt;
        }

        RectTransform Box(string name, Color color, Vector2 position, Vector2 size, Vector2 pivot)
        {
            var rt = Node(name);
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        TMP_Text Label(string name, string text, float size, Color color, TextAlignmentOptions align,
            Vector2 position, Vector2 boxSize, Vector2 pivot)
        {
            var rt = Node(name);
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = boxSize;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        float Stagger(float k, int i, int n)
        {
            if (n <= 1) return Mathf.Clamp01(k);
            float s = data.stagger;
            float span = 1f - s * (n - 1);
            if (span < 0.25f) { s = 0.75f / (n - 1); span = 0.25f; }
            return Mathf.Clamp01((k - i * s) / span);
        }

        float MaxEntry()
        {
            float m = 0f;
            foreach (var e in data.entries) m = Mathf.Max(m, e.value);
            return m;
        }

        string Format(float v, bool suffix = true)
        {
            string s = v.ToString("0.##", CultureInfo.InvariantCulture);
            return suffix && data != null ? s + data.valueSuffix : s;
        }

        static bool HasText(string s) => !string.IsNullOrWhiteSpace(s);

        static float NiceCeil(float v)
        {
            if (v <= 0f) return 1f;
            float exp = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(v)));
            float f = v / exp;
            float nice = f <= 1f ? 1f : f <= 2f ? 2f : f <= 5f ? 5f : 10f;
            return nice * exp;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
