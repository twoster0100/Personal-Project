using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// 공식:
    /// result = baseValue + add + (statValue * perStat)
    /// 
    /// 예) BF 1당 +0.1 반경 증가:
    /// - statId = BF
    /// - valueSource = FinalWithStatus
    /// - perStat = 0.1
    /// </summary>
    [CreateAssetMenu(
        menuName = "MyGame/Combat/Stat Scaling/Add (base + stat*mult)",
        fileName = "Scale_Add_"
    )]
    public sealed class StatScaleAddPerStatSO : StatScaledFloatStrategySO
    {
        [Header("Formula: base + add + (stat * perStat)")]
        [SerializeField] private float perStat = 0.1f;
        [SerializeField] private float add = 0f;

        [Header("Clamp")]
        [SerializeField] private float min = 0.1f;
        [SerializeField] private bool clampMax = false;
        [SerializeField] private float max = 999f;

        protected override float EvaluateInternal(Actor owner, float baseValue, int statValue)
        {
            float s = Mathf.Max(0, statValue);
            float r = baseValue + add + (s * perStat);

            if (clampMax) r = Mathf.Min(r, max);
            return Mathf.Max(min, r);
        }
    }
}
