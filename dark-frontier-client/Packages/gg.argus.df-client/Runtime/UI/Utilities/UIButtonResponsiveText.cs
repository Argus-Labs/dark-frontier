using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArgusLabs.DF.UI.Utilities
{

    //put this on object with button component, this will change the color of a textmeshpro when button is highlighted
    public class UIButtonResponsiveText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDeselectHandler, IPointerUpHandler
    {
        [SerializeField] private TextMeshProUGUI responsiveText;
        [SerializeField] private Color defaultColor;
        [SerializeField] private Color highlightedColor;
        [SerializeField] private Color pressedColor;

        private void OnEnable()
        {
            responsiveText.color = defaultColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            responsiveText.color = pressedColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            responsiveText.color = defaultColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            responsiveText.color = highlightedColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (EventSystem.current.currentSelectedGameObject != gameObject)
            {
                responsiveText.color = defaultColor;
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            responsiveText.color = defaultColor;
        }
    }
}
