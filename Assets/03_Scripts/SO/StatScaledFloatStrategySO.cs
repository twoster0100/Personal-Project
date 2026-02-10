using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// "파생 값(반경/범위/속도 등)"을 스탯으로 스케일링하는 Strategy 베이스.
    /// - 필요할 때 다른 파생값도 같은 방식으로 재사용 가능
    /// </summary>
    public abstract class StatScaledFloatStrategySO : ScriptableObject
    {
        [Header("Stat Source")]
        [SerializeField] private StatId statId = StatId.BF;
        [SerializeField] private StatValueSource valueSource = StatValueSource.FinalWithStatus;

        public StatId StatId => statId;
        public StatValueSource ValueSource => valueSource;

        /// <summary>
        /// baseValue를 statId/valueSource 기반으로 스케일링한 float 값을 반환
        /// </summary>
        public float Evaluate(Actor owner, float baseValue)
        {
            int statValue = GetStatValue(owner, statId, valueSource);
            return EvaluateInternal(owner, baseValue, statValue);
        }

        protected abstract float EvaluateInternal(Actor owner, float baseValue, int statValue);

        protected static int GetStatValue(Actor owner, StatId id, StatValueSource source)
        {
            if (owner == null) return 0;

            return source switch
            {
                StatValueSource.StatLevelSum =>
                    owner.Stats != null ? owner.Stats.GetTotalStatLevel(id) : 0,

                StatValueSource.BaseFinal =>
                    owner.Stats != null ? owner.Stats.GetBaseFinalStat(id) : 0,

                // 버프/디버프까지 포함한 "진짜 최종값"
                StatValueSource.FinalWithStatus =>
                    owner.GetFinalStat(id),

                _ => 0
            };
        }
    }
}
