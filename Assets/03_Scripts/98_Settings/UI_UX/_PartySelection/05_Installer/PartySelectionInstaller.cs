using System;
using PartySelection.Feedback;
using PartySelection.Model;
using PartySelection.Presenter;
using PartySelection.Provider;
using PartySelection.View;
using UnityEngine;

namespace PartySelection.Installer
{
    public sealed class PartySelectionInstaller : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PartySelectionView view;
        [SerializeField] private PartySelectionFeedback feedback;

        [Header("Profile Provider (MonoBehaviour that implements ICharacterProfileProvider)")]
        [SerializeField] private MonoBehaviour profileProviderBehaviour;

        [Header("Initial Slot Character IDs (size must be 4)")]
        [SerializeField]
        private string[] initialSlotCharacterIds = new string[4]
        {
            "Rabbit", "Dragon", "Ox", "Rat"
        };

        /// <summary>
        /// 슬롯 선택이 바뀔 때: (slotIndex, characterId)
        /// 카메라/다른 시스템이 여기 구독하면 UI와 분리된 확장이 가능.
        /// </summary>
        public event Action<int, string> OnSelectedSlotChanged;

        /// <summary>
        /// 슬롯에 배치된 캐릭터가 바뀔 때: (slotIndex, characterId)
        /// "교체 기능" 붙을 때 카메라 갱신에도 사용 가능.
        /// </summary>
        public event Action<int, string> OnSlotCharacterChanged;

        private CharacterSlotPresenter _presenter;
        private CharacterSlotState _state;
        private CharacterRoster _roster;

        private void Awake()
        {
            var provider = profileProviderBehaviour as ICharacterProfileProvider;
            if (provider == null)
            {
                Debug.LogError("[PartySelectionInstaller] profileProviderBehaviour must implement ICharacterProfileProvider");
                enabled = false;
                return;
            }

            if (view == null)
            {
                Debug.LogError("[PartySelectionInstaller] view is missing.");
                enabled = false;
                return;
            }

            // Model/State
            _state = new CharacterSlotState();
            _roster = new CharacterRoster();

            // 초기 4 슬롯 배치
            if (initialSlotCharacterIds == null || initialSlotCharacterIds.Length != CharacterRoster.SlotCount)
            {
                Debug.LogWarning("[PartySelectionInstaller] initialSlotCharacterIds must be size 4. Using fallback ids.");
                initialSlotCharacterIds = new[] { "Rabbit", "Dragon", "Ox", "Rat" };
            }

            for (int i = 0; i < CharacterRoster.SlotCount; i++)
                _roster.SetCharacterId(i, initialSlotCharacterIds[i]);

            // 외부 확장 훅(카메라/기타 시스템용)
            _state.OnSelectedSlotChanged += HandleSelectedSlotChanged;
            _roster.OnSlotCharacterChanged += HandleSlotCharacterChanged;

            _presenter = new CharacterSlotPresenter(view, _state, _roster, provider, feedback);
            _presenter.Bind();

            // 시작 슬롯도 한 번 통지(초기 카메라 세팅 용)
            HandleSelectedSlotChanged(_state.SelectedSlotIndex);
        }

        private void OnDestroy()
        {
            if (_state != null) _state.OnSelectedSlotChanged -= HandleSelectedSlotChanged;
            if (_roster != null) _roster.OnSlotCharacterChanged -= HandleSlotCharacterChanged;

            _presenter?.Unbind();
        }

        private void HandleSelectedSlotChanged(int slotIndex)
        {
            var id = _roster.GetCharacterId(slotIndex);
            OnSelectedSlotChanged?.Invoke(slotIndex, id);
        }

        private void HandleSlotCharacterChanged(int slotIndex, string characterId)
        {
            OnSlotCharacterChanged?.Invoke(slotIndex, characterId);

            // "현재 선택 슬롯"의 캐릭터가 교체되면, 선택 이벤트도 한 번 더 날려서
            // 카메라가 끊기지 않게 즉시 갱신 가능
            if (_state != null && _state.SelectedSlotIndex == slotIndex)
                OnSelectedSlotChanged?.Invoke(slotIndex, characterId);
        }

        public void ReplaceSelectedSlotCharacter(string characterId)
            => _presenter?.ReplaceCharacterInSelectedSlot(characterId);
    }
}
