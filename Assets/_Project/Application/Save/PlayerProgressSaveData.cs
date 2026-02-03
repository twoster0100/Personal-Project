using System;

namespace MyGame.Application.Save
{
    /// <summary>
    /// "유저 계정에 귀속되는" 진행 데이터
    /// - settings_0(기기 공통 설정)과 분리해서 덮어쓰기 사고를 방지
    /// - 슬롯 ID를 userId에 의해 결정하도록 설계
    ///
    ///  v1 JSON과의 호환:
    /// - 기존 저장 파일에 gem 필드가 없어도, 로드시 기본값(0)으로 들어온다.
    /// </summary>
    [Serializable]
    public sealed class PlayerProgressSaveData
    {
        public const string TypeId = "player_progress_v1";

        public int stageIndex = 1;
        public long gold = 0;

        // ✅ 추가: Gem(Gam)
        public long gem = 0;
    }
}
