using PartySelection.Feedback;
using PartySelection.Model;
using PartySelection.Provider;
using PartySelection.View;

namespace PartySelection.Presenter
{
    /// <summary>
    /// 슬롯(0~3) = 캐릭터 4명 UI Presenter
    /// </summary>
    public sealed class CharacterSlotPresenter
    {
        private readonly PartySelectionView _view;
        private readonly CharacterSlotState _state;
        private readonly CharacterRoster _roster;
        private readonly ICharacterProfileProvider _profileProvider;
        private readonly PartySelectionFeedback _feedback;

        public CharacterSlotPresenter(
            PartySelectionView view,
            CharacterSlotState state,
            CharacterRoster roster,
            ICharacterProfileProvider profileProvider,
            PartySelectionFeedback feedback)
        {
            _view = view;
            _state = state;
            _roster = roster;
            _profileProvider = profileProvider;
            _feedback = feedback;
        }

        public void Bind()
        {
            _view.OnSlotClicked += HandleSlotClicked;
            _state.OnSelectedSlotChanged += HandleSelectedSlotChanged;
            SyncAll();
        }

        public void Unbind()
        {
            _view.OnSlotClicked -= HandleSlotClicked;
            _state.OnSelectedSlotChanged -= HandleSelectedSlotChanged;
        }

        /// <summary>
        /// (확장용) 선택 슬롯의 캐릭터를 교체하는 API.
        /// - 나중에 "캐릭터 선택창" 붙이면 이걸 호출하면 됨.
        /// </summary>
        public void ReplaceCharacterInSelectedSlot(string newCharacterId)
        {
            int slot = _state.SelectedSlotIndex;
            _roster.SetCharacterId(slot, newCharacterId);

            UpdateProfileUI(slot);
            _feedback?.PlayPartyAssigned(_view.GetSlotTransform(slot)); // '교체됨' 강한 피드백 재사용
        }

        private void HandleSlotClicked(int slotIndex)
        {
            _feedback?.PlaySlotSelected(_view.GetSlotTransform(slotIndex));
            _state.SelectSlot(slotIndex);
        }

        private void HandleSelectedSlotChanged(int slotIndex)
        {
            _view.SetSlotSelectedVisual(slotIndex);
            UpdateProfileUI(slotIndex);
        }

        private void SyncAll()
        {
            int slot = _state.SelectedSlotIndex;
            _view.SetSlotSelectedVisual(slot);
            UpdateProfileUI(slot);
        }

        private void UpdateProfileUI(int slotIndex)
        {
            string id = _roster.GetCharacterId(slotIndex);
            var profile = _profileProvider.GetProfile(id);

            _view.SetUserName(profile.DisplayName);
            _view.SetPortrait(profile.Portrait);
        }
    }
}
