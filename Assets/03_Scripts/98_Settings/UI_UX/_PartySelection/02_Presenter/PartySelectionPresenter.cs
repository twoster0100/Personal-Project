using PartySelection.Feedback;
using PartySelection.Model;
using PartySelection.Provider;
using PartySelection.View;

namespace PartySelection.Presenter
{
    /// <summary>
    /// [Presenter - 흐름/결정/구독]
    /// 하는 일:
    /// 1) View 이벤트 구독 (슬롯 클릭 등)
    /// 2) Model/State 갱신 (SelectedSlot, Roster 배정 등)
    /// 3) View 갱신 (이름/초상화/선택 표시)
    /// 4) Feedback 트리거 (DOTween/MMF)
    ///
    /// 중요한 규칙:
    /// - Presenter는 Unity UI 컴포넌트에 직접 접근하지 않음 => View를 통해서만 조작
    /// - Presenter는 데이터 원천을 모름 => IStatsProvider로만 요청(DIP)
    /// </summary>
    public sealed class PartySelectionPresenter
    {
        private readonly PartySelectionView _view;
        private readonly PartySelectionState _state;
        private readonly PartyRoster _roster;
        private readonly IStatsProvider _statsProvider;
        private readonly PartySelectionFeedback _feedback;

        public PartySelectionPresenter(
            PartySelectionView view,
            PartySelectionState state,
            PartyRoster roster,
            IStatsProvider statsProvider,
            PartySelectionFeedback feedback)
        {
            _view = view;
            _state = state;
            _roster = roster;
            _statsProvider = statsProvider;
            _feedback = feedback;
        }

        /// <summary>
        /// 이벤트 구독 시작. (Installer.Awake에서 호출)
        /// </summary>
        public void Bind()
        {
            _view.OnSlotClicked += HandleSlotClicked;
            _state.OnSelectedSlotChanged += HandleSelectedSlotChanged;

            // 최초 UI 동기화
            SyncAll();
        }

        /// <summary>
        /// 이벤트 구독 해제. (Installer.OnDestroy에서 호출)
        /// - 씬 전환/파괴 시 메모리 누수/중복 구독 방지
        /// </summary>
        public void Unbind()
        {
            _view.OnSlotClicked -= HandleSlotClicked;
            _state.OnSelectedSlotChanged -= HandleSelectedSlotChanged;
        }

        /// <summary>
        /// 외부 UI(예: A/B/C 버튼)가 "현재 선택 슬롯에 파티 배정"을 요청할 때 쓰는 API.
        /// - Installer에서 public 메서드로 노출해서 Button OnClick으로 연결 가능
        /// </summary>
        public void AssignPartyToSelectedSlot(PartyType party)
        {
            int slot = _state.SelectedSlotIndex;

            // 1) 모델 갱신
            _roster.SetPartyAt(slot, party);

            // 2) UI 갱신
            UpdateProfileUI(slot);

            // 3) 피드백
            _feedback?.PlayPartyAssigned(_view.GetSlotTransform(slot));
        }

        private void HandleSlotClicked(int slotIndex)
        {
            // 클릭 즉시 피드백(선택이 바뀌지 않아도 클릭 반응은 주고 싶을 수 있음)
            _feedback?.PlaySlotSelected(_view.GetSlotTransform(slotIndex));

            // 상태 변경(실제로 선택이 바뀌면 OnSelectedSlotChanged가 호출됨)
            _state.SelectSlot(slotIndex);
        }

        private void HandleSelectedSlotChanged(int slotIndex)
        {
            // 선택 비주얼 갱신
            _view.SetSlotSelectedVisual(slotIndex);

            // 선택된 슬롯의 프로필로 갱신
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
            // 슬롯에 배정된 파티(A/B/C)를 가져오고
            PartyType party = _roster.GetPartyAt(slotIndex);

            // 외부 데이터 공급자에게 "보여줄 프로필"을 요청
            var profile = _statsProvider.GetProfile(party, slotIndex);

            // View에 표시
            _view.SetUserName(profile.DisplayName);
            _view.SetPortrait(profile.PortraitSprite);
        }
    }
}
