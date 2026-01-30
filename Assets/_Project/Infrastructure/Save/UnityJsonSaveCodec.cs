using System;
using UnityEngine;
using MyGame.Application.Save;

namespace MyGame.Infrastructure.Save
{
    /// <summary>
    /// ✅ JsonUtility 기반 Codec
    /// - 현재 단계에서 가장 설치 의존이 적고 빠르게 굴릴 수 있음
    /// - 단, Dictionary/프로퍼티 기반 타입은 제한이 있으니 "필드+Serializable DTO"로 유지
    /// </summary>
    public sealed class UnityJsonSaveCodec : ISaveCodec
    {
        public string EncodeEnvelope(SaveEnvelope env)
        {
            return JsonUtility.ToJson(env, prettyPrint: false);
        }

        public bool TryDecodeEnvelope(string json, out SaveEnvelope env)
        {
            try
            {
                env = JsonUtility.FromJson<SaveEnvelope>(json);
                return env != null;
            }
            catch
            {
                env = null;
                return false;
            }
        }

        public string EncodePayload<T>(T payload)
        {
            return JsonUtility.ToJson(payload, prettyPrint: false);
        }

        public bool TryDecodePayload<T>(string json, out T payload)
        {
            try
            {
                payload = JsonUtility.FromJson<T>(json);
                return payload != null;
            }
            catch
            {
                payload = default;
                return false;
            }
        }
    }
}
