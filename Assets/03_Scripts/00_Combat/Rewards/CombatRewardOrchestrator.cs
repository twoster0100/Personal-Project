using UnityEngine;
using MyGame.Domain.Rewards;
using MyGame.Presentation.Progress;

namespace MyGame.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatRewardOrchestrator : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private PlayerProgressRuntimeBinding progress;
        [SerializeField] private PickupSpawner pickupSpawner;

        [Header("Drop Tables")]
        [SerializeField] private MonsterDropTableSO defaultDropTable;

        [Header("Exp Spawn")]
        [SerializeField] private int expSplitCount = 3;

        [Header("Debug")]
        [SerializeField] private bool logDrops = true;

        private IRng _rng;

        private void Awake()
        {
            _rng = new SystemRng();
        }

        private void Reset()
        {
            if (progress == null) progress = FindObjectOfType<PlayerProgressRuntimeBinding>();
            if (pickupSpawner == null) pickupSpawner = FindObjectOfType<PickupSpawner>();
        }

        private void OnEnable()
        {
            Actor.AnyDied += OnAnyActorDied;
        }

        private void OnDisable()
        {
            Actor.AnyDied -= OnAnyActorDied;
        }

        private void OnAnyActorDied(ActorDeathEvent e)
        {
            if (e.Victim == null) return;
            if (e.Victim.kind != ActorKind.Monster) return;

            DropTable table = null;

            var source = e.Victim.GetComponent<MonsterDropSource>();
            if (source != null && source.CachedTable != null)
                table = source.CachedTable;

            if (table == null && defaultDropTable != null)
                table = defaultDropTable.ToDomain();

            if (table == null)
                table = new DropTable();

            var bundle = DropResolver.Resolve(table, _rng);
            if (bundle.Rewards.Count == 0) return;

            for (int i = 0; i < bundle.Rewards.Count; i++)
            {
                var r = bundle.Rewards[i];

                switch (r.Kind)
                {
                    case RewardKind.Gold:
                        if (progress != null)
                            progress.AddGold(r.Amount, reason: "MonsterDrop_Gold");
                        else
                            Debug.LogWarning($"[Drop] Gold +{r.Amount} (no progress binding)");
                        break;

                    case RewardKind.Exp:
                        if (pickupSpawner != null)
                            pickupSpawner.SpawnExpOrbs(e.WorldPos, (int)r.Amount, expSplitCount);
                        else
                            Debug.LogWarning($"[Drop] Exp {r.Amount} (no pickupSpawner)");
                        break;

                    case RewardKind.Item:
                        if (pickupSpawner != null)
                            pickupSpawner.SpawnItem(e.WorldPos, r.ItemId, (int)r.Amount);
                        else
                            Debug.LogWarning($"[Drop] Item {r.ItemId} x{r.Amount} (no pickupSpawner)");
                        break;
                }
            }

            if (logDrops)
            {
                for (int i = 0; i < bundle.Rewards.Count; i++)
                    Debug.Log($"[Drop] {e.Victim.name} -> {bundle.Rewards[i]}");
            }
        }
    }
}
