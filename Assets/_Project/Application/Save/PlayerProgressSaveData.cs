using System;

namespace MyGame.Application.Save
{
    /// <summary>
    ///  "유저 계정에 귀속되는" 진행 데이터 예시
    /// - settings_0(기기 공통 설정)과 분리해서 덮어쓰기 사고를 방지
    /// - 슬롯 ID를 userId에 의해 결정하도록 설계
    /// </summary>
    [Serializable]
    public sealed class PlayerProgressSaveData
    {
        public const string TypeId = "player_progress_v1";

        public int stageIndex = 1;
        public long gold = 0;

        // (확장 예시)
        // public int power;
        // public string[] equippedItemIds;
        // public long lastLoginUtcTicks;
    }
}
