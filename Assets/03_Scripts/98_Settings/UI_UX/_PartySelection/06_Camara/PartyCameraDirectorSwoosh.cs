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
    /// </summary>
    public sealed class PartyCameraDirectorSwoosh : MonoBehaviour
    {
        [Header("Single VCam")]
        [SerializeField] private CinemachineVirtualCamera vcam;

        [Header("Look-ahead Proxy (VCam always follows this)")]
        [SerializeField] private Transform followProxy;

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
        [Tooltip("슉 이동이 끝난 후에는 FollowProxy를 Pivot에 계속 붙여서 시점이 유지되게 함")]
        [SerializeField] private bool keepProxyLockedToPivot = true;

        private int _currentIndex;
        private Tween _moveTween;
        private bool _isTweening;

        private void Awake()
        {
            _currentIndex = Mathf.Clamp(startIndex, 0, 3);

            if (vcam == null || followProxy == null)
            {
                Debug.LogError("[PartyCameraDirectorSwoosh] vcam or followProxy missing.");
                enabled = false;
                return;
            }

            // ✅ VCam은 항상 FollowProxy만 Follow (일관성)
            if (vcam.Follow != followProxy)
                vcam.Follow = followProxy;

            // 시작 위치 고정
            var pivot = GetPivot(_currentIndex);
            if (pivot != null)
                followProxy.position = pivot.position;
        }

        private void LateUpdate()
        {
            if (!keepProxyLockedToPivot) return;
            if (_isTweening) return;

            var pivot = GetPivot(_currentIndex);
            if (pivot == null) return;

            // ✅ 평소에는 선택된 Pivot에 계속 붙어있게(캐릭터가 움직여도 추적)
            followProxy.position = pivot.position;
        }

        public void FocusSlot(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, 3);
            if (slotIndex == _currentIndex && !_isTweening)
                return;

            var targetPivot = GetPivot(slotIndex);
            if (targetPivot == null) return;

            _currentIndex = slotIndex;

            _moveTween?.Kill();

            if (!useSwoosh || travelSpeed <= 0.01f)
            {
                // 즉시 전환(연출 없이)
                followProxy.position = targetPivot.position;
                vcam.PreviousStateIsValid = false;
                return;
            }

            // 거리 기반 시간 계산 (슉 느낌)
            float dist = Vector3.Distance(followProxy.position, targetPivot.position);
            float dur = Mathf.Clamp(dist / travelSpeed, minDuration, maxDuration);

            _isTweening = true;

            _moveTween = followProxy
                .DOMove(targetPivot.position, dur)
                .SetEase(ease)
                .OnStart(() =>
                {
                    // 튐 방지 (하지만 매 프레임 lock 같은 건 안 함)
                    vcam.PreviousStateIsValid = false;
                })
                .OnComplete(() =>
                {
                    _isTweening = false;
                    // 완료 시 한 번 확정
                    followProxy.position = targetPivot.position;
                });
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
