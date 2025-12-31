using UnityEngine;

namespace MyGame.Combat
{
    public class MonsterBrain : MonoBehaviour, ICombatBrain
    {
        public float detectRange = 6f;
        public Actor targetOverride;

        public CombatIntent Decide(Actor self)
        {
            if (self == null) return CombatIntent.None;

            Actor target = targetOverride;
            if (target == null || !target.IsAlive) return CombatIntent.None;

            float dist = Vector3.Distance(self.transform.position, target.transform.position);
            if (dist > detectRange) return CombatIntent.None;

            CombatIntent intent;
            intent.Target = target;
            intent.Engage = true;
            intent.RequestedSkill = null; // 몬스터 자동스킬은 CombatController에서 보충 가능
            return intent;
        }
    }
}
