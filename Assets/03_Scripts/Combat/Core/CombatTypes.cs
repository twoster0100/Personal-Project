namespace MyGame.Combat
{
    public enum StatId
    {
        AP, AC, DX,
        MP, MA, MD,
        HP, DP, HV,
        WT, TA, LK
    }

    public enum DamageType
    {
        Physical,
        Magic,
        Gun,
        TrueDamage
    }

    public enum ActorKind
    {
        Player,
        Monster
    }

    public enum CombatStateId
    {
        Idle,
        Chase,
        AttackLoop,
        CastSkill,
        Stunned,
        Dead,
        Respawn
    }

    public struct CombatIntent
    {
        public Actor Target;
        public bool Engage;
        public SkillDefinitionSO RequestedSkill;

        public static CombatIntent None
        {
            get
            {
                CombatIntent i;
                i.Target = null;
                i.Engage = false;
                i.RequestedSkill = null;
                return i;
            }
        }
    }

    public interface ICombatBrain
    {
        CombatIntent Decide(Actor self);
    }
}
