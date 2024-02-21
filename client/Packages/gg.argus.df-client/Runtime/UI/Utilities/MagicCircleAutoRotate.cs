using UnityEngine;

namespace ArgusLabs.DF.UI.Utilities
{
    [ExecuteInEditMode]
    public class MagicCircleAutoRotate : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed;

        private bool _isRotating = true;

        public void Stop()
        {
            _isRotating = false;
        }

        public void Start()
        {
            _isRotating = true;
        }

        private void Update()
        {
            if (_isRotating)
            {
                GetComponent<RectTransform>().Rotate(0, 0, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
