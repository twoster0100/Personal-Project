using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace PartySelection.Feedback
{
    /// <summary>
    /// [Feedback - 연출 전담]
    /// 역할:
    /// - DOTween/MMF를 "어떤 파라미터로" 재생할지 한 곳에 모아둠
    /// - Presenter는 "언제 재생할지"만 결정하고 여기 메서드 호출만 함
    ///
    /// 장점:
    /// - 연출 수정이 생겨도 Presenter 로직은 건드릴 필요가 거의 없음(SRP)
    /// - DOTween/MMF 의존성을 View/Presenter에서 분리
    /// </summary>
    public sealed class PartySelectionFeedback : MonoBehaviour
    {
        [Header("DOTween - Punch Scale")]
        [SerializeField] private float punchScale = 0.12f;
        [SerializeField] private float punchDuration = 0.18f;
        [SerializeField] private int vibrato = 8;
        [SerializeField] private float elasticity = 0.9f;

        [Header("MoreMountains Feel (optional)")]
        [SerializeField] private MMF_Player onSlotSelectedFeedback;
        [SerializeField] private MMF_Player onPartyAssignedFeedback;

        /// <summary>
        /// 슬롯 선택했을 때 연출.
        /// </summary>
        public void PlaySlotSelected(Transform target)
        {
            PlayPunch(target, punchScale);
            if (onSlotSelectedFeedback != null) onSlotSelectedFeedback.PlayFeedbacks();
        }

        /// <summary>
        /// 슬롯에 A/B/C를 "배정"했을 때 연출(조금 더 강하게).
        /// </summary>
        public void PlayPartyAssigned(Transform target)
        {
            PlayPunch(target, punchScale * 1.2f);
            if (onPartyAssignedFeedback != null) onPartyAssignedFeedback.PlayFeedbacks();
        }

        private void PlayPunch(Transform target, float scale)
        {
            if (target == null) return;

            // 기존 트윈 정리(겹침 방지)
            // complete=true로 죽이면 최종값을 반영하고 종료(펀치는 원상복귀되므로 비교적 안전)
            target.DOKill(true);

            // UI는 타임스케일(일시정지) 중에도 반응해야 하는 경우가 많아서 SetUpdate(true)
            target.DOPunchScale(Vector3.one * scale, punchDuration, vibrato, elasticity)
                  .SetUpdate(true);
        }
    }
}
