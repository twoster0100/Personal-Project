namespace MyGame.Application.Save
{
    /// <summary>
    /// ✅ Migration: (버전 n) payloadJson -> (버전 n+1) payloadJson 변환
    /// - "한 단계씩" 올리는 규칙을 추천(디버깅 쉬움)
    /// </summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }     // 보통 FromVersion + 1
        string Migrate(string oldPayloadJson);
    }
}
