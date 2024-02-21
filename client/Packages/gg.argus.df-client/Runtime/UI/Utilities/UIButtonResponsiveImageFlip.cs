using UnityEngine;
using UnityEngine.EventSystems;

namespace ArgusLabs.DF.UI.Utilities
{
    public class UIButtonResponsiveImageFlip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IDeselectHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _responsiveImageTransform;
        [SerializeField] private Vector3 _defaultScale;
        [SerializeField] private Vector3 _highlightedScale;

        private void OnEnable()
        {
            _responsiveImageTransform.localScale = _defaultScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _responsiveImageTransform.localScale = _highlightedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _responsiveImageTransform.localScale = _defaultScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _responsiveImageTransform.localScale = _highlightedScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _responsiveImageTransform.localScale = _defaultScale;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _responsiveImageTransform.localScale = _defaultScale;
        }
    }
}
