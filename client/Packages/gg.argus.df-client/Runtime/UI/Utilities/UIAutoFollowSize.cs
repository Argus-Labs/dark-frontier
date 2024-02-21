using UnityEngine;

namespace ArgusLabs.DF.UI.Utilities
{
    public class UIAutoFollowSize : MonoBehaviour
    {
        [SerializeField] private RectTransform _transformToFollow;
        [SerializeField] private bool _followWidth;
        [SerializeField] private bool _followHeight;
        [SerializeField] private Vector2 _offset;
        [SerializeField] private Vector2 _maxSize;
        [SerializeField] private Vector2 _minSize;

        private RectTransform _thisRect;
        private Vector2 _lastUpdateFollowedSize;

        private void Awake()
        {
            _thisRect= GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            UpdateSize();
        }

        private void Update()
        {
            if (_followWidth && (Mathf.Approximately(_transformToFollow.sizeDelta.x, _lastUpdateFollowedSize.x) || 
                _lastUpdateFollowedSize.x + _offset.x > _maxSize.x && _transformToFollow.sizeDelta.x + _offset.x > _maxSize.x)) return;
            if (_followHeight && (Mathf.Approximately(_transformToFollow.sizeDelta.y, _lastUpdateFollowedSize.y) ||
                _lastUpdateFollowedSize.y + _offset.y > _maxSize.y && _transformToFollow.sizeDelta.y + _offset.y > _maxSize.y)) return;

            UpdateSize();
        }

        private void UpdateSize()
        {
            Vector2 targetSize = _thisRect.sizeDelta;
            if (_followHeight)
            {
                targetSize.y = _transformToFollow.sizeDelta.y + _offset.y;
                if(targetSize.y > _maxSize.y ) { targetSize.y = _maxSize.y; }
                if(targetSize.y < _minSize.y) { targetSize.y = _minSize.y; }
            }
            if (_followWidth)
            {
                targetSize.x = _transformToFollow.sizeDelta.x + _offset.x;
                if (targetSize.x > _maxSize.x) { targetSize.x = _maxSize.x; }
                if (targetSize.x < _minSize.x) { targetSize.x = _minSize.x; }
            }
            _thisRect.sizeDelta = targetSize;
            _lastUpdateFollowedSize = _transformToFollow.sizeDelta;
        }

    }
}
