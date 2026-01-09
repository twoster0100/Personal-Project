using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
/// <summary>
/// 조이스틱을 움직이면 자동(오토모드)가 비활성화되고,
/// 아무 조작없이 3초가 지나면 자동(오토모드)가 활성화 되게하는 컴포넌트
/// </summary>
public class JoystickAutoBreak : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private AutoModeController autoMode;

    [SerializeField] private float idleThreshold = 0.01f;
    [SerializeField] private float resumeDelay = 3f;

    private CancellationTokenSource _cts;

    public void OnPointerDown(PointerEventData eventData) => ManualInput();
    public void OnDrag(PointerEventData eventData) => ManualInput();
    public void OnPointerUp(PointerEventData eventData) => ManualInput();

    private void ManualInput()
    {
        // 수동 의도면 Auto 끄기
        if (autoMode.IsAuto) autoMode.SetAuto(false);
        // 타이머 리셋
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

            if (joystick.Magnitude <= idleThreshold)                // 3초 후에도 입력이 없으면 Auto ON 복귀
                autoMode.SetAuto(true);
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
