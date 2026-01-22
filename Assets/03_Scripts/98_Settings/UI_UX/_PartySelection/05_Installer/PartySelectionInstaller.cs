using PartySelection.Feedback;
using PartySelection.Model;
using PartySelection.Presenter;
using PartySelection.Provider;
using PartySelection.View;
using UnityEngine;

namespace PartySelection.Installer
{
    /// <summary>
    /// [Installer - 씬 배선]
    /// 역할:
    /// - Presenter/Model/State를 생성하고,
    /// - View/Feedback/Provider를 연결해 "한 덩어리로" 동작하게 만든다.
    ///
    /// 왜 필요한가?
    /// - Presenter는 MonoBehaviour가 아니라 순수 C# 클래스라서,
    ///   씬 오브젝트 참조를 인스펙터에서 직접 받을 수 없다.
    /// - 그래서 Installer(MonoBehaviour)가 인스펙터로 레퍼런스를 받고,
    ///   Presenter에 주입해준다(DI의 아주 가벼운 형태).
    /// </summary>
    public sealed class PartySelectionInstaller : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PartySelectionView view;
        [SerializeField] private PartySelectionFeedback feedback;

        [Header("Stats Provider (MonoBehaviour that implements IStatsProvider)")]
        [SerializeField] private MonoBehaviour statsProviderBehaviour;

        private PartySelectionPresenter _presenter;

        private void Awake()
        {
            // Provider 유효성 체크
            var statsProvider = statsProviderBehaviour as IStatsProvider;
            if (statsProvider == null)
            {
                Debug.LogError("[PartySelectionInstaller] statsProviderBehaviour must implement IStatsProvider");
                enabled = false;
                return;
            }

            if (view == null)
            {
                Debug.LogError("[PartySelectionInstaller] view is missing.");
                enabled = false;
                return;
            }

            // Model/State 생성 (MonoBehaviour로 만들 필요 없음)
            var state = new PartySelectionState();
            var roster = new PartyRoster();

            // 초기 배정(원하면 네 룰로 변경)
            roster.SetPartyAt(0, PartyType.A);
            roster.SetPartyAt(1, PartyType.B);
            roster.SetPartyAt(2, PartyType.C);
            roster.SetPartyAt(3, PartyType.A);

            // Presenter 생성 + 바인딩
            _presenter = new PartySelectionPresenter(view, state, roster, statsProvider, feedback);
            _presenter.Bind();
        }

        private void OnDestroy()
        {
            // 씬 종료/오브젝트 파괴 시 구독 해제
            _presenter?.Unbind();
        }

        // ---- 외부 UI(예: A/B/C 버튼)에서 OnClick으로 호출하기 쉬운 public API ----
        public void SelectPartyA() => _presenter?.AssignPartyToSelectedSlot(PartyType.A);
        public void SelectPartyB() => _presenter?.AssignPartyToSelectedSlot(PartyType.B);
        public void SelectPartyC() => _presenter?.AssignPartyToSelectedSlot(PartyType.C);
    }
}
