using UnityEngine;

namespace ChessMiniDemo
{
    public class Square : MonoBehaviour
    {
        private Renderer _renderer;
        private Color _baseColor;
        private bool _isHighlighted;

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _baseColor = _renderer.material.color;
            }
        }

        public void SetBaseColor(Color color)
        {
            _baseColor = color;
            if (!_isHighlighted && _renderer != null)
            {
                _renderer.material.color = _baseColor;
            }
        }

        public void Highlight(Color color)
        {
            if (_renderer == null) return;
            _renderer.material.color = color;
            _isHighlighted = true;
        }

        public void Unhighlight()
        {
            if (_renderer == null) return;
            _renderer.material.color = _baseColor;
            _isHighlighted = false;
        }
    }
}
