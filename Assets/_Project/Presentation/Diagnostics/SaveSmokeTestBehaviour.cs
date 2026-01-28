using UnityEngine;
using MyGame.Application;
using MyGame.Application.Save;

namespace MyGame.Presentation.Diagnostics
{
    public sealed class SaveSmokeTestBehaviour : MonoBehaviour
    {
        private const string Slot = "0";

        private async void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                var data = new PrototypeSaveData
                {
                    autoMode = true,
                    targetFpsMode = 0,
                    stageIndex = 1,
                    gold = 123
                };

                var r = await App.Save.SaveAsync(Slot, data, PrototypeSaveData.TypeId);
                Debug.Log($"[SAVE] success={r.Success}, status={r.Status}, msg={r.Message}");
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                var r = await App.Save.LoadAsync<PrototypeSaveData>(Slot, PrototypeSaveData.TypeId);
                Debug.Log($"[LOAD] success={r.Success}, status={r.Status}, msg={r.Message}");

                if (r.Success && r.Data != null)
                    Debug.Log($"[LOAD DATA] auto={r.Data.autoMode}, fps={r.Data.targetFpsMode}, stage={r.Data.stageIndex}, gold={r.Data.gold}");
            }
        }
    }
}
