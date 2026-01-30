using System;
using System.Threading;
using UnityEngine;
using MyGame.Application;        // App.Save 가 여기 있다고 가정
using MyGame.Application.Save;   // PrototypeSaveData

namespace MyGame.Presentation.Settings
{
    /// <summary>
    /// 부팅 파이프라인:
    /// 1) 저장 프레젠터 잠금(부팅 중 저장 금지)
    /// 2) 저장에서 값 로드
    /// 3) 모델(AutoModeController)에 적용 -> UI는 이벤트로 자동 반영
    /// 4) 저장 프레젠터 잠금 해제(이제부터 변경 시 저장)
    /// </summary>
    public sealed class AutoModeBootPresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private AutoModeController autoMode;
        [SerializeField] private AutoModeSavePresenter savePresenter;

        [Header("Save Slot")]
        [SerializeField] private string slotId = "0";

        [Header("Debug")]
        [SerializeField] private bool log = false;

        private async void Start()
        {
            if (autoMode == null)
            {
                Debug.LogError("[SettingsBoot] AutoModeController is not assigned.");
                return;
            }

            // 1) 부팅 중 저장 금지
            if (savePresenter != null)
                savePresenter.Disarm();

            // 2) 기본값(씬에 세팅된 값)을 우선 사용
            bool finalValue = autoMode.IsAuto;

            // 3) 로드
            if (App.Save != null)
            {
                try
                {
                    // SaveService.cs 시그니처:
                    // LoadAsync<T>(slotId, expectedPayloadTypeId=null, autoResaveAfterMigration=true, ct=default)
                    var result = await App.Save.LoadAsync<PrototypeSaveData>(
                        slotId,
                        PrototypeSaveData.TypeId,
                        autoResaveAfterMigration: false,
                        ct: CancellationToken.None);

                    if (result.Success && result.Data != null)
                        finalValue = result.Data.autoMode;

                    if (log)
                        Debug.Log($"[SettingsBoot] LOAD success={result.Success} status={result.Status} hasData={(result.Data != null)} autoMode={finalValue} slot={slotId}");
                }
                catch (OperationCanceledException)
                {
                    // 부팅 중 취소는 정상 케이스로 취급
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else
            {
                if (log) Debug.LogWarning("[SettingsBoot] App.Save is null. Using scene default.");
            }

            // 4) 모델 적용 -> UI는 onAutoChanged 구독으로 반영
            autoMode.SetAuto(finalValue);

            // 5) 이제부터 저장 허용
            if (savePresenter != null)
                savePresenter.Arm();
        }
    }
}
