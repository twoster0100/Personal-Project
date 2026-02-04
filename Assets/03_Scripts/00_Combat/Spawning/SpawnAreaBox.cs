using UnityEngine;

namespace MyGame.Combat
{
    [DisallowMultipleComponent]
    public sealed class SpawnAreaBox : MonoBehaviour
    {
        [Header("Area (local space)")]
        [SerializeField] private Vector3 center = Vector3.zero;
        [SerializeField] private Vector3 size = new(150f, 1f, 150f);

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        /// <summary>
        /// 랜덤 스폰 지점 반환(실패 시 transform.position).
        /// MonsterRespawnSystem이 간단히 사용하도록 래핑.
        /// </summary>
        public Vector3 GetRandomPoint()
        {
            return TryGetPoint(out var p) ? p : transform.position;
        }

        /// <summary>
        /// 박스 내부 랜덤 지점(out). y는 center.y 기준(높이 보정은 시스템에서 옵션 처리).
        /// </summary>
        public bool TryGetPoint(out Vector3 point)
        {
            // local box -> world 변환
            var c = center;
            var s = size;

            float rx = Random.Range(-s.x * 0.5f, s.x * 0.5f);
            float ry = Random.Range(-s.y * 0.5f, s.y * 0.5f);
            float rz = Random.Range(-s.z * 0.5f, s.z * 0.5f);

            Vector3 local = c + new Vector3(rx, ry, rz);
            point = transform.TransformPoint(local);
            return true;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.25f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
