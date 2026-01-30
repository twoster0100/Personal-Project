// MyGame.Presentation/Title/TitleStartPresenter.cs
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyGame.Application;      // ✅ App.Auth
using MyGame.Application.Auth; // IAuthService

namespace MyGame.Presentation.Title
{
    public sealed class TitleStartPresenter : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private string combatSceneName = "Combat_Test";

        [Header("Debug")]
        [SerializeField] private bool log = true;

        private CancellationTokenSource _cts;

        // (선택) 외부에서 주입해도 되고, 없으면 App.Auth에서 자동 바인딩된다.
        public IAuthService Auth { private get; set; }

        private void Awake()
        {
            BindAuthIfNeeded();
        }

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();
            BindAuthIfNeeded();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void BindAuthIfNeeded()
        {
            if (Auth != null) return;

            Auth = App.Auth;

            if (Auth == null)
                Debug.LogError("[Title] Auth is null. App is not initialized. Check AppAutoBootstrap/AppCompositionRoot.");
        }

        public async void OnClickStart()
        {
            if (Auth == null)
            {
                Debug.LogError("[Title] Auth is null. Check CompositionRoot wiring.");
                return;
            }

            try
            {
                var session = await Auth.SignInAsync(_cts.Token);
                if (log) Debug.Log($"[Title] SignedIn userId={session.UserId}");

                SceneManager.LoadScene(combatSceneName);
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
