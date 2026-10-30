using System.Collections.Generic;
using RataBlanca.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.Graficos
{
    /// <summary>
    /// Sección "Misterios": lista de bloques desplegables, uno por gráfico.
    /// Los bloques se crean al iniciar a partir de la lista "Charts", así que
    /// para agregar un gráfico basta con crear su archivo y sumarlo a la lista.
    /// Escape o Volver cierran primero el gráfico abierto.
    /// </summary>
    public class MisteriosSection : Section
    {
        [Header("Gráficos (en orden)")]
        public List<ChartData> charts = new List<ChartData>();

        [Header("Partes (las arma el constructor)")]
        public AccordionBlock template;
        public RectTransform listContent;
        public ScrollRect scroll;

        [Header("Comportamiento")]
        [Tooltip("Si está activo, al abrir un gráfico se cierra el que estaba abierto.")]
        public bool singleOpen = true;
        [Tooltip("Desplazar la lista para que el gráfico abierto se vea completo.")]
        public bool autoScroll = true;
        public float scrollSmoothTime = 0.25f;

        readonly List<AccordionBlock> _blocks = new List<AccordionBlock>();
        AccordionBlock _focus;
        float _focusUntil;
        float _scrollVelocity;

        void Awake()
        {
            if (template == null || listContent == null) return;
            template.gameObject.SetActive(false);
            foreach (var chart in charts)
            {
                if (chart == null) continue;
                var block = Instantiate(template, listContent);
                block.gameObject.SetActive(true);
                block.Setup(chart, OnBlockToggled);
                _blocks.Add(block);
            }
        }

        protected override void OnOpened()
        {
            foreach (var b in _blocks) b.CloseImmediate();
            if (listContent != null) listContent.anchoredPosition = Vector2.zero;
            _focus = null;
        }

        public override bool HandleBack()
        {
            bool anyOpen = false;
            foreach (var b in _blocks)
            {
                if (!b.IsOpen) continue;
                b.SetOpen(false, notify: false, playSound: !anyOpen);
                anyOpen = true;
            }
            return anyOpen;
        }

        void OnBlockToggled(AccordionBlock block, bool open)
        {
            if (!open) return;
            if (singleOpen)
                foreach (var b in _blocks)
                    if (b != block && b.IsOpen) b.SetOpen(false, notify: false, playSound: false);

            _focus = block;
            _focusUntil = Time.unscaledTime + 1.2f;
            _scrollVelocity = 0f;
            if (scroll != null) scroll.StopMovement();
        }

        void LateUpdate()
        {
            if (!autoScroll || _focus == null || scroll == null || listContent == null) return;
            if (Time.unscaledTime > _focusUntil) { _focus = null; return; }

            float viewH = scroll.viewport != null ? scroll.viewport.rect.height : ((RectTransform)scroll.transform).rect.height;
            float current = listContent.anchoredPosition.y;
            float top = -((RectTransform)_focus.transform).anchoredPosition.y;
            float target = current;
            const float margin = 24f;

            if (top + _focus.FinalHeight > current + viewH - margin) target = top + _focus.FinalHeight - viewH + margin;
            if (top - margin < target) target = Mathf.Min(target, top - margin);
            target = Mathf.Max(0f, target);

            float y = Mathf.SmoothDamp(current, target, ref _scrollVelocity, scrollSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            listContent.anchoredPosition = new Vector2(listContent.anchoredPosition.x, y);
        }
    }
}
