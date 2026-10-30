using System;
using System.Collections.Generic;
using UnityEngine;

namespace RataBlanca.Graficos
{
    public enum ChartType { Torta, BarrasVerticales, BarrasHorizontales, Lineas }

    /// <summary>Qué se escribe sobre cada porción de la torta.</summary>
    public enum PieLabelMode
    {
        [Tooltip("El número tal como está en los datos (más el sufijo).")]
        Valor,
        [Tooltip("El porcentaje real que ocupa la porción.")]
        Porcentaje,
        Ninguno
    }

    /// <summary>Un dato de torta o de barras.</summary>
    [Serializable]
    public class ChartEntry
    {
        public string label = "Dato";
        public float value = 1f;
        public Color color = new Color32(0xFF, 0x54, 0x4D, 0xFF);

        public ChartEntry() { }
        public ChartEntry(string label, float value, Color color)
        {
            this.label = label;
            this.value = value;
            this.color = color;
        }
    }

    /// <summary>Una serie de un gráfico de líneas: puntos (x, y).</summary>
    [Serializable]
    public class ChartSeries
    {
        public string name = "Serie";
        public Color color = new Color32(0xFF, 0x00, 0x00, 0xFF);
        public List<Vector2> points = new List<Vector2>();

        public ChartSeries() { }
        public ChartSeries(string name, Color color, params Vector2[] points)
        {
            this.name = name;
            this.color = color;
            this.points = new List<Vector2>(points);
        }
    }

    /// <summary>
    /// Un gráfico de la sección Misterios. Todo se edita desde el Inspector:
    /// tipo, título, datos, ejes, leyenda y animación.
    /// Archivos: Assets/Data/Graficos/*.asset
    /// Para agregar un gráfico nuevo: clic derecho → Create → Rata Blanca →
    /// Gráfico, y arrástralo a la lista "Charts" de la sección Misterios.
    /// Si editas un gráfico con el Play activo, se redibuja al instante.
    /// </summary>
    [CreateAssetMenu(fileName = "Grafico", menuName = "Rata Blanca/Gráfico")]
    public class ChartData : ScriptableObject
    {
        [Header("General")]
        public ChartType type = ChartType.BarrasVerticales;
        [Tooltip("Título que aparece en la cabecera del bloque.")]
        [TextArea(1, 3)] public string title = "Título del gráfico";
        [Tooltip("Ícono propio (opcional). Si está vacío se usa el del tipo de gráfico.")]
        public Sprite icon;

        [Header("Datos (torta y barras)")]
        public List<ChartEntry> entries = new List<ChartEntry>();

        [Header("Series (líneas)")]
        public List<ChartSeries> series = new List<ChartSeries>();

        [Header("Ejes")]
        public string xAxisLabel = "";
        public string yAxisLabel = "";
        [Tooltip("Valor máximo del eje Y (0 = automático).")]
        public float yMax = 0f;
        [Tooltip("Separación de las marcas del eje Y (líneas).")]
        public float yStep = 0f;
        public float xMin = 0f;
        public float xMax = 10f;
        [Tooltip("Separación de las marcas del eje X (líneas).")]
        public float xStep = 1f;
        public Color axisColor = new Color32(0x79, 0x2B, 0x2B, 0xFF);
        [Tooltip("Texto que se agrega después de cada valor (por ejemplo %).")]
        public string valueSuffix = "";
        [Tooltip("Mostrar el valor de cada barra.")]
        public bool showValues = true;

        [Header("Leyenda")]
        public bool showLegend = true;

        [Header("Torta")]
        [Tooltip("Tamaño del hueco central (0 = torta llena, 0.46 = dona como en el diseño).")]
        [Range(0f, 0.9f)] public float innerRadius = 0.46f;
        [Tooltip("Ángulo donde empieza la primera porción (0 = arriba, sentido horario).")]
        public float startAngle = 0f;
        [Tooltip("Separación blanca entre porciones (px).")]
        public float sliceGap = 4f;
        public PieLabelMode pieLabels = PieLabelMode.Valor;
        [Tooltip("Porciones más pequeñas que esto (en grados) no llevan texto, para que no se amontone.")]
        public float minLabelAngle = 9f;

        [Header("Animación")]
        [Tooltip("Duración de la animación del gráfico al desplegarse (s).")]
        public float duration = 1.2f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Desfase entre barras/puntos (0 = todos a la vez).")]
        [Range(0f, 0.3f)] public float stagger = 0.12f;

        [Header("Bloque")]
        [Tooltip("Alto del área del gráfico cuando el bloque está abierto (px).")]
        public float bodyHeight = 380f;

        /// <summary>Aumenta cada vez que se edita en el Inspector (para redibujar en vivo).</summary>
        [NonSerialized] public int version;
        void OnValidate() => version++;
    }
}
