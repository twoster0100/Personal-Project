using System.Threading;
using UnityEngine;
using MyGame.Application;       // App.Save
using MyGame.Application.Save;  // SettingsSaveData

namespace MyGame.Presentation.Settings
{
    public sealed class SettingsBootPresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private SettingsSavePresenter savePresenter;

        [Header("Debug")]
        [SerializeField] private bool log = false;

        private async void Start()
        {
            if (savePresenter == null)
            {
                Debug.LogError("[SettingsBoot] SettingsSavePresenter is not assigned.");
                return;
            }

            // 부팅 중 저장 방지
            savePresenter.Disarm();
            savePresenter.RebuildBindingsCache();

            if (App.Save == null)
            {
                Debug.LogWarning("[SettingsBoot] App.Save is null. Boot skipped.");

                // 저장 시스템이 없어도, 런타임 기본값을 캐시에 담아두면 이후 UI 저장 흐름이 일관됨
                var defaults = new SettingsSaveData();
                ApplyDefaultsFromRuntime(defaults);

                savePresenter.InitializeCache(defaults);
                savePresenter.Arm();
                return;
            }

            // ✅ 중요: 네 프로젝트 LoadAsync는 3번째 인수가 bool이다.
            // 4번째 인수로 CancellationToken을 받는 오버로드가 있다면 아래가 정답.
            var result = await App.Save.LoadAsync<SettingsSaveData>(
                savePresenter.SlotId,
                SettingsSaveData.TypeId,
                false,                 // <- 3번째 bool
                CancellationToken.None // <- 4번째 CancellationToken
            );

            SettingsSaveData data;

            // HasData 유무 상관없이 Data null 체크로 안정화
            bool hasData = result.Success && result.Data != null;

            if (hasData)
            {
                data = result.Data;
                ApplyToRuntime(data);
            }
            else
            {
                // 저장이 없으면 현재 씬/모델의 기본값을 캡처해서 캐시로 사용
                data = new SettingsSaveData();
                ApplyDefaultsFromRuntime(data);
            }

            savePresenter.InitializeCache(data);
            savePresenter.Arm();

            if (log)
                Debug.Log($"[SettingsBoot] LOAD success={result.Success} status={result.Status}");
        }

        private void ApplyToRuntime(SettingsSaveData data)
        {
            var monos = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < monos.Length; i++)
            {
                if (monos[i] is ISettingsBinding b)
                    b.ApplyFromSave(data);
            }
        }

        private void ApplyDefaultsFromRuntime(SettingsSaveData data)
        {
            var monos = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < monos.Length; i++)
            {
                if (monos[i] is ISettingsBinding b)
                    b.CaptureToSave(data);
            }
        }
    }
}
