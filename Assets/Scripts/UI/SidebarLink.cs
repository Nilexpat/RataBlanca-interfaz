using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Opción de la barra lateral. Indica qué sección abre; el
    /// NavigationManager conecta el clic automáticamente.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SidebarLink : MonoBehaviour
    {
        public SectionId target;
        public HoverText hover;

        public Button Button => GetComponent<Button>();
    }
}
