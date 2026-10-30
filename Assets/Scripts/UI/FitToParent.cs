using UnityEngine;

namespace RataBlanca.UI
{
    /// <summary>
    /// Escala una composición de tamaño fijo (por ejemplo 1849×1407, la
    /// imagen de referencia del turbante) para que quepa entera en su padre,
    /// sin deformarse. Así se adapta al tamaño de la ventana y a la barra
    /// lateral abierta o cerrada.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class FitToParent : MonoBehaviour
    {
        public Vector2 designSize = new Vector2(1849f, 1407f);
        [Tooltip("Escala máxima (1 = tamaño original).")]
        public float maxScale = 1f;

        void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            if (parent == null || designSize.x <= 0f || designSize.y <= 0f) return;

            Rect r = parent.rect;
            float s = Mathf.Min(r.width / designSize.x, r.height / designSize.y, maxScale);
            if (s <= 0f) return;
            if (Mathf.Abs(transform.localScale.x - s) > 0.0001f)
                transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
