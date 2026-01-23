using Cinemachine;
using DG.Tweening;
using UnityEngine;

namespace PartySelection.Camera
{
    /// <summary>
    /// FollowProxy의 "단일 주인" 역할.
    /// - VCam_Party는 항상 FollowProxy를 Follow
    /// - 슬롯 전환 시 FollowProxy를 DOTween으로 "슉" 이동
    /// - 평소에는 FollowProxy를 현재 Pivot에 붙여서(추적) 시점 유지
    /// - FollowProxy에 LookAheadFollowProxy가 붙어있으면, 슬롯 전환 시 Target을 자동 갱신
    /// </summary>
    public sealed class PartyCameraDirectorSwoosh : MonoBehaviour
    {
        [Header("Single VCam")]
        [SerializeField] private CinemachineVirtualCamera vcam;

        [Header("Look-ahead Proxy (VCam always follows this)")]
        [SerializeField] private Transform followProxy;

        [Header("Optional: LookAhead controller on FollowProxy")]
        [Tooltip("FollowProxy에 LookAheadFollowProxy가 붙어있으면 자동으로 Target을 갱신합니다. 비워두면 FollowProxy에서 자동 탐색합니다.")]
        [SerializeField] private LookAheadFollowProxy lookAhead;

        [Header("Party Camera Pivots (order = slots 0~3)")]
        [SerializeField] private Transform[] cameraPivots = new Transform[4];

        [Header("Start Focus")]
        [SerializeField] private int startIndex = 0;

        [Header("Swoosh Transition (DOTween)")]
        [SerializeField] private bool useSwoosh = true;
        [SerializeField] private float travelSpeed = 25f;
        [SerializeField] private float minDuration = 0.10f;
        [SerializeField] private float maxDuration = 0.30f;
        [SerializeField] private Ease ease = Ease.OutCubic;

        [Header("Keep Tracking While Idle")]
        [Tooltip("슉 이동이 끝난 후에는 FollowProxy를 Pivot에 계속 붙여서 시점이 유지되게 함(룩어헤드가 있으면 Target 유지로 동작)")]
        [SerializeField] private bool keepProxyLockedToPivot = true;

        private int _currentIndex;
        private Tween _moveTween;
        private bool _isTweening;
        private bool _killForNewFocus;

        private void Awake()
        {
            _currentIndex = Mathf.Clamp(startIndex, 0, 3);

            if (vcam == null || followProxy == null)
            {
                Debug.LogError("[PartyCameraDirectorSwoosh] vcam or followProxy missing.");
                enabled = false;
                return;
            }

            // LookAhead 자동 탐색 (인스펙터에 안 넣어도 동작)
            if (lookAhead == null)
                lookAhead = followProxy.GetComponent<LookAheadFollowProxy>();

            // ✅ VCam은 항상 FollowProxy만 Follow (일관성)
            if (vcam.Follow != followProxy)
                vcam.Follow = followProxy;

            // 시작 위치/타겟 고정
            var pivot = GetPivot(_currentIndex);
            if (pivot != null)
            {
                if (lookAhead != null)
                {
                    lookAhead.SetTarget(pivot, snap: true);
                }
                else
                {
                    followProxy.position = pivot.position;
                }

                // 튐 방지
                vcam.PreviousStateIsValid = false;
            }
        }

        private void LateUpdate()
        {
            if (!keepProxyLockedToPivot) return;
            if (_isTweening) return;

            var pivot = GetPivot(_currentIndex);
            if (pivot == null) return;

            // ✅ LookAhead가 있으면: FollowProxy를 직접 잠그지 말고 Target만 유지 (LookAhead가 FollowProxy를 움직임)
            if (lookAhead != null)
            {
                // Target이 풀렸을 때(= None) 자동 복구
                if (!lookAhead.HasTarget || lookAhead.Target != pivot)
                    lookAhead.SetTarget(pivot, snap: true);

                return;
            }

            // ✅ LookAhead가 없으면: 예전 방식(직접 Pivot에 붙임)
            followProxy.position = pivot.position;
        }

        public void FocusSlot(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, 3);

            var targetPivot = GetPivot(slotIndex);
            if (targetPivot == null) return;

            // 같은 슬롯이라도 Target이 풀렸을 수 있으니 복구는 해준다
            if (slotIndex == _currentIndex && !_isTweening)
            {
                if (lookAhead != null && (!lookAhead.HasTarget || lookAhead.Target != targetPivot))
                    lookAhead.SetTarget(targetPivot, snap: true);
                return;
            }

            _currentIndex = slotIndex;

            // 기존 트윈 종료
            _killForNewFocus = true;
            _moveTween?.Kill();
            _killForNewFocus = false;

            // LookAhead가 FollowProxy를 덮어쓰면 트윈 중 충돌할 수 있으니 트윈 동안은 잠시 끈다
            if (lookAhead != null)
                lookAhead.enabled = false;

            if (!useSwoosh || travelSpeed <= 0.01f)
            {
                // 즉시 전환(연출 없이)
                followProxy.position = targetPivot.position;
                vcam.PreviousStateIsValid = false;

                RestoreLookAhead(targetPivot, snap: true);
                return;
            }

            // 거리 기반 시간 계산 (슉 느낌)
            float dist = Vector3.Distance(followProxy.position, targetPivot.position);
            float dur = Mathf.Clamp(dist / travelSpeed, minDuration, maxDuration);

            _isTweening = true;

            Vector3 startPos = followProxy.position;

            // ✅ 핵심: 목적지를 "고정 좌표"로 잡지 않고, 매 프레임 targetPivot.position을 읽어서 보정
            // -> 이동 중 캐릭터 선택 시 "그 순간 위치로 갔다가 툭 끊김" 현상 제거
            _moveTween = DOVirtual.Float(0f, 1f, dur, t =>
            {
                Vector3 targetPosNow = targetPivot.position;
                followProxy.position = Vector3.LerpUnclamped(startPos, targetPosNow, t);
            })
                .SetEase(ease)
                .OnStart(() =>
                {
                    vcam.PreviousStateIsValid = false;
                })
                .OnKill(() =>
                {
                    _isTweening = false;
                    if (_killForNewFocus) return; // 새 포커스 전환 중 kill이면 복구는 새 트윈이 처리

                    // 외부에서 강제 Kill된 경우만 안전 복구
                    var pivot = GetPivot(_currentIndex);
                    if (pivot != null)
                        RestoreLookAhead(pivot, snap: true);
                })
                .OnComplete(() =>
                {
                    _isTweening = false;

                    // 완료 시 한 번 확정
                    followProxy.position = targetPivot.position;

                    RestoreLookAhead(targetPivot, snap: true);
                });
        }

        private void RestoreLookAhead(Transform pivot, bool snap)
        {
            if (lookAhead == null || pivot == null) return;

            lookAhead.enabled = true;
            lookAhead.SetTarget(pivot, snap);
        }

        private Transform GetPivot(int index)
        {
            if (cameraPivots == null || cameraPivots.Length != 4)
            {
                Debug.LogError("[PartyCameraDirectorSwoosh] cameraPivots must be size 4.");
                return null;
            }

            var p = cameraPivots[index];
            if (p == null)
                Debug.LogWarning($"[PartyCameraDirectorSwoosh] cameraPivots[{index}] is null.");
            return p;
        }
    }
}
