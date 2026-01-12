using UnityEngine;
using UnityEngine.Events;

public class AutoModeController : MonoBehaviour
{
     private bool isAuto = true;
     private bool forceAutoOnStart = true;   // 시작 즉시 Auto ON 강제
     private bool notifyOnStart = true;      // UI 토글/아이콘 초기 동기화

    public bool IsAuto => isAuto;

    // UI 갱신용
    public UnityEvent<bool> onAutoChanged;
    private void Start()
    {
        if (forceAutoOnStart)
            isAuto = true;

        if (notifyOnStart)
            onAutoChanged?.Invoke(isAuto);
    }

    public void SetAuto(bool value)
    {
        if (isAuto == value) return;
        isAuto = value;
        onAutoChanged?.Invoke(isAuto);
    }

    public void ToggleAuto()
    {
        SetAuto(!isAuto);
    }
}
