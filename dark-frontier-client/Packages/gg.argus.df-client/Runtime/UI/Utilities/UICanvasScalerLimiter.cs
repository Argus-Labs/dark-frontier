using UnityEngine;
using UnityEngine.UI;

namespace ArgusLabs.DF.UI.Utilities
{
    [RequireComponent(typeof(CanvasScaler))]
    public class UICanvasScalerLimiter : MonoBehaviour
    {
        CanvasScaler _scaler;
        Vector2 _lastResolution;
        const float WIDTH_LIMIT = 2560;
        const float HEIGHT_LIMIT = 1440;

        void Start()
        {
            _scaler = GetComponent<CanvasScaler>();
            _lastResolution = new Vector2(Screen.width, Screen.height);

            if (_lastResolution.x > WIDTH_LIMIT || _lastResolution.y > HEIGHT_LIMIT)
            {
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            }
            else
            {
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (Screen.width != _lastResolution.x || Screen.height != _lastResolution.y)
            {
                if (_scaler.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize && (_lastResolution.x > WIDTH_LIMIT || _lastResolution.y > HEIGHT_LIMIT))
                {
                    _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                }
                else if(_scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize && (_lastResolution.x < WIDTH_LIMIT && _lastResolution.y < HEIGHT_LIMIT))
                {
                    _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    _scaler.matchWidthOrHeight = 0.5f;
                }

                _lastResolution = new Vector2(Screen.width, Screen.height);
            }
        }
    }
}
