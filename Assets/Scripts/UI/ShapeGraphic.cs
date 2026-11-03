using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Rectángulo o elipse dibujado con uGUI, con relleno y borde de colores
    /// independientes (el mapa de Rat City y la base de los personajes).
    /// En la elipse, el clic solo cuenta dentro de la forma, no en las esquinas.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ShapeGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        public enum Shape { Rectangulo, Elipse }

        [SerializeField] Shape shape = Shape.Rectangulo;
        [SerializeField] Color fillColor = Color.white;
        [SerializeField] Color borderColor = Color.black;
        [SerializeField] float borderWidth = 2f;

        public Shape Form { get => shape; set { shape = value; SetVerticesDirty(); } }
        public Color FillColor { get => fillColor; set { fillColor = value; SetVerticesDirty(); } }
        public Color BorderColor { get => borderColor; set { borderColor = value; SetVerticesDirty(); } }
        public float BorderWidth { get => borderWidth; set { borderWidth = Mathf.Max(0f, value); SetVerticesDirty(); } }

        protected override void Awake()
        {
            if (!TryGetComponent<CanvasRenderer>(out _)) gameObject.AddComponent<CanvasRenderer>();
            base.Awake();
        }

        Color Tint(Color c) => c * color;   // "color" del Graphic sirve para desvanecer todo

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (r.width <= 0f || r.height <= 0f) return;
            if (shape == Shape.Elipse) PopulateEllipse(vh, r);
            else PopulateRect(vh, r);
        }

        void PopulateRect(VertexHelper vh, Rect r)
        {
            float b = Mathf.Min(borderWidth, r.width * 0.5f, r.height * 0.5f);
            Rect inner = new Rect(r.xMin + b, r.yMin + b, r.width - 2f * b, r.height - 2f * b);
            Quad(vh, inner.min, inner.max, Tint(fillColor));
            if (b <= 0f) return;
            Color bc = Tint(borderColor);
            Quad(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin + b), bc);          // abajo
            Quad(vh, new Vector2(r.xMin, r.yMax - b), new Vector2(r.xMax, r.yMax), bc);          // arriba
            Quad(vh, new Vector2(r.xMin, r.yMin + b), new Vector2(r.xMin + b, r.yMax - b), bc);  // izquierda
            Quad(vh, new Vector2(r.xMax - b, r.yMin + b), new Vector2(r.xMax, r.yMax - b), bc);  // derecha
        }

        void PopulateEllipse(VertexHelper vh, Rect r)
        {
            const int segments = 96;
            Vector2 c = r.center;
            float rx = r.width * 0.5f, ry = r.height * 0.5f;
            float b = Mathf.Min(borderWidth, rx, ry);
            float irx = rx - b, iry = ry - b;
            Color fc = Tint(fillColor);
            Color bc = Tint(borderColor);

            // Relleno (abanico)
            int center = vh.currentVertCount;
            vh.AddVert(c, fc, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                vh.AddVert(c + new Vector2(Mathf.Cos(a) * irx, Mathf.Sin(a) * iry), fc, Vector2.zero);
            }
            for (int i = 0; i < segments; i++) vh.AddTriangle(center, center + 1 + i, center + 2 + i);

            // Borde (anillo)
            if (b <= 0f) return;
            int start = vh.currentVertCount;
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vh.AddVert(c + new Vector2(dir.x * irx, dir.y * iry), bc, Vector2.zero);
                vh.AddVert(c + new Vector2(dir.x * rx, dir.y * ry), bc, Vector2.zero);
            }
            for (int i = 0; i < segments; i++)
            {
                int v = start + i * 2;
                vh.AddTriangle(v, v + 1, v + 3);
                vh.AddTriangle(v, v + 3, v + 2);
            }
        }

        static void Quad(VertexHelper vh, Vector2 min, Vector2 max, Color c)
        {
            if (max.x <= min.x || max.y <= min.y) return;
            int v = vh.currentVertCount;
            vh.AddVert(new Vector2(min.x, min.y), c, Vector2.zero);
            vh.AddVert(new Vector2(min.x, max.y), c, Vector2.zero);
            vh.AddVert(new Vector2(max.x, max.y), c, Vector2.zero);
            vh.AddVert(new Vector2(max.x, min.y), c, Vector2.zero);
            vh.AddTriangle(v, v + 1, v + 2);
            vh.AddTriangle(v, v + 2, v + 3);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (shape != Shape.Elipse) return true;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 p))
                return false;
            Rect r = rectTransform.rect;
            Vector2 d = p - r.center;
            float rx = r.width * 0.5f, ry = r.height * 0.5f;
            if (rx <= 0f || ry <= 0f) return false;
            return (d.x * d.x) / (rx * rx) + (d.y * d.y) / (ry * ry) <= 1f;
        }
    }
}
