using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArgusLabs.DF.UI.Utilities
{
    public class UIChangeTextOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI responsiveText;

        [SerializeField] private string defaultText;

        [SerializeField] private string onHoveredText;

        private void OnEnable()
        {
            responsiveText.text = defaultText;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            responsiveText.text = onHoveredText;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (EventSystem.current.currentSelectedGameObject != gameObject)
            {
                responsiveText.text = defaultText;
            }
        }
    }
}
