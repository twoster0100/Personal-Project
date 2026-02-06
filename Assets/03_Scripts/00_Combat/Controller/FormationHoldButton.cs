using UnityEngine;
using UnityEngine.EventSystems;

namespace MyGame.Party
{
    public sealed class FormationHoldButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICancelHandler
    {
        public enum HoldType
        {
            SearchScatter,
            DefenseGather
        }

        [Header("Wiring")]
        [SerializeField] private PartyFormationController formationController;

        [Header("Config")]
        [SerializeField] private HoldType holdType = HoldType.SearchScatter;

        [Tooltip("누른 채로 버튼 영역 밖으로 드래그하면 종료할지")]
        [SerializeField] private bool endOnPointerExit = true;

        private bool _pressed;

        private void Awake()
        {
            if (formationController == null)
                formationController = GetComponentInParent<PartyFormationController>();
        }

        private void OnDisable()
        {
            if (_pressed && formationController != null)
            {
                _pressed = false;
                formationController.EndHold();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (formationController == null) return;

            _pressed = true;

            if (holdType == HoldType.SearchScatter)
                formationController.BeginHoldSearch();
            else
                formationController.BeginHoldDefense();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed) return;
            _pressed = false;

            formationController?.EndHold();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!endOnPointerExit) return;
            if (!_pressed) return;

            _pressed = false;
            formationController?.EndHold();
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (!_pressed) return;

            _pressed = false;
            formationController?.EndHold();
        }
    }
}
