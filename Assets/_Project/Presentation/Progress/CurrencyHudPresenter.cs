using TMPro;
using UnityEngine;
using MyGame.Domain.Progress;

namespace MyGame.Presentation.Progress
{
    /// <summary>
    /// Status Bar 영역(코인/젬 등 재화 UI)만 담당하는 Presenter.
    /// - PlayerProgressRuntimeBinding의 변경 이벤트를 구독해 텍스트 갱신.
    /// - 재화가 늘어나면 이 파일만 확장하면 됨.
    /// </summary>
    public sealed class CurrencyHudPresenter : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private PlayerProgressRuntimeBinding progress;

        [Header("UI")]
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text gemText; // 아직 Progress에 gem이 없으면 비워도 됨

        [Header("Format")]
        [SerializeField] private string coinFormat = "{0:N0}";
        [SerializeField] private string gemFormat = "{0:N0}";

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

            if (coinText != null)
                coinText.text = string.Format(coinFormat, progress.Gold);

            // Gem은 아직 모델에 없으니 임시로 0 표시 or 비워둬도 OK
            if (gemText != null)
                gemText.text = string.Format(gemFormat, 0);
        }
    }
}
