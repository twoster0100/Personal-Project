using System;

namespace MyGame.Application.Save
{
    [Serializable]
    public sealed class PrototypeSaveData
    {
        // 타입 ID는 "리팩토링(네임스페이스 변경)"에도 깨지지 않게 별도 문자열로 유지 추천
        public const string TypeId = "prototype_v1";
        public const int SchemaVersion = 1;

        // ---- 지금 당장 저장해도 의미 있는 최소치(예시) ----
        public bool autoMode;
        public int targetFpsMode; // 0=Idle30, 1=Game60 같은 식으로  규칙 정하면 됨

        // ---- 나중에 확장 가능한 필드 ----
        public int stageIndex;
        public int gold;
    }
}
