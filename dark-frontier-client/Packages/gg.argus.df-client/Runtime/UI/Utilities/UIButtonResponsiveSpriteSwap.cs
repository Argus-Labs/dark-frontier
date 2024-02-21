using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.Utilities
{
    public class UIButtonResponsiveSpriteSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDeselectHandler, IPointerUpHandler
    {
        [SerializeField] private Image responsiveImage;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite highlightedSprite;
        private void OnEnable()
        {
            responsiveImage.sprite = defaultSprite;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            responsiveImage.sprite = defaultSprite;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            responsiveImage.sprite = highlightedSprite;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            responsiveImage.sprite = defaultSprite;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            responsiveImage.sprite = defaultSprite;
        }
    }
}
