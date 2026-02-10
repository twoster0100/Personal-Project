using UnityEngine;

namespace MyGame.Combat
{
    public interface IBasicAttackStrategy
    {
        void PerformAttack(Actor caster, Actor target);
    }

    public interface ISkillSelectorStrategy
    {
        SkillDefinitionSO SelectSkill(Actor caster, Actor target);
    }

    public interface ISkillExecutorStrategy
    {
        void Execute(Actor caster, Actor target, SkillDefinitionSO skill);
    }

    /// <summary>
    /// 기본공격(근접) 전략
    /// - Physical Hit(AC>=HV) 성공 시에만 데미지 적용
    /// </summary>
    public class MeleeBasicAttackStrategy : IBasicAttackStrategy
    {
        public void PerformAttack(Actor caster, Actor target)
        {
            if (caster == null || target == null) return;
            if (!caster.IsAlive || !target.IsAlive) return;

            // 1) Hit 판정(비교식)
            var hit = CombatHitSystem.CheckHit(caster, target, DamageType.Physical, skillForceHit: false);
            if (!hit.isHit)
            {
                Debug.Log($"[BasicAttack] Miss (AC={hit.attackerValue}, HV={hit.defenderValue})");
                return;
            }

            // 2) Hit면 데미지
            int dmg = DamageResolver.ResolveBasicAttackDamage(caster, target);
            target.TakeDamage(dmg, caster);
        }
    }

    /// <summary>
    ///  자동 스킬 선택(몬스터용 예시)
    /// - caster.skills에서 사용 가능(태그/쿨) 한 첫 스킬 선택
    /// </summary>
    public class FirstReadySkillSelector : ISkillSelectorStrategy
    {
        public SkillDefinitionSO SelectSkill(Actor caster, Actor target)
        {
            if (caster == null || caster.skills == null) return null;

            for (int i = 0; i < caster.skills.Count; i++)
            {
                var s = caster.skills[i];
                if (s == null) continue;
                if (!s.CanBeUsedBy(caster)) continue;
                if (!caster.IsSkillReady(s)) continue;

                return s;
            }

            return null;
        }
    }

    /// <summary>
    ///  즉시 데미지 스킬 실행 전략
    /// - Hit 성공 시에만 (데미지 + onHitEffects) 적용
    /// </summary>
    public class InstantDamageSkillExecutor : ISkillExecutorStrategy
    {
        public void Execute(Actor caster, Actor target, SkillDefinitionSO skill)
        {
            if (caster == null || target == null || skill == null) return;
            if (!caster.IsAlive || !target.IsAlive) return;

            // 1) Hit 판정(비교식)
            var hit = CombatHitSystem.CheckHit(caster, target, skill.damageType, skill.forceHit);
            if (!hit.isHit)
            {
                Debug.Log($"[Skill] Miss skill={skill.skillId} (A={hit.attackerValue}, D={hit.defenderValue})");
                return;
            }

            // 2) Hit면 데미지
            int dmg = DamageResolver.ResolveSkillDamage(caster, target, skill);
            target.TakeDamage(dmg, caster);

            // 3) Hit면 상태이상 적용
            if (target.Status != null && skill.onHitEffects != null)
            {
                for (int i = 0; i < skill.onHitEffects.Count; i++)
                {
                    var eff = skill.onHitEffects[i];
                    if (eff == null) continue;
                    target.Status.Apply(eff, caster);
                }
            }
        }

    }
}
