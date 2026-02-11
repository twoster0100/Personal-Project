using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Combat
{
    [DisallowMultipleComponent]
    public class Actor : MonoBehaviour
    {
        [Header("Identity")]
        public ActorKind kind = ActorKind.Monster;

        [Header("Movement/Range")]
        public float walkSpeed = 3.5f;
        public float attackRange = 2f;

        [Header("Basic Attack")]
        public float baseAttackInterval = 3.0f;
        public float minAttackInterval = 0.5f;

        [Header("Skill List (optional)")]
        public List<SkillDefinitionSO> skills = new();

        [Header("Death/Respawn")]
        public bool useRespawn = false;
        public float respawnDelay = 3f;

        public ActorStats Stats { get; private set; }
        public StatusController Status { get; private set; }

        public int CurrentHP { get; private set; }
        public int MaxHP { get; private set; }

        public bool IsAlive => CurrentHP > 0;

        public event Action<ActorDeathEvent> Died;
        public static event Action<ActorDeathEvent> AnyDied;

        private bool _deathRaised;
        private readonly Dictionary<SkillDefinitionSO, float> skillCooldownRemaining = new();
        private readonly List<SkillDefinitionSO> skillCooldownKeys = new();

        private void Awake()
        {
            Stats = GetComponent<ActorStats>();
            Status = GetComponent<StatusController>();
        }

        private void Start()
        {
            RefreshMaxHP();
            CurrentHP = MaxHP;
            _deathRaised = false;
        }

        public void RefreshMaxHP()
        {
            MaxHP = Mathf.Max(1, GetFinalStat(StatId.HP));
            CurrentHP = Mathf.Min(CurrentHP, MaxHP);
        }

        public int GetFinalStat(StatId id)
        {
            if (Stats == null) return 0;

            int baseFinal = Stats.GetBaseFinalStat(id);
            if (Status != null)
                return Status.ModifyStat(id, baseFinal);

            return baseFinal;
        }

        public float GetAttackInterval()
        {
            int dx = GetFinalStat(StatId.AS);
            float speed = 1f + dx * 0.01f;
            float interval = baseAttackInterval / Mathf.Max(0.1f, speed);
            return Mathf.Max(minAttackInterval, interval);
        }

        public bool IsSkillReady(SkillDefinitionSO skill)
        {
            if (skill == null) return false;
            if (!skillCooldownRemaining.TryGetValue(skill, out float remain)) return true;
            return remain <= 0f;
        }

        public void ConsumeSkillCooldown(SkillDefinitionSO skill)
        {
            if (skill == null) return;
            skillCooldownRemaining[skill] = Mathf.Max(0f, skill.cooldown);
        }

        public void TickSkillCooldowns(float dt)
        {
            if (dt <= 0f) return;
            if (skillCooldownRemaining.Count == 0) return;

            skillCooldownKeys.Clear();
            foreach (var kvp in skillCooldownRemaining)
                skillCooldownKeys.Add(kvp.Key);

            for (int i = 0; i < skillCooldownKeys.Count; i++)
            {
                var skill = skillCooldownKeys[i];
                float remain = skillCooldownRemaining[skill] - dt;
                if (remain <= 0f)
                    skillCooldownRemaining.Remove(skill);
                else
                    skillCooldownRemaining[skill] = remain;
            }
        }

        public void TakeDamage(int amount, Actor source)
        {
            if (!IsAlive) return;

            amount = Mathf.Max(0, amount);
            CurrentHP -= amount;

            if (CurrentHP <= 0) CurrentHP = 0;

            if (CurrentHP == 0)
                RaiseDeathOnce(source);
        }

        public void Kill(Actor killer = null)
        {
            if (!IsAlive) return;
            CurrentHP = 0;
            RaiseDeathOnce(killer);
        }

        private void RaiseDeathOnce(Actor killer)
        {
            if (_deathRaised) return;
            _deathRaised = true;

            var ev = new ActorDeathEvent(this, killer, transform.position, Time.time);

            // Debug.Log($"[DEAD] {name} died. killer={(killer != null ? killer.name : "Unknown")}");

            Died?.Invoke(ev);
            AnyDied?.Invoke(ev);
        }

        public void RespawnNow()
        {
            Status?.ClearAll();
            skillCooldownRemaining.Clear();

            RefreshMaxHP();
            CurrentHP = MaxHP;

            _deathRaised = false;

            Debug.Log($"[RESPAWN] {name} HP={CurrentHP}/{MaxHP} (DP={GetFinalStat(StatId.DP)}, AP={GetFinalStat(StatId.AP)}) | Status={Status?.DebugDump() ?? "(no StatusController)"}");
        }

        public void ReturnToPoolOrDisable()
        {
            gameObject.SetActive(false);
        }
    }
}
