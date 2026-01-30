using System;

namespace MyGame.Application.Save
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int schemaVersion;
        public string payloadType;     // 안정적인 타입 ID(권장) 또는 typeof(T).FullName
        public string payloadJson;     // payload 자체 JSON
        public long utcTicks;          // 마지막 저장 시간(UTC ticks)
    }
}
