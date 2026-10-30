using System;
using RataBlanca.Audio;
using RataBlanca.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.Graficos
{
    /// <summary>
    /// Un bloque desplegable de Misterios (diseño de Figma "Desktop - 4"):
    /// cabecera con título e ícono, cuerpo con el gráfico y barra inferior
    /// con la flecha. Al abrirse, el cuerpo crece con animación, los demás
    /// bloques se reacomodan solos (VerticalLayoutGroup) y el gráfico se
    /// anima. Al cerrarse, todo vuelve.
    /// </summary>
    public class AccordionBlock : MonoBehaviour
    {
        [Header("Partes (las arma el constructor)")]
        public RectTransform header;
        public RectTransform body;
        public RectTransform card;
        public RectTransform footer;
        public RectTransform chevron;
        public TMP_Text title;
        public Image icon;
        public ChartView chart;
        public Button headerButton;
        public Button footerButton;
        public LayoutElement layout;

        [Header("Íconos por tipo de gráfico")]
        public Sprite iconPie;
        public Sprite iconHorizontalBars;
        public Sprite iconVerticalBars;
        public Sprite iconLines;

        [Header("Medidas (Figma)")]
        public float headerMinHeight = 109f;
        public float titleVerticalPadding = 29f;
        public float titleWidth = 449f;
        public float footerHeight = 38f;
        public float cardMargin = 12f;

        [Header("Animación")]
        public float openDuration = 0.45f;
        [Tooltip("En qué punto de la apertura empieza a animarse el gráfico (0-1).")]
        [Range(0f, 1f)] public float chartStartsAt = 0.55f;
        [Tooltip("Qué tan rápido se 'desarma' el gráfico al cerrar, comparado con armarse.")]
        public float chartCloseSpeed = 3f;

        public bool IsOpen { get; private set; }
        public ChartData Data { get; private set; }

        /// <summary>Alto que tendrá el bloque cuando termine la animación.</summary>
        public float FinalHeight => _headerHeight + (IsOpen ? _bodyHeight : 0f) + footerHeight;

        Action<AccordionBlock, bool> _onToggle;
        float _headerHeight;
        float _bodyHeight;
        float _t;          // 0 = cerrado, 1 = abierto (lineal)
        float _chartT;     // progreso del gráfico (lineal)

        public void Setup(ChartData data, Action<AccordionBlock, bool> onToggle)
        {
            Data = data;
            _onToggle = onToggle;
            name = "Bloque " + data.name;

            title.text = data.title;
            Vector2 pref = title.GetPreferredValues(data.title, titleWidth, 0f);
            _headerHeight = Mathf.Max(headerMinHeight, pref.y + titleVerticalPadding * 2f);
            header.sizeDelta = new Vector2(header.sizeDelta.x, _headerHeight);

            icon.sprite = data.icon != null ? data.icon : IconFor(data.type);
            icon.preserveAspect = true;

            _bodyHeight = Mathf.Max(120f, data.bodyHeight);
            float width = ((RectTransform)transform).rect.width;
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.anchoredPosition = new Vector2(0f, -cardMargin);
            card.sizeDelta = new Vector2(width - cardMargin * 2f, _bodyHeight - cardMargin * 2f);

            chart.data = data;
            chart.Build();

            headerButton.onClick.AddListener(Toggle);
            footerButton.onClick.AddListener(Toggle);

            _t = 0f;
            _chartT = 0f;
            Apply();
        }

        Sprite IconFor(ChartType type)
        {
            switch (type)
            {
                case ChartType.Torta: return iconPie;
                case ChartType.BarrasHorizontales: return iconHorizontalBars;
                case ChartType.Lineas: return iconLines;
                default: return iconVerticalBars;
            }
        }

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool open, bool notify = true, bool playSound = true)
        {
            if (open == IsOpen) return;
            IsOpen = open;
            if (playSound && AudioManager.Instance != null)
            {
                if (open) AudioManager.Instance.PlayExpand();
                else AudioManager.Instance.PlayCollapse();
            }
            if (notify) _onToggle?.Invoke(this, open);
        }

        /// <summary>Cierra sin animación (al salir de la sección).</summary>
        public void CloseImmediate()
        {
            IsOpen = false;
            _t = 0f;
            _chartT = 0f;
            Apply();
        }

        void Update()
        {
            if (Data == null) return;
            float dt = Time.unscaledDeltaTime;
            float before = _t;
            _t = Mathf.MoveTowards(_t, IsOpen ? 1f : 0f, dt / Mathf.Max(0.01f, openDuration));

            float chartDuration = Mathf.Max(0.05f, Data.duration);
            if (IsOpen && _t >= chartStartsAt) _chartT = Mathf.Min(1f, _chartT + dt / chartDuration);
            else if (!IsOpen) _chartT = Mathf.Max(0f, _chartT - dt * chartCloseSpeed / chartDuration);

            if (!Mathf.Approximately(before, _t) || IsOpen || _chartT > 0f) Apply();
        }

        void Apply()
        {
            float eased = UITween.EaseInOutCubic(_t);
            float bodyNow = _bodyHeight * eased;

            body.gameObject.SetActive(bodyNow > 0.5f);
            body.anchoredPosition = new Vector2(0f, -(_headerHeight - 2f));
            body.sizeDelta = new Vector2(body.sizeDelta.x, bodyNow);

            footer.anchoredPosition = new Vector2(0f, -(_headerHeight + Mathf.Max(0f, bodyNow - 2f) - 2f));
            footer.sizeDelta = new Vector2(footer.sizeDelta.x, footerHeight);

            if (chevron != null) chevron.localEulerAngles = new Vector3(0f, 0f, 180f * eased);

            float total = _headerHeight + Mathf.Max(0f, bodyNow - 2f) + footerHeight - 2f;
            layout.preferredHeight = total;
            layout.minHeight = total;

            if (chart != null && !Mathf.Approximately(chart.Progress, _chartT)) chart.SetProgress(_chartT);
        }
    }
}
