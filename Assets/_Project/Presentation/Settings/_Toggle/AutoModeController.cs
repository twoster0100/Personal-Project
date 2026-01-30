using System;
using UnityEngine;
using UnityEngine.Events;

public sealed class AutoModeController : MonoBehaviour
{
    [Serializable] public class BoolEvent : UnityEvent<bool> { }

    [SerializeField] private bool isAuto = true;
    [SerializeField] public BoolEvent onAutoChanged = new();

    public bool IsAuto => isAuto;

    public void ToggleAuto()
    {
        SetAuto(!isAuto);
    }

    public void SetAuto(bool value)
    {
        if (isAuto == value) return;

        isAuto = value;
        onAutoChanged.Invoke(isAuto);
    }

#if UNITY_EDITOR
    // 디버그 버튼: Play 중 AutoMode 컴포넌트 톱니/우클릭 메뉴에서 실행 가능
    [ContextMenu("Debug/Invoke OnAutoChanged")]
    private void DebugInvoke()
    {
        Debug.Log($"[AutoMode] DebugInvoke IsAuto={isAuto}");
        onAutoChanged.Invoke(isAuto);
    }
#endif
}
