using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// "이동 적용"을 담당하는  인터페이스
    /// - CombatController는 이 인터페이스에만 의존
    /// </summary>
    public interface IMover
    {
        void SetDesiredMove(Vector3 worldMoveDir01);
        void Stop();
    }
}
