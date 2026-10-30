using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Línea quebrada (polilínea) dibujada con uGUI. Los puntos van en
    /// píxeles desde la esquina inferior izquierda del rect. "Progress" (0 a 1)
    /// dibuja la línea parcialmente a lo largo de su longitud, para animarla.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class LineGraphic : MaskableGraphic
    {
        readonly List<Vector2> _points = new List<Vector2>();
        readonly List<float> _cumulative = new List<float>();
        float _thickness = 3f;
        float _progress = 1f;

        public float Thickness { get => _thickness; set { _thickness = Mathf.Max(0.5f, value); SetVerticesDirty(); } }

        public float Progress
        {
            get => _progress;
            set { value = Mathf.Clamp01(value); if (Mathf.Approximately(value, _progress)) return; _progress = value; SetVerticesDirty(); }
        }

        public float TotalLength => _cumulative.Count > 0 ? _cumulative[_cumulative.Count - 1] : 0f;

        /// <summary>Fracción (0-1) del recorrido en la que la línea llega al punto i.</summary>
        public float FractionAt(int i)
        {
            float total = TotalLength;
            if (total <= 0f || i < 0 || i >= _cumulative.Count) return 0f;
            return _cumulative[i] / total;
        }

        public void SetPoints(IList<Vector2> points)
        {
            _points.Clear();
            _points.AddRange(points);
            _cumulative.Clear();
            float acc = 0f;
            for (int i = 0; i < _points.Count; i++)
            {
                if (i > 0) acc += Vector2.Distance(_points[i - 1], _points[i]);
                _cumulative.Add(acc);
            }
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

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < 2 || _progress <= 0f) return;

            Vector2 origin = rectTransform.rect.min;
            float drawn = TotalLength * _progress;

            for (int i = 1; i < _points.Count; i++)
            {
                float segStart = _cumulative[i - 1];
                float segEnd = _cumulative[i];
                if (drawn <= segStart) break;

                Vector2 a = origin + _points[i - 1];
                Vector2 b = origin + _points[i];
                if (drawn < segEnd)
                {
                    float t = (drawn - segStart) / Mathf.Max(0.0001f, segEnd - segStart);
                    b = Vector2.Lerp(a, b, t);
                }
                AddSegment(vh, a, b);
                AddJoint(vh, a);
                if (drawn >= segEnd) AddJoint(vh, b);
            }
        }

        void AddSegment(VertexHelper vh, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 0.0001f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * (_thickness * 0.5f);
            int v = vh.currentVertCount;
            vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero);
            vh.AddVert(b - n, color, Vector2.zero);
            vh.AddVert(a - n, color, Vector2.zero);
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v, v + 2, v + 3);
        }

        // Pequeño disco en cada vértice para que las uniones queden redondas.
        void AddJoint(VertexHelper vh, Vector2 c)
        {
            const int segments = 10;
            float r = _thickness * 0.5f;
            int center = vh.currentVertCount;
            vh.AddVert(c, color, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, color, Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
                vh.AddTriangle(center, center + 1 + i, center + 2 + i);
        }
    }
}
