using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Combat
{
    public enum AllowedCasterTags
    {
        Player,
        Monster,
        Both
    }

    /// <summary>
    /// ✅ 공용 스킬 데이터
    /// - damageType에 따라 Hit 규칙/데미지 규칙이 자동 선택됨
    /// - onHitEffects는 "명중했을 때만" 적용됨
    /// </summary>
    [CreateAssetMenu(menuName = "Combat/Skill Definition", fileName = "SK_")]
    public class SkillDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string skillId = "Skill";

        [Header("Caster Restriction")]
        public AllowedCasterTags allowedCasters = AllowedCasterTags.Both;

        [Header("Core")]
        public DamageType damageType = DamageType.Magic;
        public int basePower = 10;

        [Header("Timing/Range")]
        public float castTime = 0f;
        public float range = 0f;

        [Header("Cooldown")]
        public float cooldown = 1.0f;

        [Header("Hit Rule")]
        public bool forceHit = false; // 스킬 자체 확정 명중

        [Header("On Hit Effects (Hit일 때만 적용)")]
        public List<StatusEffectSO> onHitEffects = new();

        public bool CanBeUsedBy(Actor caster)
        {
            if (caster == null) return false;

            return allowedCasters switch
            {
                AllowedCasterTags.Both => true,
                AllowedCasterTags.Player => caster.kind == ActorKind.Player,
                AllowedCasterTags.Monster => caster.kind == ActorKind.Monster,
                _ => true
            };
        }
    }
}
