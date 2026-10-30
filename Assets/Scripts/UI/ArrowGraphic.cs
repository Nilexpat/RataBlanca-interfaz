using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Flecha (o línea punteada) dibujada con uGUI, sin imágenes.
    /// Los puntos se dan en píxeles desde la esquina superior izquierda del
    /// rect (y hacia abajo), igual que en Figma. "progress" va de 0 a 1 y
    /// permite animar la flecha como si se estuviera dibujando.
    /// El rect de este objeto debe cubrir toda la composición.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ArrowGraphic : MaskableGraphic
    {
        [SerializeField] Vector2 start;
        [SerializeField] Vector2 end = new Vector2(100f, 0f);
        [SerializeField] float thickness = 4f;
        [SerializeField] bool showHead = true;
        [SerializeField] float headLength = 22f;
        [SerializeField] float headWidth = 20f;
        [SerializeField] bool dashed;
        [SerializeField] float dashLength = 16f;
        [SerializeField] float dashGap = 10f;
        [SerializeField, Range(0f, 1f)] float progress = 1f;

        public Vector2 StartPoint { get => start; set { start = value; SetVerticesDirty(); } }
        public Vector2 EndPoint { get => end; set { end = value; SetVerticesDirty(); } }
        public float Thickness { get => thickness; set { thickness = value; SetVerticesDirty(); } }
        public bool ShowHead { get => showHead; set { showHead = value; SetVerticesDirty(); } }
        public float HeadLength { get => headLength; set { headLength = value; SetVerticesDirty(); } }
        public float HeadWidth { get => headWidth; set { headWidth = value; SetVerticesDirty(); } }
        public bool Dashed { get => dashed; set { dashed = value; SetVerticesDirty(); } }
        public float DashLength { get => dashLength; set { dashLength = value; SetVerticesDirty(); } }
        public float DashGap { get => dashGap; set { dashGap = value; SetVerticesDirty(); } }

        public float Progress
        {
            get => progress;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, progress)) return;
                progress = value;
                SetVerticesDirty();
            }
        }

        protected override void Awake()
        {
            // Sin CanvasRenderer el gráfico no se dibuja. Las escenas creadas
            // antes de este arreglo no lo tienen, así que se agrega aquí.
            if (!TryGetComponent<CanvasRenderer>(out _)) gameObject.AddComponent<CanvasRenderer>();
            base.Awake();
            raycastTarget = false;
        }

        Vector2 ToLocal(Vector2 p)
        {
            Rect r = rectTransform.rect;
            return new Vector2(r.xMin + p.x, r.yMax - p.y);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (progress <= 0f) return;

            Vector2 a = ToLocal(start);
            Vector2 b = ToLocal(end);
            Vector2 dir = b - a;
            float length = dir.magnitude;
            if (length < 0.01f) return;
            dir /= length;

            float drawn = length * progress;
            float head = showHead ? Mathf.Min(headLength, length * 0.5f) : 0f;
            Vector2 tip = a + dir * drawn;
            float shaftLength = Mathf.Max(0f, drawn - head * 0.85f);

            // Cuerpo (continuo o punteado)
            if (dashed)
            {
                float step = Mathf.Max(1f, dashLength + dashGap);
                for (float d = 0f; d < shaftLength; d += step)
                {
                    float e = Mathf.Min(d + dashLength, shaftLength);
                    AddSegment(vh, a + dir * d, a + dir * e, thickness);
                }
            }
            else if (shaftLength > 0f)
            {
                AddSegment(vh, a, a + dir * shaftLength, thickness);
            }

            // Punta: se mueve con la línea mientras se dibuja
            if (showHead && drawn >= head * 0.5f)
            {
                Vector2 normal = new Vector2(-dir.y, dir.x);
                Vector2 baseCenter = tip - dir * head;
                AddTriangle(vh, tip, baseCenter + normal * (headWidth * 0.5f), baseCenter - normal * (headWidth * 0.5f));
            }
        }

        void AddSegment(VertexHelper vh, Vector2 p0, Vector2 p1, float width)
        {
            Vector2 d = p1 - p0;
            if (d.sqrMagnitude < 0.0001f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * (width * 0.5f);
            int i = vh.currentVertCount;
            vh.AddVert(p0 + n, color, Vector2.zero);
            vh.AddVert(p1 + n, color, Vector2.zero);
            vh.AddVert(p1 - n, color, Vector2.zero);
            vh.AddVert(p0 - n, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        void AddTriangle(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            int i = vh.currentVertCount;
            vh.AddVert(p0, color, Vector2.zero);
            vh.AddVert(p1, color, Vector2.zero);
            vh.AddVert(p2, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }
    }
}
