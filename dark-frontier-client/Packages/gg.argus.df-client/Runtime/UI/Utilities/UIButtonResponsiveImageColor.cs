using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.Utilities
{
    public class UIButtonResponsiveImageColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDeselectHandler, IPointerUpHandler
    {
        [SerializeField] private Image responsiveImage;
        [SerializeField] private Color defaultColor;
        [SerializeField] private Color highlightedColor;
        [SerializeField] private Color pressedColor;

        private bool _isInside;

        private void OnEnable()
        {
            responsiveImage.color = defaultColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            responsiveImage.color = pressedColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isInside)
            {
                responsiveImage.color = highlightedColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isInside = true;
            responsiveImage.color = highlightedColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isInside = false;
            responsiveImage.color = defaultColor;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            responsiveImage.color = defaultColor;
        }
    }
}
