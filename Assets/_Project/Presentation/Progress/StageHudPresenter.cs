using TMPro;
using UnityEngine;
using MyGame.Domain.Progress;

namespace MyGame.Presentation.Progress
{
    /// <summary>
    /// ✅ 스테이지 HUD
    /// - 기본: stageIndex 그대로 표시
    /// - 옵션: "챕터-스테이지" 형태로 표시 (1-2 같은)
    /// </summary>
    public sealed class StageHudPresenter : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private PlayerProgressRuntimeBinding progress;

        [Header("UI")]
        [SerializeField] private TMP_Text stageText;

        [Header("Display")]
        [SerializeField] private bool useChapterDashFormat = true;
        [SerializeField] private int stagesPerChapter = 10;
        [SerializeField] private string rawFormat = "{0}";
        [SerializeField] private string chapterDashFormat = "{0}-{1}"; // chapter-stage

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

        private void OnProgressChanged(PlayerProgressChanged e)
        {
            if ((e.Flags & ProgressChangedFlags.StageIndex) == 0) return;
            Refresh();
        }

        private void Refresh()
        {
            if (progress == null || stageText == null) return;

            int s = progress.StageIndex;

            if (!useChapterDashFormat)
            {
                stageText.text = string.Format(rawFormat, s);
                return;
            }

            // 기본 정책: 10스테이지 = 1챕터 (수정시 stagesPerChapter만 바꾸면 됨)
            int size = Mathf.Max(1, stagesPerChapter);
            int chapter = ((s - 1) / size) + 1;
            int stageInChapter = ((s - 1) % size) + 1;

            stageText.text = string.Format(chapterDashFormat, chapter, stageInChapter);
        }
    }
}
