using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// ✅ Player 전투 의사결정(Brain)
    /// - CombatController는 Brain이 만든 CombatIntent만 믿고 FSM을 돌림
    /// - 여기서는 "타겟 선정 + Engage 여부"만 결정
    /// 
    /// (확장)
    /// - 왼클 이동은 PlayerInput(이동 전용)에서 처리
    /// - 왼클로 몬스터 클릭 시 target 지정 → Engage=true로 전투 시작
    /// - 스킬 버튼 입력 시 intent.RequestedSkill에 스킬을 담아주면 됨
    /// </summary>
    public class PlayerBrain : MonoBehaviour, ICombatBrain
    {
        [Header("Demo Target (Optional)")]
        public Actor currentTarget;

        [Header("Options")]
        public bool engageWhenHasTarget = true;

        public CombatIntent Decide(Actor self)
        {
            if (self == null) return CombatIntent.None;

            // ✅ (데모) 좌클릭으로 타겟 선택: 클릭한 오브젝트에서 Actor 찾기
            // 2D면 Physics2D.Raycast로 바꿔야 함(아래 주석 참고)
            if (Input.GetMouseButtonDown(0))
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                    {
                        var a = hit.collider.GetComponentInParent<Actor>();
                        if (a != null && a != self)
                        {
                            currentTarget = a;
                        }
                    }
                }
            }

            // 타겟이 없거나 죽었으면 전투 안 함
            if (!engageWhenHasTarget || currentTarget == null || !currentTarget.IsAlive)
                return CombatIntent.None;

            // ✅ 전투 시작 (스킬 요청은 아직 없음)
            CombatIntent intent;
            intent.Target = currentTarget;
            intent.Engage = true;
            intent.RequestedSkill = null;

            return intent;
        }

        /*
        // ✅ 2D 프로젝트면 이렇게 바꾸기 예시:
        // Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        // var hit2D = Physics2D.Raycast(ray.origin, ray.direction, 200f);
        // if (hit2D.collider != null) { var a = hit2D.collider.GetComponentInParent<Actor>(); ... }
        */
    }
}
