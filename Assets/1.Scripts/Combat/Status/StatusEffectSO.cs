using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Combat
{
    public enum StackPolicy
    {
        RefreshDuration, // 같은 효과 재적용 시 지속시간만 갱신
        AddStacks,       // 스택 증가(+지속시간 갱신)
        Independent      // 동일 효과 여러 개가 개별 타이머로 존재
    }

    public enum StatModMode
    {
        AddFlat,   // +10 / -5
        Multiply   // 0.8(20%감소) / 1.2(20%증가)
    }

    [Serializable]
    public struct StatModifier
    {
        public StatId stat;
        public StatModMode mode;
        public float value;

        [Tooltip("스택이 있을 때, 스택당 적용할지 여부")]
        public bool perStack;
    }

    /// <summary>
    /// ✅ 상태이상/버프 데이터(SO)
    /// - 이동불가/기본공격불가/스킬시전불가
    /// - 물리/마법/총기 공격불가 (DamageType 차단)
    /// - 특정 스탯 감소/증가 (AP/AC/LK 등)
    /// - 명중 보장(forceHit)
    /// - (선택) FSM 강제 상태 전이 (예: Stun -> Stunned)
    /// </summary>
    [CreateAssetMenu(menuName = "Combat/Status Effect", fileName = "SE_")]
    public class StatusEffectSO : ScriptableObject
    {
        [Header("Identity")]
        public string effectId = "Stun";

        [Header("Duration")]
        [Tooltip("지속시간(초). -1이면 무한 지속(수동 제거).")]
        public float duration = 1.0f;

        [Header("Stacking")]
        public StackPolicy stackPolicy = StackPolicy.RefreshDuration;
        public int maxStacks = 1;

        [Header("Action Restriction")]
        public bool blockMovement;
        public bool blockBasicAttack;
        public bool blockSkillCast;

        [Header("DamageType Restriction")]
        public List<DamageType> blockedDamageTypes = new();

        [Header("Hit Rule")]
        [Tooltip("명중 판정을 무조건 성공으로 처리(확정 명중)")]
        public bool forceHit;

        [Header("Stat Modifiers")]
        public List<StatModifier> statModifiers = new();

        [Header("Force Combat State (Optional)")]
        [Tooltip("이 효과가 활성화된 동안 FSM을 특정 상태로 강제하고 싶으면 체크")]
        public bool forceStateTransition;

        public CombatStateId forcedState = CombatStateId.Stunned;
    }
}
