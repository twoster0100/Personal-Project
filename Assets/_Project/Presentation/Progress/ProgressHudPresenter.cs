using TMPro;
using UnityEngine;

namespace MyGame.Presentation.Progress
{
    public sealed class ProgressHudPresenter : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private PlayerProgressRuntimeBinding progress;

        [Header("UI (TMP_Text)")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text stageText;

        [Header("Format")]
        [SerializeField] private string goldFormat = "{0:N0}";
        [SerializeField] private string stageFormat = "{0}";

        private void Reset()
        {
            if (progress == null) progress = FindObjectOfType<PlayerProgressRuntimeBinding>();
        }

        private void OnEnable()
        {
            if (progress != null)
                progress.ProgressChanged += OnProgressChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (progress != null)
                progress.ProgressChanged -= OnProgressChanged;
        }

        private void OnProgressChanged(MyGame.Domain.Progress.PlayerProgressChanged _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (progress == null) return;

            if (goldText != null)
                goldText.text = string.Format(goldFormat, progress.Gold);

            if (stageText != null)
                stageText.text = string.Format(stageFormat, progress.StageIndex);
        }
    }
}
