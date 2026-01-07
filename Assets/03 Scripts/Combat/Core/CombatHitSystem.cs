namespace MyGame.Combat
{
    /// <summary>
    /// ✅ 비교식 명중 판정(결정론)
    /// - Physical: caster.AC >= target.HV  => Hit
    /// - Magic/Gun: caster.LK >= target.LK => Hit
    /// - TrueDamage: Always Hit
    /// - forceHit: 스킬 자체 or 상태이상으로 확정 명중 가능
    /// </summary>
    public static class CombatHitSystem
    {
        public struct HitResult
        {
            public bool isHit;
            public bool forcedHit;
            public int attackerValue;
            public int defenderValue;
        }

        public static HitResult CheckHit(Actor caster, Actor target, DamageType type, bool skillForceHit)
        {
            if (caster == null || target == null) return new HitResult { isHit = false };
            if (!caster.IsAlive || !target.IsAlive) return new HitResult { isHit = false };

            // 0) 확정 명중
            bool forced = skillForceHit || (caster.Status != null && caster.Status.ForceHit());
            if (forced)
            {
                return new HitResult
                {
                    isHit = true,
                    forcedHit = true,
                    attackerValue = 0,
                    defenderValue = 0
                };
            }

            // 1) 타입별 비교식 판정
            switch (type)
            {
                case DamageType.Physical:
                    {
                        int ac = caster.GetFinalStat(StatId.AC);
                        int hv = target.GetFinalStat(StatId.HV);
                        return new HitResult
                        {
                            isHit = ac >= hv,
                            forcedHit = false,
                            attackerValue = ac,
                            defenderValue = hv
                        };
                    }

                case DamageType.Magic:
                case DamageType.Gun:
                    {
                        int atkLK = caster.GetFinalStat(StatId.LK);
                        int defLK = target.GetFinalStat(StatId.LK);
                        return new HitResult
                        {
                            isHit = atkLK >= defLK,
                            forcedHit = false,
                            attackerValue = atkLK,
                            defenderValue = defLK
                        };
                    }

                case DamageType.TrueDamage:
                default:
                    return new HitResult { isHit = true, forcedHit = false };
            }
        }
    }
}
