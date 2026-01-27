using UnityEngine;
using MyGame.Application.Diagnostics;

// ✅ 네임 충돌 방지
using UApp = UnityEngine.Application;
using UDebug = UnityEngine.Debug;

namespace MyGame.Presentation.Diagnostics
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class ResourceAuditHUD : MonoBehaviour
    {
        [Header("Visibility")]
        [SerializeField] private bool show = true;
        [SerializeField] private bool showOnlyWhenNonZero = true;

        [Header("Auto Dump")]
        [SerializeField] private bool dumpIfLeaksOnDisable = true;
        [SerializeField] private bool dumpIfLeaksOnQuit = true;

        [Header("UI")]
        [SerializeField] private bool showDumpButton = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F8;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesBuilt;

        private bool _enabledInThisBuild;

        private void Awake()
        {
            // ✅ 릴리즈 빌드에서는 자동 비활성화
            _enabledInThisBuild = UDebug.isDebugBuild || UApp.isEditor;
            if (!_enabledInThisBuild)
            {
                enabled = false;
                return;
            }

            // ❌ 여기서 GUI.skin 만지면 예외가 날 수 있음 (OnGUI 밖)
            // BuildStyles();  <-- 삭제
        }

        private void OnEnable()
        {
            if (!_enabledInThisBuild) return;
            UApp.quitting += OnAppQuitting;
        }

        private void OnDisable()
        {
            if (!_enabledInThisBuild) return;

            UApp.quitting -= OnAppQuitting;

            if (dumpIfLeaksOnDisable && HasLeaks())
                Dump("OnDisable");
        }

        private void OnGUI()
        {
            if (!_enabledInThisBuild) return;

            // ✅ GUI 스타일은 OnGUI 안에서만 안전하게 생성
            EnsureStyles();

            // F8 토글 (Update 없이 OnGUI 이벤트로 처리)
            var e = Event.current;
            if (toggleKey != KeyCode.None && e.type == EventType.KeyDown && e.keyCode == toggleKey)
            {
                show = !show;
                e.Use();
            }

            if (!show) return;

            int tweens = ResourceAudit.ActiveTweens;
            int addr = ResourceAudit.ActiveAddressablesHandles;

            // showOnlyWhenNonZero가 켜져 있으면 0일 때 HUD 자체가 안 보임
            if (showOnlyWhenNonZero && tweens == 0 && addr == 0)
                return;

            const float w = 260f;
            float h = showDumpButton ? 115f : 90f;

            GUILayout.BeginArea(new Rect(10, 10, w, h), _boxStyle);
            GUILayout.Label("RESOURCE AUDIT HUD", _labelStyle);
            GUILayout.Label($"ActiveTweens : {tweens}", _labelStyle);
            GUILayout.Label($"ActiveAddr   : {addr}", _labelStyle);

            if (showDumpButton)
            {
                if (GUILayout.Button("Dump Report", _buttonStyle))
                    Dump("Manual(Dump Button)");
            }

            GUILayout.EndArea();
        }

        [ContextMenu("Dump Report Now")]
        private void DumpNow()
        {
            if (!_enabledInThisBuild) return;
            Dump("Manual(Context Menu)");
        }

        private void OnAppQuitting()
        {
            if (dumpIfLeaksOnQuit && HasLeaks())
                Dump("OnQuit");
        }

        private static bool HasLeaks()
            => ResourceAudit.ActiveTweens != 0 || ResourceAudit.ActiveAddressablesHandles != 0;

        private static void Dump(string reason)
        {
            int tweens = ResourceAudit.ActiveTweens;
            int addr = ResourceAudit.ActiveAddressablesHandles;

            UDebug.Log(
                $"[ResourceAuditHUD] {reason}\n" +
                $"ActiveTweens={tweens}\n" +
                $"ActiveAddressablesHandles={addr}\n" +
                $"Frame={Time.frameCount}, Time={Time.time:0.000}"
            );
        }

        private void EnsureStyles()
        {
            if (_stylesBuilt) return;

            // ✅ 여기서만 GUI.skin 접근 (OnGUI 안이라 안전)
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 8, 8)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12
            };

            _stylesBuilt = true;
        }
    }
}
