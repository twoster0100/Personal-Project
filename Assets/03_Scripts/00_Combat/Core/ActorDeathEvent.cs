using UnityEngine;

namespace MyGame.Combat
{
    public readonly struct ActorDeathEvent
    {
        public readonly Actor Victim;
        public readonly Actor Killer;
        public readonly Vector3 WorldPos;
        public readonly float Time;

        public ActorDeathEvent(Actor victim, Actor killer, Vector3 worldPos, float time)
        {
            Victim = victim;
            Killer = killer;
            WorldPos = worldPos;
            Time = time;
        }
    }
}
