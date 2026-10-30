using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RataBlanca.Audio
{
    /// <summary>
    /// Pon este componente en cualquier botón para que suene al pasar el
    /// mouse (hover) y al hacer clic. Los sonidos salen del AudioManager.
    /// </summary>
    public class UISound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public bool playHover = true;
        public bool playClick = true;

        Selectable _selectable;

        void Awake() => _selectable = GetComponent<Selectable>();

        bool Interactable => _selectable == null || _selectable.IsInteractable();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!playHover || !Interactable || AudioManager.Instance == null) return;
            AudioManager.Instance.PlayHover();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!playClick || !Interactable || AudioManager.Instance == null) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            AudioManager.Instance.PlayClick();
        }
    }
}
