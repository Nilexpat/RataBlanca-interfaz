using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Torta o dona dibujada con uGUI. Cada porción tiene su fracción y color.
    /// "Fill" (0 a 1) recorre el círculo en sentido horario desde el ángulo
    /// inicial: así se anima la torta "llenándose".
    /// Ángulos en grados, 0 = arriba, sentido horario.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class PieGraphic : MaskableGraphic
    {
        public struct Slice
        {
            public float fraction;
            public Color color;
            public Slice(float fraction, Color color) { this.fraction = fraction; this.color = color; }
        }

        readonly List<Slice> _slices = new List<Slice>();
        float _innerRatio = 0.46f;
        float _startAngle;
        float _fill = 1f;
        float _gap = 4f;
        float _stepDegrees = 2.5f;

        public float Fill
        {
            get => _fill;
            set { value = Mathf.Clamp01(value); if (Mathf.Approximately(value, _fill)) return; _fill = value; SetVerticesDirty(); }
        }

        public float InnerRatio { get => _innerRatio; set { _innerRatio = Mathf.Clamp(value, 0f, 0.95f); SetVerticesDirty(); } }
        public float StartAngle { get => _startAngle; set { _startAngle = value; SetVerticesDirty(); } }
        /// <summary>Separación entre porciones, en píxeles.</summary>
        public float Gap { get => _gap; set { _gap = Mathf.Max(0f, value); SetVerticesDirty(); } }

        public void SetSlices(IEnumerable<Slice> slices)
        {
            _slices.Clear();
            _slices.AddRange(slices);
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            // Sin CanvasRenderer el gráfico no se dibuja. Las escenas creadas
            // antes de este arreglo no lo tienen, así que se agrega aquí.
            if (!TryGetComponent<CanvasRenderer>(out _)) gameObject.AddComponent<CanvasRenderer>();
            base.Awake();
            raycastTarget = false;
        }

        public float OuterRadius
        {
            get { Rect r = rectTransform.rect; return Mathf.Min(r.width, r.height) * 0.5f; }
        }

        /// <summary>Punto sobre el círculo (coordenadas locales del rect).</summary>
        public Vector2 PointAt(float angleDegrees, float radius)
        {
            Rect r = rectTransform.rect;
            float a = (angleDegrees + _startAngle) * Mathf.Deg2Rad;
            return r.center + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radius;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float outer = OuterRadius;
            float inner = outer * _innerRatio;
            if (outer <= 0f || _slices.Count == 0 || _fill <= 0f) return;

            float limit = 360f * _fill;
            float acc = 0f;
            foreach (var slice in _slices)
            {
                float a0 = acc;
                float a1 = acc + Mathf.Max(0f, slice.fraction) * 360f;
                acc = a1;
                float end = Mathf.Min(a1, limit);
                if (end <= a0) break;
                bool trimStart = true;
                bool trimEnd = end >= a1 - 0.001f;
                AddRing(vh, a0, end, inner, outer, slice.color, trimStart, trimEnd);
            }
        }

        void AddRing(VertexHelper vh, float a0, float a1, float inner, float outer, Color c, bool trimStart, bool trimEnd)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt((a1 - a0) / _stepDegrees));
            for (int i = 0; i < steps; i++)
            {
                float t0 = Mathf.Lerp(a0, a1, (float)i / steps);
                float t1 = Mathf.Lerp(a0, a1, (float)(i + 1) / steps);
                bool first = i == 0 && trimStart;
                bool last = i == steps - 1 && trimEnd;

                Vector2 i0 = Edge(t0, inner, a0, a1, first, false);
                Vector2 o0 = Edge(t0, outer, a0, a1, first, false);
                Vector2 i1 = Edge(t1, inner, a0, a1, false, last);
                Vector2 o1 = Edge(t1, outer, a0, a1, false, last);

                int v = vh.currentVertCount;
                vh.AddVert(i0, c, Vector2.zero);
                vh.AddVert(o0, c, Vector2.zero);
                vh.AddVert(o1, c, Vector2.zero);
                vh.AddVert(i1, c, Vector2.zero);
                vh.AddTriangle(v, v + 1, v + 2);
                vh.AddTriangle(v, v + 2, v + 3);
            }
        }

        // Recorta el borde de la porción para dejar una separación de ancho
        // constante (en píxeles) entre porciones vecinas.
        Vector2 Edge(float angle, float radius, float a0, float a1, bool trimStart, bool trimEnd)
        {
            if (radius > 0f && _gap > 0f && (trimStart || trimEnd))
            {
                float offset = Mathf.Rad2Deg * (_gap * 0.5f / radius);
                float span = a1 - a0;
                offset = Mathf.Min(offset, span * 0.35f);
                if (trimStart) angle += offset;
                if (trimEnd) angle -= offset;
            }
            return PointAt(angle, radius);
        }
    }
}
