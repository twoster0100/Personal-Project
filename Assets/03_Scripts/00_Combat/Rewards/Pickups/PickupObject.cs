using UnityEngine;
using DG.Tweening;

namespace MyGame.Combat
{
    public enum PickupKind { ExpOrb, Item }

    [DisallowMultipleComponent]
    public sealed class PickupObject : MonoBehaviour
    {
        [Header("Payload (readonly at runtime)")]
        [SerializeField] private PickupKind kind;
        [SerializeField] private string itemId;
        [SerializeField] private int amount;

        [Header("DOTween (visual only)")]
        [Tooltip("드랍될 때 흩뿌리며 착지하는 총 시간")]
        [SerializeField] private float dropDuration = 0.25f;

        [Tooltip("드랍 시 점프 높이")]
        [SerializeField] private float dropJumpPower = 0.6f;

        [Tooltip("드랍 시 회전(도). 360 이상이면 회전이 더 크게 보임")]
        [SerializeField] private float dropSpinDegrees = 720f;

        [Tooltip("자석 이동 기본 시간")]
        [SerializeField] private float magnetDuration = 0.18f;

        [Tooltip("거리 기반 자석 시간 가중치")]
        [SerializeField] private float magnetDurationPerMeter = 0.03f;

        [Tooltip("자석 이동 최소/최대")]
        [SerializeField] private Vector2 magnetDurationClamp = new(0.10f, 0.35f);

        [Tooltip("자석 이동 Ease")]
        [SerializeField] private Ease magnetEase = Ease.InQuad;

        [Tooltip("드랍 Ease")]
        [SerializeField] private Ease dropEase = Ease.OutQuad;

        private PickupSpawner _owner;
        private bool _collected;

        private bool _magneting;
        private PickupCollector _magnetCollector;
        private Transform _magnetTarget;

        private Tween _moveTween;
        private Tween _spinTween;
        private Collider _collider;

        public PickupKind Kind => kind;
        public string ItemId => itemId;
        public int Amount => amount;

        private void Awake()
        {
            _collider = GetComponentInChildren<Collider>();
        }

        /// <summary>
        /// 풀에서 꺼내져 스폰될 때 호출.
        /// origin -> targetPos 로 "드랍" 연출을 먼저 실행한다.
        /// </summary>
        internal void SpawnedBy(PickupSpawner owner, PickupKind k, int amt, string id, Vector3 origin, Vector3 targetPos)
        {
            _owner = owner;
            kind = k;
            amount = Mathf.Max(0, amt);
            itemId = id ?? string.Empty;

            _collected = false;
            _magneting = false;
            _magnetCollector = null;
            _magnetTarget = null;

            KillTweens();

            if (_collider != null) _collider.enabled = true;

            // 드랍 시작 위치를 origin으로 강제
            transform.position = origin;
            PlayDropTween(targetPos);
        }

        /// <summary>
        /// 플레이어 근처에 들어오면 호출. (Trigger 의존 ↓)
        /// 도착 시 collector.TryCollect(this) 호출로 수집 확정.
        /// </summary>
        public bool TryBeginMagnet(PickupCollector collector, Transform magnetTarget)
        {
            if (_collected) return false;
            if (_magneting) return false;
            if (collector == null) return false;
            if (magnetTarget == null) return false;

            _magneting = true;
            _magnetCollector = collector;
            _magnetTarget = magnetTarget;

            // 드랍 연출 중이더라도, 자석이 걸리면 즉시 자석 연출로 전환
            KillTweens();

            // 이동 중 충돌/트리거로 중복 수집되는 것 방지
            if (_collider != null) _collider.enabled = false;

            Vector3 start = transform.position;
            float dist = Vector3.Distance(start, magnetTarget.position);
            float dur = magnetDuration + dist * magnetDurationPerMeter;
            dur = Mathf.Clamp(dur, magnetDurationClamp.x, magnetDurationClamp.y);

            // "움직이는 타겟(플레이어)"을 따라가기 위해
            // endValue를 매 프레임 다시 읽어 Lerp 종점으로 사용한다.
            float t = 0f;
            _moveTween = DOTween.To(
                    () => t,
                    x =>
                    {
                        t = x;
                        if (_magnetTarget == null) return;
                        transform.position = Vector3.LerpUnclamped(start, _magnetTarget.position, t);
                    },
                    1f,
                    dur)
                .SetEase(magnetEase);

            // 자석 중에도 살짝 회전하면 "빨려들어옴" 느낌이 좋아짐
            _spinTween = transform
                .DORotate(new Vector3(0f, dropSpinDegrees, 0f), dur, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear);

            _moveTween.OnComplete(() => CollectByMagnet());
            return true;
        }

        private void CollectByMagnet()
        {
            if (_collected) return;
            if (_magnetCollector == null)
            {
                // 타겟/콜렉터가 사라졌으면 다시 트리거 수집 가능하도록 복구
                _magneting = false;
                if (_collider != null) _collider.enabled = true;
                return;
            }

            Collect(_magnetCollector);
        }

        private void PlayDropTween(Vector3 targetPos)
        {
            float dur = Mathf.Max(0.01f, dropDuration);

            // DOJump: 포물선 느낌의 간단 드랍
            _moveTween = transform
                .DOJump(targetPos, dropJumpPower, 1, dur)
                .SetEase(dropEase);

            // 드랍하면서 회전
            _spinTween = transform
                .DORotate(new Vector3(0f, dropSpinDegrees, 0f), dur, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear);
        }

        private void KillTweens()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _spinTween?.Kill();
            _spinTween = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;
            if (_magneting) return; // 자석 이동 중엔 Trigger 의존을 줄이기 위해 무시

            var collector = other.GetComponentInParent<PickupCollector>();
            if (collector == null) return;

            Collect(collector);
        }

        private void Collect(PickupCollector collector)
        {
            if (_collected) return;
            if (collector == null) return;

            if (collector.TryCollect(this))
            {
                _collected = true;
                KillTweens();

                if (_owner != null) _owner.Release(this);
                else gameObject.SetActive(false);
            }
            else
            {
                // 수집 실패(예: 조건)면 다시 트리거 수집 가능하도록 복구
                _magneting = false;
                _magnetCollector = null;
                _magnetTarget = null;
                if (_collider != null) _collider.enabled = true;
            }
        }

        private void OnDisable()
        {
            KillTweens();
            _collected = false;
            _magneting = false;
            _magnetCollector = null;
            _magnetTarget = null;
            if (_collider != null) _collider.enabled = true;
        }
    }
}
