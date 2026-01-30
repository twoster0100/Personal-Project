using System;
using UnityEngine;
using UnityEngine.UI;

namespace MyGame.Presentation.Settings
{
    public sealed class ToggleAdapter : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;

        public event Action<bool> Changed;

        public bool Value => _toggle != null && _toggle.isOn;

        private void Reset()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            if (_toggle != null)
                _toggle.onValueChanged.AddListener(OnChanged);
        }

        private void OnDisable()
        {
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(OnChanged);
        }

        private void OnChanged(bool v) => Changed?.Invoke(v);
    }
}
