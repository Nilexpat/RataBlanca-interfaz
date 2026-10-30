using TMPro;
using UnityEngine;

namespace RataBlanca.Core
{
    /// <summary>
    /// Fuentes y colores de la interfaz, tomados de Figma. El constructor de
    /// escenas (menú Rata Blanca) lee este archivo: si cambias una fuente o
    /// un color aquí y vuelves a construir, se aplica en toda la interfaz.
    /// Archivo: Assets/Data/TemaUI.asset
    /// </summary>
    [CreateAssetMenu(fileName = "TemaUI", menuName = "Rata Blanca/Tema de interfaz")]
    public class UITheme : ScriptableObject
    {
        [Header("Fuentes (TextMeshPro). Si están vacías se usa la fuente por defecto.")]
        [Tooltip("Kiwi Maru — título RATA BLANCA.")]
        public TMP_FontAsset titleFont;
        [Tooltip("Inter — menús y textos.")]
        public TMP_FontAsset bodyFont;
        [Tooltip("Akshar — títulos de los gráficos (cabecera de cada bloque).")]
        public TMP_FontAsset chartFont;
        [Tooltip("Space Mono Bold — textos dentro de los gráficos (como en el archivo de Illustrator).")]
        public TMP_FontAsset chartTextFont;

        [Header("Colores de Figma")]
        public Color background = Color.white;
        public Color titleText = Color.black;
        public Color darkText = new Color32(0x28, 0x28, 0x28, 0xFF);
        public Color menuPlay = new Color32(0x61, 0x11, 0x11, 0xFF);
        public Color menuItem = new Color32(0x6D, 0x48, 0x48, 0xFF);
        public Color menuItemActive = new Color32(0x40, 0x12, 0x12, 0xFF);
        public Color sidebarGlass = new Color32(84, 84, 84, 84);   // rgba(84,84,84,0.33)
        public Color panelGray = new Color32(0xD9, 0xD9, 0xD9, 0xFF);
        public Color blockHeader = new Color32(0xE0, 0xE0, 0xE0, 0xFF);
        public Color blockBorder = new Color32(0x57, 0x57, 0x57, 0xFF);
        public Color accentRed = new Color32(0xE2, 0x64, 0x64, 0xFF);
        public Color fade = new Color32(0x14, 0x10, 0x10, 0xFF);
    }
}
