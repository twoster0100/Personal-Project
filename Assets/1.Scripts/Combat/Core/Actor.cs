using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// ✅ 전투 주체(플레이어/몬스터 공용)
    /// - Stats(성장/투자/장비) + Status(버프/디버프) => GetFinalStat
    /// - HP/사망/부활 최소 구현
    /// - 스킬 쿨다운 최소 구현
    /// </summary>
    [DisallowMultipleComponent]
    public class Actor : MonoBehaviour
    {
        [Header("Identity")]
        public ActorKind kind = ActorKind.Monster;

        [Header("Movement/Range")]
        public float walkSpeed = 3.5f;
        public float attackRange = 2f;

        [Header("Basic Attack")]
        public float baseAttackInterval = 1.0f;

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

        // 스킬 쿨다운(간단 버전)
        private readonly Dictionary<SkillDefinitionSO, float> nextSkillReadyTime = new();

        private void Awake()
        {
            Stats = GetComponent<ActorStats>();
            Status = GetComponent<StatusController>();
        }

        private void Start()
        {
            RefreshMaxHP();
            CurrentHP = MaxHP;
        }

        public void RefreshMaxHP()
        {
            MaxHP = Mathf.Max(1, GetFinalStat(StatId.HP));
            CurrentHP = Mathf.Min(CurrentHP, MaxHP);
        }

        /// <summary>✅ 최종 스탯 제공(무조건 이 함수로만 스탯 읽기)</summary>
        public int GetFinalStat(StatId id)
        {
            if (Stats == null) return 0;

            int baseFinal = Stats.GetBaseFinalStat(id);
            if (Status != null)
                return Status.ModifyStat(id, baseFinal);

            return baseFinal;
        }

        /// <summary>✅ 일반공격 속도(DX 기반)</summary>
        public float GetAttackInterval()
        {
            int dx = GetFinalStat(StatId.DX);
            float speed = 1f + dx * 0.01f;
            return baseAttackInterval / Mathf.Max(0.1f, speed);
        }

        // ===== 스킬 쿨다운(최소) =====
        public bool IsSkillReady(SkillDefinitionSO skill)
        {
            if (skill == null) return false;
            if (!nextSkillReadyTime.TryGetValue(skill, out float t)) return true;
            return Time.time >= t;
        }

        public void ConsumeSkillCooldown(SkillDefinitionSO skill)
        {
            if (skill == null) return;
            nextSkillReadyTime[skill] = Time.time + Mathf.Max(0f, skill.cooldown);
        }

        // ===== 데미지/사망 =====
        public void TakeDamage(int amount, Actor source)
        {
            if (!IsAlive) return;

            amount = Mathf.Max(0, amount);
            CurrentHP -= amount;

            if (CurrentHP <= 0) CurrentHP = 0;

            Debug.Log($"[DMG] {name} -{amount} from {(source ? source.name : "Unknown")}  HP={CurrentHP}/{MaxHP}");

            if (CurrentHP == 0)
                Debug.Log($"[DEAD] {name} died.");
        }

        public void RespawnNow()
        {
            // ✅ “부활하면 무엇을 리셋할지” 정책을 여기서 통제
            // 1) 상태이상 제거(원하면 Debuff만 제거로 분리 가능)
            Status?.ClearAll();

            // 2) 스킬 쿨다운 초기화(원하면 유지/부분 유지로 변경 가능)
            nextSkillReadyTime.Clear();

            RefreshMaxHP();
            CurrentHP = MaxHP;

            Debug.Log($"[RESPAWN] {name} HP={CurrentHP}/{MaxHP} (DP={GetFinalStat(StatId.DP)}, AP={GetFinalStat(StatId.AP)}) | Status={Status?.DebugDump() ?? "(no StatusController)"}");
        }

        public void ReturnToPoolOrDisable()
        {
            gameObject.SetActive(false);
        }
    }
}
