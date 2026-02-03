using TMPro;
using UnityEngine;
using MyGame.Domain.Progress;

namespace MyGame.Presentation.Progress
{
    /// <summary>
    /// 상단 중앙 StageHUD만 담당하는 Presenter.
    /// - 현재는 stageIndex(int) 하나를 표시.
    /// - 나중에 난이도/챕터/웨이브 등으로 확장하기 쉽다.
    /// </summary>
    public sealed class StageHudPresenter : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private PlayerProgressRuntimeBinding progress;

        [Header("UI")]
        [SerializeField] private TMP_Text stageText;

        [Header("Format")]
        [Tooltip("예: '1-{0}' => 1-1, 1-2... / 'Stage {0}'")]
        [SerializeField] private string stageFormat = "1-{0}";

        private void Reset()
        {
            if (progress == null) progress = FindObjectOfType<PlayerProgressRuntimeBinding>();
        }

        private void OnEnable()
        {
            if (progress != null) progress.ProgressChanged += OnProgressChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (progress != null) progress.ProgressChanged -= OnProgressChanged;
        }

        private void OnProgressChanged(PlayerProgressChanged _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (progress == null) return;
            if (stageText == null) return;

            stageText.text = string.Format(stageFormat, progress.StageIndex);
        }
    }
}
