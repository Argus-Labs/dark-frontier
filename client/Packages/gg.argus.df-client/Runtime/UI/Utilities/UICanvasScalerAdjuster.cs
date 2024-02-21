using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.Utilities
{
    [RequireComponent(typeof(CanvasScaler))]
    public class UICanvasScalerAdjuster : MonoBehaviour
    {
        CanvasScaler _scaler;
        Vector2 _lastResolution;

        // Start is called before the first frame update
        void Start()
        {
            _scaler = GetComponent<CanvasScaler>();
            _scaler.matchWidthOrHeight = (float)Screen.width / Screen.height > 16f / 9f ? 0 : 1;

            _lastResolution = new Vector2(Screen.width, Screen.height);
        }

        public void ResetAndTurnOff()
        {
            _scaler.matchWidthOrHeight = 0.5f;
            enabled = false;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if(Screen.width != _lastResolution.x || Screen.height != _lastResolution.y)
            {
                _scaler.matchWidthOrHeight = (float)Screen.width / Screen.height > 16f / 9f ? 0 : 1;

                _lastResolution = new Vector2(Screen.width, Screen.height);
            }
        }
    }
}
