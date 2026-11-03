using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RataBlanca.Mapa
{
    /// <summary>Eventos del mouse sobre una zona del mapa.</summary>
    public class MapZoneEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action Entered;
        public Action Exited;
        public Action Clicked;

        public void OnPointerEnter(PointerEventData e) => Entered?.Invoke();
        public void OnPointerExit(PointerEventData e) => Exited?.Invoke();
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left) Clicked?.Invoke();
        }
        void OnDisable() => Exited?.Invoke();
    }
}
