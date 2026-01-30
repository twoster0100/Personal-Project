using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    ///  데미지 공식 모음
    /// - 일반공격: max(1, AP - DP)
    /// - 스킬: 타입별로 (basePower + 공격계수 - 방어계수) 임시 기본 틀 
    /// </summary>
    public static class DamageResolver
    {
        public static int ResolveBasicAttackDamage(Actor caster, Actor target)
        {
            int ap = caster.GetFinalStat(StatId.AP);
            int dp = target.GetFinalStat(StatId.DP);
            return Mathf.Max(1, ap - dp);
        }

        public static int ResolveSkillDamage(Actor caster, Actor target, SkillDefinitionSO skill)
        {
            int basePower = Mathf.Max(0, skill.basePower);

            return skill.damageType switch
            {
                DamageType.Physical =>
                    Mathf.Max(1, basePower + caster.GetFinalStat(StatId.AP) - target.GetFinalStat(StatId.DP)),

                DamageType.Magic =>
                    Mathf.Max(1, basePower + caster.GetFinalStat(StatId.MA) - target.GetFinalStat(StatId.MD)),

                // 총기: 데미지는 AC vs HV로 설계(문서 구조와 잘 맞음)
                DamageType.Gun =>
                    Mathf.Max(1, basePower + caster.GetFinalStat(StatId.AC) - target.GetFinalStat(StatId.HV)),

                DamageType.TrueDamage =>
                    Mathf.Max(1, basePower),

                _ =>
                    Mathf.Max(1, basePower)
            };
        }
    }
}
