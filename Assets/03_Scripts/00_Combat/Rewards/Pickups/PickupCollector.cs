using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// 플레이어가 픽업 오브젝트와 Trigger 충돌했을 때 보상을 실제로 반영하는 수집기.
    /// 현재는 PlayerRewardRuntime(임시)로 전달해서 런타임 검증을 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PickupCollector : MonoBehaviour
    {
        [Header("Receiver")]
        [SerializeField] private PlayerRewardRuntime rewardRuntime;

        private void Reset()
        {
            if (rewardRuntime == null) rewardRuntime = GetComponent<PlayerRewardRuntime>();
        }

        public bool TryCollect(PickupObject pickup)
        {
            if (pickup == null) return false;

            if (rewardRuntime == null)
            {
                Debug.LogWarning("[PickupCollector] No PlayerRewardRuntime on collector.");
                return false;
            }

            switch (pickup.Kind)
            {
                case PickupKind.ExpOrb:
                    rewardRuntime.AddExp(pickup.Amount);
                    return true;

                case PickupKind.Item:
                    rewardRuntime.AddItem(pickup.ItemId, pickup.Amount);
                    return true;

                default:
                    return false;
            }
        }
    }
}
