namespace MyGame.Application.Save
{
    /// <summary>
    /// ✅ Port: 직렬화/역직렬화 추상화
    /// - JsonUtility / Newtonsoft / MessagePack 등 교체 가능
    /// </summary>
    public interface ISaveCodec
    {
        string EncodeEnvelope(SaveEnvelope env);
        bool TryDecodeEnvelope(string json, out SaveEnvelope env);

        string EncodePayload<T>(T payload);
        bool TryDecodePayload<T>(string json, out T payload);
    }
}
