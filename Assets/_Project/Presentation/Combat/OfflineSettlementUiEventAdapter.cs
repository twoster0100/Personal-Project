using System;
using UnityEngine;
using UnityEngine.Events;

namespace MyGame.Presentation.Combat
{
    /// <summary>
    /// CombatBootPresenter의 정산 결과를 UnityEvent/UI 레이어로 안전 전달하는 어댑터.
    /// </summary>
    public sealed class OfflineSettlementUiEventAdapter : MonoBehaviour
    {
        [Serializable]
        public sealed class OfflineSettlementEvent : UnityEvent<OfflineSettlementUiPayload> { }

        [Header("Source")]
        [SerializeField] private CombatBootPresenter combatBootPresenter;

        [Header("Dispatch")]
        [SerializeField] private bool replayLatestOnEnable = true;
        [SerializeField] private OfflineSettlementEvent onSettlement = new();

        public event Action<OfflineSettlementUiPayload> SettlementRaised;

        public bool HasLastPayload { get; private set; }
        public OfflineSettlementUiPayload LastPayload { get; private set; }

        private void Reset()
        {
            if (combatBootPresenter == null)
                combatBootPresenter = FindObjectOfType<CombatBootPresenter>(true);
        }

        private void OnEnable()
        {
            if (combatBootPresenter == null)
                combatBootPresenter = FindObjectOfType<CombatBootPresenter>(true);

            if (combatBootPresenter == null)
                return;

            combatBootPresenter.OfflineSettlementApplied += Forward;

            if (replayLatestOnEnable && combatBootPresenter.LastOfflineSettlement.HasReward)
                Forward(combatBootPresenter.LastOfflineSettlement);
        }

        private void OnDisable()
        {
            if (combatBootPresenter != null)
                combatBootPresenter.OfflineSettlementApplied -= Forward;
        }

        public bool TryReplayLatest(Action<OfflineSettlementUiPayload> listener)
        {
            if (!HasLastPayload || listener == null)
                return false;

            listener.Invoke(LastPayload);
            return true;
        }

        private void Forward(OfflineSettlementUiPayload payload)
        {
            LastPayload = payload;
            HasLastPayload = payload.HasReward;

            if (!HasLastPayload)
                return;

            SettlementRaised?.Invoke(payload);
            onSettlement?.Invoke(payload);
        }
    }
}
