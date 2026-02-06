using UnityEngine;
using MyGame.Application.Diagnostics;

// 네임 충돌 방지 (UnityEngine.Application / Debug를 별칭으로 사용)
using UApp = UnityEngine.Application;
using UDebug = UnityEngine.Debug;

namespace MyGame.Presentation.Diagnostics
{
    /// <summary>
    /// 디버그 빌드(또는 에디터)에서만 동작하는 "리소스 누수 감시 HUD".
    ///
    /// 화면 좌측 상단에 ResourceAudit에서 집계하는 카운터를 표시한다:
    /// - ActiveTweens : 현재 살아있는 Tween 개수 (예: DOTween)
    /// - ActiveAddressablesHandles : 현재 살아있는 Addressables 핸들 개수
    ///
    /// 목적:
    /// - 씬 전환/종료 시 Kill/Release 누락(누수) 여부를 즉시 눈으로 확인
    /// - 누수가 남아있으면 자동으로 로그 덤프(옵션)
    ///
    /// 설계 포인트:
    /// - Update를 쓰지 않는다. (OnGUI 이벤트에서 키 입력 처리)
    /// - GUIStyle은 OnGUI 안에서만 생성한다. (OnGUI 밖에서 GUI.skin 접근 시 예외 가능)
    /// - 릴리즈 빌드에서는 자동으로 완전 비활성화한다.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)] // 매우 이른 시점에 실행(진단 HUD는 항상 먼저)
    public sealed class ResourceAuditHUD : MonoBehaviour
    {
        [Header("Visibility")]
        [SerializeField] private bool show = true;                 // HUD 표시 여부
        [SerializeField] private bool showOnlyWhenNonZero = true;  // 카운터가 0이면 HUD를 숨길지 여부

        [Header("Auto Dump")]
        [SerializeField] private bool dumpIfLeaksOnDisable = true; // Disable될 때 누수가 있으면 로그 덤프
        [SerializeField] private bool dumpIfLeaksOnQuit = true;    // 앱 종료 시 누수가 있으면 로그 덤프

        [Header("UI")]
        [SerializeField] private bool showDumpButton = true;       // HUD에 수동 덤프 버튼 표시
        [SerializeField] private KeyCode toggleKey = KeyCode.F8;   // HUD 토글 키

        // OnGUI에서 사용할 스타일들
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesBuilt;

        // 릴리즈 빌드에서는 false가 되어 완전히 꺼짐
        private bool _enabledInThisBuild;

        private void Awake()
        {
            // 디버그 빌드 또는 에디터에서만 HUD 활성
            _enabledInThisBuild = UDebug.isDebugBuild || UApp.isEditor;
            if (!_enabledInThisBuild)
            {
                enabled = false; // MonoBehaviour 자체 비활성화
                return;
            }

            // ⚠️ 주의:
            // GUI.skin은 OnGUI 호출 시점에 안정적이다.
            // Awake/Start에서 GUI.skin을 만지면 환경에 따라 예외가 날 수 있어
            // 스타일 생성은 OnGUI 안에서 수행한다.
        }

        private void OnEnable()
        {
            if (!_enabledInThisBuild) return;

            // 앱 종료 이벤트 구독 (OnQuit 시 누수 덤프에 사용)
            UApp.quitting += OnAppQuitting;
        }

        private void OnDisable()
        {
            if (!_enabledInThisBuild) return;

            // 구독 해제
            UApp.quitting -= OnAppQuitting;

            // 오브젝트가 Disable되는 시점에 누수가 남아있으면 덤프
            // (씬 전환/파괴 시 누수 검출에 특히 유용)
            if (dumpIfLeaksOnDisable && HasLeaks())
                Dump("OnDisable");
        }

        private void OnGUI()
        {
            if (!_enabledInThisBuild) return;

            // GUIStyle은 OnGUI에서만 안전하게 생성
            EnsureStyles();

            // Update 없이 OnGUI 이벤트에서 키 입력 처리
            var e = Event.current;
            if (toggleKey != KeyCode.None && e.type == EventType.KeyDown && e.keyCode == toggleKey)
            {
                show = !show;
                e.Use(); // 이벤트 소비(다른 GUI에게 전달 방지)
            }

            if (!show) return;

            // 리소스 카운트 취득 (ResourceAudit는 별도 진단 집계 시스템)
            int tweens = ResourceAudit.ActiveTweens;
            int addr = ResourceAudit.ActiveAddressablesHandles;

            // 옵션: 둘 다 0이면 HUD 자체를 표시하지 않음
            if (showOnlyWhenNonZero && tweens == 0 && addr == 0)
                return;

            // HUD 레이아웃
            const float w = 260f;
            float h = showDumpButton ? 115f : 90f;

            GUILayout.BeginArea(new Rect(10, 10, w, h), _boxStyle);
            GUILayout.Label("RESOURCE AUDIT HUD", _labelStyle);
            GUILayout.Label($"ActiveTweens : {tweens}", _labelStyle);
            GUILayout.Label($"ActiveAddr   : {addr}", _labelStyle);

            // 수동 덤프 버튼
            if (showDumpButton)
            {
                if (GUILayout.Button("Dump Report", _buttonStyle))
                    Dump("Manual(Dump Button)");
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// 인스펙터 우클릭 메뉴로 즉시 덤프 가능
        /// </summary>
        [ContextMenu("Dump Report Now")]
        private void DumpNow()
        {
            if (!_enabledInThisBuild) return;
            Dump("Manual(Context Menu)");
        }

        /// <summary>
        /// Application.quitting 이벤트 핸들러
        /// </summary>
        private void OnAppQuitting()
        {
            if (dumpIfLeaksOnQuit && HasLeaks())
                Dump("OnQuit");
        }

        /// <summary>
        /// 누수 판단 기준: 트윈 또는 어드레서블 핸들이 0이 아니면 누수로 간주
        /// </summary>
        private static bool HasLeaks()
            => ResourceAudit.ActiveTweens != 0 || ResourceAudit.ActiveAddressablesHandles != 0;

        /// <summary>
        /// 현재 카운터 값을 로그로 출력
        /// - 원인을 reason으로 남겨, 어떤 타이밍에 누수가 확인됐는지 추적 가능
        /// </summary>
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

        /// <summary>
        /// GUIStyle 지연 초기화:
        /// - OnGUI에서만 GUI.skin 접근이 안전하므로 여기서 생성한다.
        /// - 한 번만 생성하고 이후 재사용.
        /// </summary>
        private void EnsureStyles()
        {
            if (_stylesBuilt) return;

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
