using MyGame.Application.Save;

namespace MyGame.Presentation.Progress
{
    /// <summary>
    /// ✅ SettingsBinding과 동일한 개념(진행 데이터 버전)
    /// - ApplyFromSave: 로드된 저장값을 런타임(모델/뷰)에 반영
    /// - CaptureToSave: 런타임 현재값을 저장 데이터로 수집
    /// </summary>
    public interface IPlayerProgressBinding
    {
        void ApplyFromSave(PlayerProgressSaveData data);
        void CaptureToSave(PlayerProgressSaveData data);
    }
}
