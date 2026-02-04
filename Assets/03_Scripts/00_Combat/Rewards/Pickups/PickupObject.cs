using System;
using DG.Tweening;
using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// 픽업 오브젝트(ExpOrb / Item 등).
    /// - 드랍: Scatter + 포물선 점프 + 회전
    /// - 자석: 플레이어 MagnetTarget으로 가속 이동(+옵션 오버슈트) 후 TryCollect로 확정
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PickupObject : MonoBehaviour
    {
        // ----------------------------
        // Payload
        // ----------------------------
        [Header("Payload")]
        [SerializeField] private PickupKind kind = PickupKind.ExpOrb;
        [SerializeField] private int amount = 1;

        [Tooltip("Kind=Item일 때만 사용")]
        [SerializeField] private string itemId;

        [Tooltip("연출 차등용(아이템/재화 희귀도). 실제 아이템 시스템과 연결되기 전까지는 MVP 값.")]
        [SerializeField] private PickupRarity rarity = PickupRarity.Common;

        // ----------------------------
        // Drop FX
        // ----------------------------
        [Header("Drop FX (Scatter + Jump + Spin)")]
        [SerializeField] private float dropDuration = 0.28f;
        [SerializeField] private float dropJumpPower = 0.55f;

        [Tooltip("드랍 중 회전량(도). 720~1440 추천")]
        [SerializeField] private float dropSpinDegrees = 1080f;

        [Tooltip("드랍 시 회전축 랜덤 흔들림(도). 0이면 Y축 스핀만")]
        [SerializeField] private float dropRandomTiltDegrees = 15f;

        // ----------------------------
        // Magnet FX
        // ----------------------------
        [Header("Magnet FX (Accelerate + Overshoot + Trail)")]
        [Tooltip("거리 기반 이동시간 계산용 속도(유닛/초). 값이 클수록 빨리 빨려옴")]
        [SerializeField] private float magnetSpeed = 14f;

        [Tooltip("자석 이동 최소 시간")]
        [SerializeField] private float magnetMinDuration = 0.12f;

        [Tooltip("자석 이동 최대 시간")]
        [SerializeField] private float magnetMaxDuration = 0.30f;

        [Tooltip("가속 느낌(빨려들어오는 느낌)을 만드는 Ease")]
        [SerializeField] private Ease magnetEase = Ease.InCubic;

        [Tooltip("타겟을 살짝 지나쳤다가(오버슈트) 다시 들어오는 연출")]
        [SerializeField] private bool useOvershoot = true;

        [Tooltip("오버슈트 거리(유닛). 너무 크면 튄다. 0.15~0.45 추천")]
        [SerializeField] private float overshootDistance = 0.28f;

        [Tooltip("오버슈트 구간 비율(0~0.5). 0.12~0.25 추천")]
        [Range(0f, 0.5f)]
        [SerializeField] private float overshootRatio = 0.18f;

        [Tooltip("자석 중 TrailRenderer를 켜서 꼬리(트레일) 연출")]
        [SerializeField] private bool enableTrailDuringMagnet = true;

        [SerializeField] private TrailRenderer trail;

        // ----------------------------
        // Runtime
        // ----------------------------
        private PickupCollector _collector;
        private bool _collected;
        private bool _magneting;

        private Tween _dropTween;
        private Tween _spinTween;
        private Sequence _magnetSeq;

        public PickupKind Kind => kind;
        public int Amount => amount;
        public string ItemId => itemId;
        public PickupRarity Rarity => rarity;

        private void Reset()
        {
            if (trail == null) trail = GetComponentInChildren<TrailRenderer>(includeInactive: true);
        }

        private void OnDisable()
        {
            KillTweens();

            // 풀링 재사용 시 트레일 잔상 방지
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }

            _collector = null;
            _collected = false;
            _magneting = false;
        }

        // ----------------------------
        // Setup (Spawner가 호출)
        // ----------------------------
        public void SetupExp(int expAmount)
        {
            kind = PickupKind.ExpOrb;
            amount = Mathf.Max(1, expAmount);
            itemId = null;
            rarity = PickupRarity.Common;
        }

        public void SetupItem(string id, int count, PickupRarity r)
        {
            kind = PickupKind.Item;
            itemId = id;
            amount = Mathf.Max(1, count);
            rarity = r;
        }

        // ----------------------------
        // Drop
        // ----------------------------
        public void BeginDrop(Vector3 originWorld, Vector3 landingWorld)
        {
            transform.position = originWorld;

            // 드랍 시작 시 트레일은 꺼둔다(자석에서만 켬)
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }

            KillDropTweens();

            // 희귀도에 따라 살짝 더 화려하게(점프/스핀)
            var (dur, jump, spin, tilt) = GetDropFxByRarity();

            _dropTween = transform
                .DOJump(landingWorld, jump, 1, dur)
                .SetEase(Ease.OutQuad);

            // 회전(포물선 동안 회전하면서 떨어지는 느낌)
            Vector3 axisTilt = Vector3.zero;
            if (tilt > 0f)
            {
                axisTilt = new Vector3(
                    UnityEngine.Random.Range(-tilt, tilt),
                    0f,
                    UnityEngine.Random.Range(-tilt, tilt)
                );
            }

            _spinTween = transform
                .DORotate(axisTilt + new Vector3(0f, spin, 0f), dur, RotateMode.FastBeyond360)
                .SetRelative(true)
                .SetEase(Ease.OutQuad);
        }

        private (float dur, float jump, float spin, float tilt) GetDropFxByRarity()
        {
            // Exp는 항상 동일(가벼운 느낌)
            if (kind == PickupKind.ExpOrb)
                return (dropDuration, dropJumpPower, dropSpinDegrees, dropRandomTiltDegrees);

            // Item 희귀도별 가중(원하면 여기 숫자만 바꾸면 됨)
            float mul = rarity switch
            {
                PickupRarity.Common => 1.00f,
                PickupRarity.Rare => 1.05f,
                PickupRarity.Epic => 1.10f,
                PickupRarity.Legendary => 1.15f,
                _ => 1.0f
            };

            float spinMul = rarity switch
            {
                PickupRarity.Common => 1.0f,
                PickupRarity.Rare => 1.2f,
                PickupRarity.Epic => 1.4f,
                PickupRarity.Legendary => 1.7f,
                _ => 1.0f
            };

            return (dropDuration * mul, dropJumpPower * mul, dropSpinDegrees * spinMul, dropRandomTiltDegrees);
        }

        // ----------------------------
        // Magnet
        // ----------------------------
        public void TryStartMagnet(Transform magnetTarget, PickupCollector collector)
        {
            if (_collected) return;
            if (_magneting) return;
            if (magnetTarget == null) return;
            if (collector == null) return;

            _collector = collector;

            // 드랍 중이라도 자석이 시작되면 드랍 트윈을 끊고 자석으로 전환
            KillDropTweens();

            BeginMagnet(magnetTarget);
        }

        private void BeginMagnet(Transform magnetTarget)
        {
            _magneting = true;

            if (trail != null && enableTrailDuringMagnet)
            {
                trail.Clear();
                trail.emitting = true;
            }

            // 회전은 자석 중에도 약간 유지(원하면 0으로)
            float spinPerSec = 720f;
            _spinTween = transform
                .DORotate(new Vector3(0f, spinPerSec, 0f), 1f, RotateMode.FastBeyond360)
                .SetRelative(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1);

            Vector3 start = transform.position;
            Vector3 end = magnetTarget.position;

            float dist = Vector3.Distance(start, end);
            float dur = (magnetSpeed <= 0.01f) ? magnetMinDuration : (dist / magnetSpeed);
            dur = Mathf.Clamp(dur, magnetMinDuration, magnetMaxDuration);

            // 너무 가까우면 오버슈트 금지
            float maxOver = Mathf.Min(overshootDistance, dist * 0.35f);
            bool doOver = useOvershoot && maxOver > 0.05f && overshootRatio > 0.01f;

            _magnetSeq?.Kill(complete: false);
            _magnetSeq = DOTween.Sequence();

            if (!doOver)
            {
                _magnetSeq.Append(transform.DOMove(end, dur).SetEase(magnetEase));
            }
            else
            {
                Vector3 dir = (end - start);
                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
                Vector3 over = end + dir.normalized * maxOver;

                float t2 = dur * Mathf.Clamp01(overshootRatio);
                float t1 = Mathf.Max(0.01f, dur - t2);

                _magnetSeq.Append(transform.DOMove(over, t1).SetEase(magnetEase));
                _magnetSeq.Append(transform.DOMove(end, t2).SetEase(Ease.OutQuad));
            }

            _magnetSeq.OnComplete(CollectNow);
        }

        private void CollectNow()
        {
            if (_collected) return;
            _collected = true;
            _magneting = false;

            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }

            KillTweens();

            // Trigger 의존 줄이고, "도착 = 수집"으로 확정
            if (_collector != null)
                _collector.TryCollect(this);
        }

        // ----------------------------
        // Utils
        // ----------------------------
        private void KillDropTweens()
        {
            _dropTween?.Kill(complete: false);
            _dropTween = null;

            // 드랍 회전도 끊는다(자석에서 다시 돌릴 수 있음)
            _spinTween?.Kill(complete: false);
            _spinTween = null;
        }

        private void KillTweens()
        {
            KillDropTweens();

            _magnetSeq?.Kill(complete: false);
            _magnetSeq = null;
        }
    }
}
