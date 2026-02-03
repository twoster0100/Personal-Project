using TMPro;
using UnityEngine;
using MyGame.Domain.Progress;

namespace MyGame.Presentation.Progress
{
    /// <summary>
    ///  통화 HUD (Coin + Gem)
    /// - ProgressChanged 이벤트로 즉시 갱신
    /// </summary>
    public sealed class CurrencyHudPresenter : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private PlayerProgressRuntimeBinding progress;

        [Header("UI")]
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text gemText;

        [Header("Format")]
        [SerializeField] private string coinFormat = "{0:N0}";
        [SerializeField] private string gemFormat = "{0:N0}";

        private void Reset()
        {
            if (progress == null) progress = FindObjectOfType<PlayerProgressRuntimeBinding>();
        }

        private void OnEnable()
        {
            if (progress != null)
                progress.ProgressChanged += OnProgressChanged;

            RefreshAll();
        }

        private void OnDisable()
        {
            if (progress != null)
                progress.ProgressChanged -= OnProgressChanged;
        }

        private void OnProgressChanged(PlayerProgressChanged e)
        {
            // ✅ 필요한 값만 갱신(미세 최적화)
            if ((e.Flags & ProgressChangedFlags.Gold) != 0) RefreshCoin();
            if ((e.Flags & ProgressChangedFlags.Gem) != 0) RefreshGem();
        }

        private void RefreshAll()
        {
            RefreshCoin();
            RefreshGem();
        }

        private void RefreshCoin()
        {
            if (progress == null || coinText == null) return;
            coinText.text = string.Format(coinFormat, progress.Gold);
        }

        private void RefreshGem()
        {
            if (progress == null || gemText == null) return;
            gemText.text = string.Format(gemFormat, progress.Gem);
        }
    }
}
