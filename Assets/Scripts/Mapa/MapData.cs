using System;
using System.Collections.Generic;
using UnityEngine;

namespace RataBlanca.Mapa
{
    public enum MapShape { Rectangulo, Elipse }

    /// <summary>Una zona de Rat City.</summary>
    [Serializable]
    public class MapZone
    {
        public string name = "Zona";
        public MapShape shape = MapShape.Rectangulo;
        [Tooltip("Posición y tamaño en píxeles del mapa (x hacia la derecha, y hacia abajo), como en RatCity.png.")]
        public Rect rect = new Rect(100f, 100f, 150f, 150f);
        [Tooltip("Texto que aparece al hacer clic en la zona. Si está vacío, la zona solo se resalta.")]
        [TextArea(2, 8)] public string description = "";

        public MapZone() { }
        public MapZone(string name, MapShape shape, Rect rect, string description = "")
        {
            this.name = name;
            this.shape = shape;
            this.rect = rect;
            this.description = description;
        }
    }

    /// <summary>
    /// Datos del mapa de Rat City / Bio-jaula (sección Mapa).
    /// Archivo: Assets/Data/MapaRatCity.asset
    /// Las medidas vienen de Imagenes/RatCity.png (800×800). Si editas con el
    /// Play activo, el mapa se redibuja al instante y el cambio se conserva.
    /// </summary>
    [CreateAssetMenu(fileName = "MapaRatCity", menuName = "Rata Blanca/Datos del mapa")]
    public class MapData : ScriptableObject
    {
        [Header("Lienzo")]
        [Tooltip("Tamaño del dibujo original del mapa (px).")]
        public Vector2 size = new Vector2(800f, 800f);
        [Tooltip("Borde exterior de la jaula.")]
        public Rect cage = new Rect(68f, 61f, 664f, 694f);

        [Header("Zonas")]
        public List<MapZone> zones = new List<MapZone>
        {
            new MapZone("NIDOS", MapShape.Rectangulo, new Rect(107f, 114f, 155f, 185f),
                "El hogar de todas las ratas, donde están los nidos de cada una. Está dividido en tres secciones: machos, hembras y ancianos."),
            new MapZone("BAÑOS", MapShape.Rectangulo, new Rect(105f, 308f, 155f, 185f),
                "El baño comunitario, al que van todas las ratas a bañarse y a conversar. Es como un sauna público."),
            new MapZone("CATEDRAL", MapShape.Rectangulo, new Rect(581f, 146f, 122f, 129f),
                "Donde las ratas van a rezar, a orar y a rendirles alabanzas a los dioses."),
            new MapZone("OFICINAS", MapShape.Rectangulo, new Rect(551f, 301f, 159f, 146f),
                "El lugar donde trabajan las ratas de alto rango y de mayor poder económico."),
            new MapZone("BAR", MapShape.Rectangulo, new Rect(623f, 512f, 59f, 38f),
                "Un sitio abandonado al que nadie ha ido en mucho tiempo. Aun así, dicen que de vez en cuando se escucha a alguien tomando."),
            new MapZone("PLAYA", MapShape.Elipse, new Rect(94f, 584f, 611f, 156f),
                "A donde suelen ir las ratas a divertirse y a pasarla rico."),
        };

        [Header("Colores")]
        public Color cageFill = new Color(1f, 1f, 1f, 0.35f);
        public Color cageBorder = new Color32(0x57, 0x57, 0x57, 0xFF);
        public Color zoneFill = new Color32(0xE0, 0xE0, 0xE0, 0xF0);
        public Color zoneBorder = new Color32(0x57, 0x57, 0x57, 0xFF);
        [Tooltip("Relleno al pasar el mouse.")]
        public Color zoneHover = new Color32(0xFF, 0x8E, 0x85, 0xFF);
        [Tooltip("Relleno de la zona seleccionada.")]
        public Color zoneSelected = new Color32(0xFF, 0x54, 0x4D, 0xFF);
        public Color textColor = new Color32(0x28, 0x28, 0x28, 0xFF);
        public Color selectedTextColor = Color.white;

        [Header("Estilo")]
        public float cageBorderWidth = 3f;
        public float zoneBorderWidth = 2f;
        public float labelSize = 24f;
        [Tooltip("Cuánto crece una zona al pasar el mouse (1.04 = 4 %).")]
        public float hoverScale = 1.04f;

        [Header("Animación")]
        [Tooltip("Duración de la aparición del mapa al abrir la sección (s).")]
        public float appearDuration = 0.5f;
        [Tooltip("Pausa entre la aparición de una zona y la siguiente (s).")]
        public float zoneStagger = 0.07f;

        [NonSerialized] public int version;
        void OnValidate() => version++;
    }
}
