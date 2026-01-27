using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MyGame.Application.Tick;
using MyGame.Application;

namespace MyGame.Combat
{
    /// <summary>
    /// ✅ Actor에 붙는 상태이상 관리자
    /// - Apply/만료/중첩
    /// - CanMove/CanCastSkill/CanUseDamageType 같은 쿼리 제공
    /// - ModifyStat로 최종 스탯에 버프/디버프 적용
    /// - Stun 같은 효과는 forceStateTransition으로 FSM 강제 전이 가능
    /// - 30Hz SimulationTick으로 만료 처리
    /// - OnEnable/OnDisable에서 Register/Unregister (규약: Dispose/해제 강제)
    /// </summary>
    [DisallowMultipleComponent]
    public class StatusController : MonoBehaviour, ISimulationTickable
    {
        [Serializable]
        private class ActiveEffect
        {
            public StatusEffectSO def;
            public float remaining;
            public int stacks;
        }

        // 런타임 상태는 저장되면 안 됨 (예측불가 현상 유발)
        [NonSerialized] private readonly List<ActiveEffect> active = new();

        private void OnEnable()
        {
            App.RegisterWhenReady(this);
        }

        private void OnDisable()
        {
            App.UnregisterTickable(this);
        }

        /// <summary>
        /// ✅ 30Hz 고정 시뮬레이션 Tick에서 상태 만료 처리
        /// - 프레임레이트(60/30)에 영향받지 않음
        /// </summary>
        public void SimulationTick(float dt)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                if (e.def == null) { active.RemoveAt(i); continue; }

                if (e.def.duration < 0f) continue; // 무한 지속

                e.remaining -= dt;
                if (e.remaining <= 0f) active.RemoveAt(i);
            }
        }

        public void Apply(StatusEffectSO effect)
        {
            if (effect == null) return;

            var existing = Find(effect);

            if (existing == null)
            {
                active.Add(new ActiveEffect
                {
                    def = effect,
                    remaining = effect.duration,
                    stacks = 1
                });
                return;
            }

            switch (effect.stackPolicy)
            {
                case StackPolicy.RefreshDuration:
                    existing.remaining = effect.duration;
                    break;

                case StackPolicy.AddStacks:
                    existing.stacks = Mathf.Min(existing.stacks + 1, Mathf.Max(1, effect.maxStacks));
                    existing.remaining = effect.duration;
                    break;

                case StackPolicy.Independent:
                    active.Add(new ActiveEffect
                    {
                        def = effect,
                        remaining = effect.duration,
                        stacks = 1
                    });
                    break;
            }
        }

        public void Remove(StatusEffectSO effect)
        {
            if (effect == null) return;
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i].def == effect) active.RemoveAt(i);
        }

        public void ClearAll() => active.Clear();

        private ActiveEffect Find(StatusEffectSO effect)
        {
            for (int i = 0; i < active.Count; i++)
                if (active[i].def == effect) return active[i];
            return null;
        }

        // =========================
        // ✅ 쿼리(행동/타입 제한)
        // =========================
        public bool CanMove()
        {
            foreach (var e in active)
                if (e.def != null && e.def.blockMovement) return false;
            return true;
        }

        public bool CanBasicAttack()
        {
            foreach (var e in active)
                if (e.def != null && e.def.blockBasicAttack) return false;
            return true;
        }

        public bool CanCastSkill()
        {
            foreach (var e in active)
                if (e.def != null && e.def.blockSkillCast) return false;
            return true;
        }

        public bool CanUseDamageType(DamageType type)
        {
            foreach (var e in active)
            {
                if (e.def == null) continue;
                if (e.def.blockedDamageTypes != null && e.def.blockedDamageTypes.Contains(type))
                    return false;
            }
            return true;
        }

        public bool ForceHit()
        {
            foreach (var e in active)
                if (e.def != null && e.def.forceHit) return true;
            return false;
        }

        public int ModifyStat(StatId stat, int baseFinalValue)
        {
            float add = 0f;
            float mul = 1f;

            foreach (var e in active)
            {
                if (e.def == null || e.def.statModifiers == null) continue;

                int stacks = Mathf.Max(1, e.stacks);

                foreach (var m in e.def.statModifiers)
                {
                    if (m.stat != stat) continue;

                    int k = m.perStack ? stacks : 1;

                    if (m.mode == StatModMode.AddFlat)
                        add += m.value * k;
                    else
                        mul *= m.perStack ? Mathf.Pow(m.value, k) : m.value;
                }
            }

            float result = (baseFinalValue + add) * mul;
            if (result < 0f) result = 0f;
            return Mathf.FloorToInt(result);
        }

        // ✅ 강제 상태(FSM 전이용)
        public bool TryGetForcedState(out CombatStateId stateId)
        {
            foreach (var e in active)
            {
                if (e == null || e.def == null) continue;

                if (e.def.forceStateTransition)
                {
                    stateId = e.def.forcedState;
                    return true;
                }
            }

            stateId = default;
            return false;
        }

        // =========================
        // ✅ 디버그
        // =========================
        public string DebugDump()
        {
            if (active.Count == 0) return "(none)";

            var sb = new StringBuilder();
            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e.def == null) continue;

                sb.Append(e.def.effectId);
                sb.Append($" x{Mathf.Max(1, e.stacks)}");
                sb.Append($" ({(e.def.duration < 0f ? "INF" : e.remaining.ToString("0.00"))}s)");

                if (i < active.Count - 1) sb.Append(", ");
            }
            return sb.ToString();
        }
    }
}
