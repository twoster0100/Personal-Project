namespace MyGame.Combat
{
    public enum StatId
    {       // 나중에 스텟창 툴팁 :
        AP, // 물리 일반공격의 공격력과 물리형 스킬의 공격력을 높여준다.
        AC, // 총기 일반공격의 공격력과 총기형 스킬의 공격력을 높여준다. 물리스킬의 경우 상대방의 회피보다 높아야 명중한다.
        AS, // 숫자가 높아질수록 일반 공격속도가 빨라진다.(정확한산식 나중에)

        MP, // 캐릭터의 최대 마나(MP)를 올려준다. 마나 50당 초당 1의 마나회복력을 가진다.
        MA, // 마법 일반공격의 공격력과 마법형 스킬의 파워를 높여준다.
        MD, // 마법형 공격에 대한 방어력을 높여준다. (1 = 1 비율)

        HP, // 캐릭터의 최대 체력(HP)를 높여준다. 
        DP, // 물리형 공격에 대한 방어력을 높여준다. (1 = 1 비율)
        HV, // 물리형 스킬 공격을 회피할 수 있는 확률을 높여준다. 물리스킬의 경우 상대방의 명중보다 높아야 회피한다. 총기형 공격형에 대한 방어력을 높여준다.(1 = 12 비율)

        BF, // 숫자가 높을수록 아이템 습득범위가 증가한다. 재화 드랍확률이 소폭 증가한다.
        TA, // 숫자가 높을수록 상호작용 이벤트 조우 확률이 증가한다. 
        LK  // 마법/총기 스킬의 경우 상대방의 행운보다 높아야 명중/회피한다. 크리티컬 확률이 소폭 증가한다.
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
