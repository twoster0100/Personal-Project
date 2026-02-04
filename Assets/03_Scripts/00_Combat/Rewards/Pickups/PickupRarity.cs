using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// 픽업(아이템/재화) 연출 파라미터를 차등 주기 위한 MVP 등급.
    /// (실제 아이템 등급/희귀도 시스템과는 별개로, 현재는 연출 용도)
    /// </summary>
    public enum PickupRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }
}
