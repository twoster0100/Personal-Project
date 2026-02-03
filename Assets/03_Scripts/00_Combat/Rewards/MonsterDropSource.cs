using UnityEngine;
using MyGame.Domain.Rewards;

namespace MyGame.Combat
{
    [DisallowMultipleComponent]
    public sealed class MonsterDropSource : MonoBehaviour
    {
        [SerializeField] private MonsterDropTableSO dropTable;
        private DropTable _cached;
        public DropTable CachedTable => _cached;

        private void Awake()
        {
            _cached = dropTable != null ? dropTable.ToDomain() : null;
        }
    }
}
