using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using MyGame.Combat;
using MyGame.Party;

/// <summary>
/// 조이스틱을 움직이면 자동(오토모드)가 비활성화되고,
/// 아무 조작없이 resumeDelay가 지나면 자동(오토모드)가 활성화 되게 하는 컴포넌트.
///
/// ✅ 파티 요구사항 대응
/// - "현재 컨트롤 중인(카메라가 잡은) 캐릭터"만 AutoMode OFF/ON 및 BlockAutoCombatFor의 영향을 받는다.
/// - 파티 컨트롤이 없는 씬에서는 기존처럼 playerCombat 참조를 사용한다.
/// </summary>
public class JoystickAutoBreak : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Refs")]
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private AutoModeController autoMode;

    [Tooltip("현재 컨트롤 중인 파티 멤버를 알기 위한 라우터(권장).")]
    [SerializeField] private PartyControlRouter partyControl;

    [Tooltip("파티 라우터를 쓰지 않는 씬(단일 플레이어)에서만 사용.")]
    [SerializeField] private CombatController playerCombat;

    [Header("Tuning")]
    [SerializeField] private float idleThreshold = 0.01f; // 조이스틱 드리프트 잡는 용도
    [SerializeField] private float resumeDelay = 6f; // 수동입력 이후 자동으로 전환되기까지의 시간

    private CancellationTokenSource _cts;

    public void OnPointerDown(PointerEventData eventData) => ManualInput();
    public void OnDrag(PointerEventData eventData) => ManualInput();
    public void OnPointerUp(PointerEventData eventData) => ManualInput();

    private CombatController ResolveControlledCombat()
    {
        if (partyControl != null && partyControl.ControlledCombatController != null)
            return partyControl.ControlledCombatController;

        return playerCombat;
    }

    private void ManualInput()
    {
        // ✅ 컨트롤 중인 플레이어만 블록/오토 토글
        ResolveControlledCombat()?.BlockAutoCombatFor(resumeDelay);

        if (autoMode != null && autoMode.IsAuto)
            autoMode.SetAuto(false);

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        ResumeAfterDelay(_cts.Token).Forget();
    }

    private async UniTaskVoid ResumeAfterDelay(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(resumeDelay), ignoreTimeScale: true, cancellationToken: token);

            // resumeDelay 동안 추가 입력이 없었고(토큰이 취소 안 됐고)
            // 현재 조이스틱도 거의 0이면 Auto ON
            float mag = (joystick != null) ? joystick.Magnitude : 0f;
            if (mag <= idleThreshold)
            {
                if (autoMode != null)
                    autoMode.SetAuto(true);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void Awake()
    {
        if (partyControl == null) partyControl = FindObjectOfType<PartyControlRouter>();
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
