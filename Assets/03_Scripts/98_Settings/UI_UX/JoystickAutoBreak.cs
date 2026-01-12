using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using MyGame.Combat;
/// <summary>
/// 조이스틱을 움직이면 자동(오토모드)가 비활성화되고,
/// 아무 조작없이 3초가 지나면 자동(오토모드)가 활성화 되게하는 컴포넌트
/// </summary>
public class JoystickAutoBreak : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private AutoModeController autoMode;
    [SerializeField] private CombatController playerCombat;

    [Header("Tuning")]
    [SerializeField] private float idleThreshold = 0.01f; // 조이스틱 드리프트 잡는 용도
    [SerializeField] private float resumeDelay = 3f; //수동입력 후 자동으로 전환되기까지의 시간

    private CancellationTokenSource _cts;

    public void OnPointerDown(PointerEventData eventData) => ManualInput();
    public void OnDrag(PointerEventData eventData) => ManualInput();
    public void OnPointerUp(PointerEventData eventData) => ManualInput();

    private void ManualInput()
    {
        playerCombat?.BlockAutoCombatFor(resumeDelay);

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

            // 3초 동안 추가 입력이 없었고(토큰이 취소 안 됐고)
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

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
