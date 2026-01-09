using UnityEngine;
using UnityEngine.UI;

public class AutoToggleSwapVisual : MonoBehaviour
{
    [SerializeField] private AutoModeController autoMode;
    [SerializeField] private Button toggleButton;

    [Header("Visuals")]
    [SerializeField] private GameObject onVisual;
    [SerializeField] private GameObject offVisual;

    private void Awake()
    {
        if (!toggleButton) toggleButton = GetComponent<Button>();
        toggleButton.onClick.AddListener(() => autoMode.ToggleAuto());
    }

    private void OnEnable()
    {
        autoMode.onAutoChanged.AddListener(Apply);
        Apply(autoMode.IsAuto);
    }

    private void OnDisable()
    {
        autoMode.onAutoChanged.RemoveListener(Apply);
    }

    private void Apply(bool isAuto)
    {
        onVisual.SetActive(isAuto);
        offVisual.SetActive(!isAuto);
    }
}
