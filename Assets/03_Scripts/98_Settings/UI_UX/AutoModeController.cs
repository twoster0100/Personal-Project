using UnityEngine;
using UnityEngine.Events;

public class AutoModeController : MonoBehaviour
{
    [SerializeField] private bool isAuto = false;

    public bool IsAuto => isAuto;

    // UI 갱신용(선택)
    public UnityEvent<bool> onAutoChanged;

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
