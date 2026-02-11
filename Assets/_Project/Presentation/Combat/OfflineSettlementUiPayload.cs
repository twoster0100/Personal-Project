using System;

namespace MyGame.Presentation.Combat
{
    /// <summary>
    /// 오프라인 정산 결과를 UI 계층으로 전달하기 위한 읽기 전용 payload.
    /// </summary>
    [Serializable]
    public struct OfflineSettlementUiPayload
    {
        public long elapsedSeconds;
        public long cappedSeconds;
        public int powerTier;

        public long gold;
        public long exp;
        public long drop;

        public double dropCarry;

        public bool HasReward => gold > 0 || exp > 0 || drop > 0;
    }
}