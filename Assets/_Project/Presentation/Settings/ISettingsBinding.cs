using MyGame.Application.Save;

namespace MyGame.Presentation.Settings
{
    /// <summary>
    /// SettingsSavePresenter가 "현재 모델 값 ↔ 저장 데이터"를 연결하기 위한 어댑터 인터페이스.
    /// - ApplyFromSave : 로드된 값을 런타임 모델에 반영(저장 트리거 금지)
    /// - CaptureToSave : 런타임 모델의 현재 값을 저장 데이터에 기록
    /// </summary>
    public interface ISettingsBinding
    {
        void ApplyFromSave(SettingsSaveData data);
        void CaptureToSave(SettingsSaveData data);
    }
}
