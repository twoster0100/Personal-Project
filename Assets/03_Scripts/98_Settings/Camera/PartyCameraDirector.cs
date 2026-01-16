using Cinemachine;
using UnityEngine;
/// <summary>
/// 캐릭터 카메라 전환 기능
/// </summary>
public sealed class PartyCameraDirector : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera[] vcams;
    [SerializeField] private int basePriority = 10;
    [SerializeField] private int activeBoost = 20;

    private int activeIndex = -1;

    private void Awake()
    {
        if (vcams == null || vcams.Length == 0)
        {
            Debug.LogError($"{nameof(PartyCameraDirector)}: vcams not assigned.");
            enabled = false;
            return;
        }

        Focus(0); // 시작은 0번 캐릭터
    }

    public void Focus(int index)
    {
        if ((uint)index >= (uint)vcams.Length) return;
        if (activeIndex == index) return;

        for (int i = 0; i < vcams.Length; i++)
        {
            var v = vcams[i];
            if (v == null) continue;
            v.Priority = basePriority + (i == index ? activeBoost : 0);
        }

        activeIndex = index;
    }

#if UNITY_EDITOR
    private void Update()
    {
        // UI 연결 전 임시 테스트
        if (Input.GetKeyDown(KeyCode.Z)) Focus(0);
        if (Input.GetKeyDown(KeyCode.X)) Focus(1);
        if (Input.GetKeyDown(KeyCode.C)) Focus(2);
        if (Input.GetKeyDown(KeyCode.V)) Focus(3);
    }
#endif
}
