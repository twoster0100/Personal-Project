using Cinemachine;
using DG.Tweening;
using UnityEngine;
using MyGame.Application.Tick;
using MyGame.Composition;

namespace PartySelection.Camera
{
    public sealed class PartyCameraDirectorSwoosh : MonoBehaviour, ILateFrameTickable
    {
        [Header("Single VCam")]
        [SerializeField] private CinemachineVirtualCamera vcam;

        [Header("Look-ahead Proxy (VCam always follows this)")]
        [SerializeField] private Transform followProxy;

        [Header("Optional: LookAhead controller on FollowProxy")]
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

            if (lookAhead == null)
                lookAhead = followProxy.GetComponent<LookAheadFollowProxy>();

            if (vcam.Follow != followProxy)
                vcam.Follow = followProxy;

            var pivot = GetPivot(_currentIndex);
            if (pivot != null)
            {
                if (lookAhead != null) lookAhead.SetTarget(pivot, snap: true);
                else followProxy.position = pivot.position;

                vcam.PreviousStateIsValid = false;
            }
        }

        private void OnEnable()
        {
            if (global::UnityEngine.Application.isPlaying)
                AppCompositionRoot.RegisterWhenReady(this);
        }

        private void OnDisable()
        {
            AppCompositionRoot.UnregisterTickable(this);
        }

        // ✅ LateUpdate 제거 → LateFrameTick
        public void LateFrameTick(float dt)
        {
            if (!keepProxyLockedToPivot) return;
            if (_isTweening) return;

            var pivot = GetPivot(_currentIndex);
            if (pivot == null) return;

            if (lookAhead != null)
            {
                if (!lookAhead.HasTarget || lookAhead.Target != pivot)
                    lookAhead.SetTarget(pivot, snap: true);
                return;
            }

            followProxy.position = pivot.position;
        }

        public void FocusSlot(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, 3);

            var targetPivot = GetPivot(slotIndex);
            if (targetPivot == null) return;

            if (slotIndex == _currentIndex && !_isTweening)
            {
                if (lookAhead != null && (!lookAhead.HasTarget || lookAhead.Target != targetPivot))
                    lookAhead.SetTarget(targetPivot, snap: true);
                return;
            }

            _currentIndex = slotIndex;

            _killForNewFocus = true;
            _moveTween?.Kill();
            _killForNewFocus = false;

            if (lookAhead != null)
                lookAhead.enabled = false;

            if (!useSwoosh || travelSpeed <= 0.01f)
            {
                followProxy.position = targetPivot.position;
                vcam.PreviousStateIsValid = false;

                RestoreLookAhead(targetPivot, snap: true);
                return;
            }

            float dist = Vector3.Distance(followProxy.position, targetPivot.position);
            float dur = Mathf.Clamp(dist / travelSpeed, minDuration, maxDuration);

            _isTweening = true;

            Vector3 startPos = followProxy.position;

            _moveTween = DOVirtual.Float(0f, 1f, dur, t =>
            {
                Vector3 targetPosNow = targetPivot.position;
                followProxy.position = Vector3.LerpUnclamped(startPos, targetPosNow, t);
            })
                .SetEase(ease)
                .OnStart(() => { vcam.PreviousStateIsValid = false; })
                .OnKill(() =>
                {
                    _isTweening = false;
                    if (_killForNewFocus) return;

                    var pivot = GetPivot(_currentIndex);
                    if (pivot != null)
                        RestoreLookAhead(pivot, snap: true);
                })
                .OnComplete(() =>
                {
                    _isTweening = false;
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
