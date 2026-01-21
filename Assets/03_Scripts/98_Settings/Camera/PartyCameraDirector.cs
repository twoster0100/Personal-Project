using Cinemachine;
using UnityEngine;

public sealed class PartyCameraDirector : MonoBehaviour
{
    [Header("Single VCam")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    [Header("Look-ahead Proxy (VCam always follows this)")]
    [SerializeField] private LookAheadFollowProxy followProxy;

    [Header("Party Camera Pivots (order = portraits)")]
    [SerializeField] private Transform[] cameraPivots;

    [Header("Start Focus")]
    [SerializeField] private int startIndex = 0;

    private int currentIndex = -1;

#if UNITY_EDITOR
    [ContextMenu("Focus 0 (Bunny)")] private void Focus0() => Focus(0);
    [ContextMenu("Focus 1")] private void Focus1() => Focus(1);
    [ContextMenu("Focus 2")] private void Focus2() => Focus(2);
    [ContextMenu("Focus 3")] private void Focus3() => Focus(3);
#endif

    private void Awake()
    {
        if (vcam == null || followProxy == null || cameraPivots == null || cameraPivots.Length == 0)
        {
            Debug.LogError($"{nameof(PartyCameraDirector)}: Missing references.");
            enabled = false;
            return;
        }

        // VCam은 항상 Proxy만 Follow
        vcam.Follow = followProxy.transform;

        Focus(startIndex, snap: true);
    }

    public void Focus(int index) => Focus(index, snap: false);

    private void Focus(int index, bool snap)
    {
        if ((uint)index >= (uint)cameraPivots.Length) return;
        var pivot = cameraPivots[index];
        if (pivot == null) return;
        if (currentIndex == index) return;

        followProxy.SetTarget(pivot, snap);
        currentIndex = index;
    }
}
