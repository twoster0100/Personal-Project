using UnityEngine;
using MyGame.Application.Save;

namespace MyGame.Presentation.Progress
{
    /// <summary>
    ///  테스트용 동작/검증 가능한 "임시 런타임 모델"
    /// - 나중에  실제 전투/재화/스테이지 시스템이 생기면
    ///   그 시스템이 IPlayerProgressBinding을 구현하거나,
    ///   이 컴포넌트를 그 시스템에 연결해서 대체하면 됨.
    /// </summary>
    public sealed class PlayerProgressRuntimeBinding : MonoBehaviour, IPlayerProgressBinding
    {
        [Header("Wiring")]
        [SerializeField] private PlayerProgressSavePresenter savePresenter;

        [Header("Runtime Values (임시)")]
        [SerializeField] private int stageIndex = 1;
        [SerializeField] private long gold = 0;

        public int StageIndex => stageIndex;
        public long Gold => gold;

        public void ApplyFromSave(PlayerProgressSaveData data)
        {
            stageIndex = data.stageIndex;
            gold = data.gold;
        }

        public void CaptureToSave(PlayerProgressSaveData data)
        {
            data.stageIndex = stageIndex;
            data.gold = gold;
        }

        // ---------------------------
        // 아래 2개는 버튼/테스트 연결용 (UnityEvent 파라미터 없이 호출 가능)
        // ---------------------------
        public void DebugAddGold100()
        {
            gold += 100;
            savePresenter?.NotifyChangedFromGame("DebugAddGold100");
        }

        public void DebugNextStage()
        {
            stageIndex += 1;
            savePresenter?.NotifyChangedFromGame("DebugNextStage");
        }
    }
}
