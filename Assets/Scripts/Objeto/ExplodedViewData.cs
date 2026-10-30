using System;
using System.Collections.Generic;
using UnityEngine;

namespace RataBlanca.Objeto
{
    /// <summary>De qué lado de la punta inicial de la flecha va el texto.</summary>
    public enum LabelSide { Derecha, Izquierda, Abajo, Arriba }

    /// <summary>Una flecha con su texto.</summary>
    [Serializable]
    public class Callout
    {
        [Tooltip("Texto de la etiqueta.")]
        [TextArea(1, 3)] public string text = "Capa";
        [Tooltip("Punto donde empieza la flecha (junto al texto). Píxeles de la imagen de referencia: x hacia la derecha, y hacia abajo.")]
        public Vector2 start;
        [Tooltip("Punto al que apunta la flecha (la pieza).")]
        public Vector2 end;
        [Tooltip("Lado del punto inicial en el que se escribe el texto.")]
        public LabelSide labelSide = LabelSide.Derecha;
        [Tooltip("Ancho máximo del texto antes de pasar a otra línea.")]
        public float labelWidth = 300f;

        public Callout() { }
        public Callout(string text, Vector2 start, Vector2 end, LabelSide side, float width = 300f)
        {
            this.text = text;
            this.start = start;
            this.end = end;
            labelSide = side;
            labelWidth = width;
        }
    }

    /// <summary>
    /// Todo lo configurable de la sección Objeto (el turbante de Madame White).
    /// Archivo: Assets/Data/ObjetoTurbante.asset
    ///
    /// Las coordenadas están en píxeles de la composición de referencia
    /// (Referencias/Exposion.png, 1849×1407): x hacia la derecha y y hacia
    /// abajo, como en Figma o Photoshop. Como es un archivo de datos, lo que
    /// cambies aquí se conserva aunque lo hagas en modo Play, y se ve en vivo.
    /// </summary>
    [CreateAssetMenu(fileName = "ObjetoTurbante", menuName = "Rata Blanca/Datos del objeto (despiece)")]
    public class ExplodedViewData : ScriptableObject
    {
        [Header("Flechas y textos de la tela (una por capa)")]
        public List<Callout> clothCallouts = new List<Callout>
        {
            new Callout("Capa 1", new Vector2(845f, 1145f), new Vector2(642f, 1143f), LabelSide.Derecha),
            new Callout("Capa 2", new Vector2(845f, 1205f), new Vector2(747f, 1160f), LabelSide.Derecha),
            new Callout("Capa 3", new Vector2(845f, 1265f), new Vector2(672f, 1231f), LabelSide.Derecha),
        };

        [Header("Flecha y texto del rubí")]
        public List<Callout> rubyCallouts = new List<Callout>
        {
            new Callout("Componente", new Vector2(1345f, 1298f), new Vector2(1318f, 1122f), LabelSide.Abajo, 320f),
        };

        [Header("Estilo de flechas y textos")]
        public Color arrowColor = new Color32(0x40, 0x12, 0x12, 0xFF);
        [Range(1f, 16f)] public float arrowThickness = 4f;
        [Range(0f, 60f)] public float arrowHeadLength = 22f;
        [Range(0f, 60f)] public float arrowHeadWidth = 20f;
        public Color textColor = new Color32(0x40, 0x12, 0x12, 0xFF);
        [Range(16f, 80f)] public float textSize = 42f;
        [Tooltip("Separación entre la punta de la flecha y el texto.")]
        public float labelGap = 12f;

        [Header("Línea de la lupa (del rubí al círculo)")]
        public Vector2 magnifierLineStart = new Vector2(985f, 672f);
        public Vector2 magnifierLineEnd = new Vector2(1246f, 961f);
        public Color magnifierLineColor = new Color32(0x6D, 0x48, 0x48, 0xFF);
        [Range(1f, 12f)] public float magnifierLineThickness = 3f;
        public float dashLength = 16f;
        public float dashGap = 10f;
        [Tooltip("Mostrar un anillo fino alrededor del rubí cuando se abre la lupa.")]
        public bool showRing = true;
        [Tooltip("Centro y diámetro del anillo que rodea el rubí al abrir la lupa.")]
        public Vector2 ringCenter = new Vector2(901f, 579f);
        public float ringDiameter = 260f;

        [Header("Brillo de las zonas interactivas")]
        public Color clothGlowColor = new Color32(0xFF, 0xD2, 0x3F, 0xFF);
        public Color rubyGlowColor = new Color32(0xFF, 0xD2, 0x3F, 0xFF);
        [Tooltip("Intensidad mínima del pulso.")]
        [Range(0f, 1f)] public float glowMin = 0.25f;
        [Tooltip("Intensidad máxima del pulso.")]
        [Range(0f, 1f)] public float glowMax = 0.9f;
        [Tooltip("Intensidad al pasar el mouse.")]
        [Range(0f, 1f)] public float glowHover = 1f;
        [Tooltip("Intensidad mientras la vista está abierta.")]
        [Range(0f, 1f)] public float glowWhenOpen = 0.15f;
        [Tooltip("Pulsos por segundo.")]
        [Range(0.1f, 4f)] public float pulseSpeed = 0.8f;
        [Tooltip("Qué tan rápido sube o baja el brillo al entrar o salir con el mouse.")]
        public float hoverResponse = 10f;

        [Header("Animación (segundos)")]
        [Tooltip("Aparición de la tela y de la lupa (escala y desvanecido).")]
        public float appearDuration = 0.5f;
        [Tooltip("Escala inicial de la pieza al aparecer.")]
        [Range(0.1f, 1f)] public float appearStartScale = 0.6f;
        [Tooltip("Dibujo de la línea de la lupa.")]
        public float magnifierLineDuration = 0.35f;
        [Tooltip("Dibujo de cada flecha.")]
        public float arrowDuration = 0.4f;
        [Tooltip("Pausa entre una flecha y la siguiente.")]
        public float arrowStagger = 0.12f;
        [Tooltip("Aparición de cada texto.")]
        public float textFadeDuration = 0.3f;
        [Tooltip("Cerrar es más rápido que abrir: 2 = el doble de rápido.")]
        [Range(1f, 4f)] public float closeSpeed = 1.8f;

        /// <summary>Aumenta cada vez que se edita en el Inspector (para refrescar en vivo).</summary>
        [NonSerialized] public int version;

        void OnValidate() => version++;
    }
}
