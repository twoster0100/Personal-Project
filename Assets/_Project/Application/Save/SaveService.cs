using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGame.Application.Save
{
    /// <summary>
    /// ✅ Save Orchestrator (Application)
    /// - 버전/마이그레이션/타입 검증을 책임지고
    /// - 실제 저장 매체와 직렬화는 Port(ISaveStore/ISaveCodec)로 뺀다
    /// </summary>
    public sealed class SaveService
    {
        private readonly ISaveStore _store;
        private readonly ISaveCodec _codec;

        private readonly Dictionary<int, ISaveMigration> _migrationsByFrom = new();

        public int CurrentSchemaVersion { get; }

        public SaveService(
            ISaveStore store,
            ISaveCodec codec,
            int currentSchemaVersion,
            IEnumerable<ISaveMigration> migrations = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            CurrentSchemaVersion = currentSchemaVersion;

            if (migrations != null)
            {
                foreach (var m in migrations)
                {
                    if (m == null) continue;
                    _migrationsByFrom[m.FromVersion] = m;
                }
            }
        }

        public static string DefaultKey(string slotId) => $"save_{slotId}.json";

        public async Task<SaveOpResult> SaveAsync<T>(
            string slotId,
            T payload,
            string payloadTypeId = null,
            CancellationToken ct = default)
        {
            try
            {
                string typeId = payloadTypeId ?? typeof(T).FullName;
                string payloadJson = _codec.EncodePayload(payload);

                var env = new SaveEnvelope
                {
                    schemaVersion = CurrentSchemaVersion,
                    payloadType = typeId,
                    payloadJson = payloadJson,
                    utcTicks = DateTime.UtcNow.Ticks
                };

                string envelopeJson = _codec.EncodeEnvelope(env);
                await _store.WriteAsync(DefaultKey(slotId), envelopeJson, ct);
                return SaveOpResult.Ok();
            }
            catch (Exception e)
            {
                return SaveOpResult.Fail(SaveLoadStatus.IOError, e.Message);
            }
        }

        public async Task<SaveLoadResult<T>> LoadAsync<T>(
            string slotId,
            string expectedPayloadTypeId = null,
            bool autoResaveAfterMigration = true,
            CancellationToken ct = default)
        {
            string typeId = expectedPayloadTypeId ?? typeof(T).FullName;

            try
            {
                var read = await _store.ReadAsync(DefaultKey(slotId), ct);
                if (!read.Found || string.IsNullOrEmpty(read.Contents))
                    return SaveLoadResult<T>.Fail(SaveLoadStatus.NotFound, "Save not found.");

                if (!_codec.TryDecodeEnvelope(read.Contents, out var env) || env == null)
                    return SaveLoadResult<T>.Fail(SaveLoadStatus.Corrupt, "Envelope decode failed.");

                if (env.schemaVersion > CurrentSchemaVersion)
                    return SaveLoadResult<T>.Fail(
                        SaveLoadStatus.VersionTooNew,
                        $"Save version({env.schemaVersion}) is newer than current({CurrentSchemaVersion}).");

                if (!string.Equals(env.payloadType, typeId, StringComparison.Ordinal))
                    return SaveLoadResult<T>.Fail(
                        SaveLoadStatus.TypeMismatch,
                        $"PayloadType mismatch. expected={typeId}, actual={env.payloadType}");

                // ✅ Migration
                if (env.schemaVersion < CurrentSchemaVersion)
                {
                    var migResult = ApplyMigrations(env);
                    if (!migResult.Success)
                        return SaveLoadResult<T>.Fail(migResult.Status, migResult.Message);

                    if (autoResaveAfterMigration)
                    {
                        // 마이그레이션 후 최신 포맷으로 재저장(선택)
                        string upgradedJson = _codec.EncodeEnvelope(env);
                        await _store.WriteAsync(DefaultKey(slotId), upgradedJson, ct);
                    }
                }

                if (!_codec.TryDecodePayload<T>(env.payloadJson, out var payload))
                    return SaveLoadResult<T>.Fail(SaveLoadStatus.Corrupt, "Payload decode failed.");

                return SaveLoadResult<T>.Ok(payload);
            }
            catch (Exception e)
            {
                return SaveLoadResult<T>.Fail(SaveLoadStatus.IOError, e.Message);
            }
        }

        private SaveOpResult ApplyMigrations(SaveEnvelope env)
        {
            while (env.schemaVersion < CurrentSchemaVersion)
            {
                if (!_migrationsByFrom.TryGetValue(env.schemaVersion, out var m) || m == null)
                {
                    return SaveOpResult.Fail(
                        SaveLoadStatus.MigrationMissing,
                        $"Missing migration: {env.schemaVersion} -> {env.schemaVersion + 1}");
                }

                string nextJson = m.Migrate(env.payloadJson);
                env.payloadJson = nextJson;
                env.schemaVersion = m.ToVersion;
            }

            return SaveOpResult.Ok();
        }
    }
}
