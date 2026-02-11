#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MyGame.EditorTools
{
    public sealed class OfflineAfkAutomationWindow : EditorWindow
    {
        private const string WindowTitle = "Offline AFK Automation";
        private const string PlayerProgressType = "player_progress_v1";
        private const string LastSeenKey = "lastSeenUtcTicks";

        [SerializeField] private string targetJsonPath = string.Empty;
        [SerializeField] private long secondsAgo = 600;

        [Header("Backup")]
        [SerializeField] private bool createBackupBeforeWrite = true;
        [SerializeField] private bool appendTimestampToBackup = true;

        [Header("Log")]
        [SerializeField] private bool verboseLog = true;

        [MenuItem("Tools/Offline AFK/Automation")]
        public static void Open()
        {
            var window = GetWindow<OfflineAfkAutomationWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(560f, 280f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Offline AFK Save Debugger", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "progress_0.json 경로를 지정하고 lastSeenUtcTicks를 과거로 조정합니다.\n" +
                "Envelope(payloadJson 문자열) 포맷과 Raw Payload 포맷을 자동으로 처리합니다.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Target JSON", GUILayout.Width(84f));
                targetJsonPath = EditorGUILayout.TextField(targetJsonPath);

                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                {
                    string startDir = string.IsNullOrWhiteSpace(targetJsonPath)
                        ? Application.persistentDataPath
                        : Path.GetDirectoryName(targetJsonPath);

                    string picked = EditorUtility.OpenFilePanel(
                        "Select progress_0.json",
                        string.IsNullOrEmpty(startDir) ? Application.persistentDataPath : startDir,
                        "json");

                    if (!string.IsNullOrWhiteSpace(picked))
                        targetJsonPath = picked;
                }
            }

            secondsAgo = EditorGUILayout.LongField("Seconds Ago", Math.Max(0L, secondsAgo));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Backup Options", EditorStyles.boldLabel);
            createBackupBeforeWrite = EditorGUILayout.ToggleLeft("Create Backup Before Write", createBackupBeforeWrite);

            using (new EditorGUI.DisabledScope(!createBackupBeforeWrite))
            {
                appendTimestampToBackup = EditorGUILayout.ToggleLeft("Append Timestamp To Backup Name", appendTimestampToBackup);
            }

            verboseLog = EditorGUILayout.ToggleLeft("Verbose Log", verboseLog);

            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(targetJsonPath)))
            {
                if (GUILayout.Button("Adjust lastSeenUtcTicks", GUILayout.Height(34f)))
                    AdjustLastSeenTicks();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "권장 절차: 1) 파일 선택 → 2) Seconds Ago 입력 → 3) Adjust 버튼 실행 → 4) Play Mode에서 [OfflineAFK] 로그 확인",
                MessageType.None);
        }

        private void AdjustLastSeenTicks()
        {
            if (string.IsNullOrWhiteSpace(targetJsonPath))
            {
                Debug.LogWarning("[OfflineAFK][Editor] Target JSON path is empty.");
                return;
            }

            if (!File.Exists(targetJsonPath))
            {
                Debug.LogWarning($"[OfflineAFK][Editor] File not found: {targetJsonPath}");
                return;
            }

            try
            {
                string original = File.ReadAllText(targetJsonPath);
                JObject root = JObject.Parse(original);

                bool isEnvelope = root["payloadJson"] != null;
                string payloadType = root["payloadType"]?.Value<string>() ?? string.Empty;

                JObject payload;
                if (isEnvelope)
                {
                    string payloadJson = root["payloadJson"]?.Value<string>();
                    if (string.IsNullOrWhiteSpace(payloadJson))
                    {
                        Debug.LogWarning("[OfflineAFK][Editor] payloadJson is missing or empty.");
                        return;
                    }

                    payload = JObject.Parse(payloadJson);
                }
                else
                {
                    payload = root;
                    payloadType = PlayerProgressType;
                }

                if (!string.Equals(payloadType, PlayerProgressType, StringComparison.Ordinal))
                {
                    Debug.LogWarning(
                        $"[OfflineAFK][Editor] payloadType mismatch. expected={PlayerProgressType}, actual={payloadType}.\n" +
                        "선택한 파일이 player_progress_v1 저장본인지 확인하세요.");
                    return;
                }

                long nowTicks = DateTime.UtcNow.Ticks;
                long targetTicks = nowTicks - (Math.Max(0L, secondsAgo) * TimeSpan.TicksPerSecond);

                bool hadKey = payload[LastSeenKey] != null;
                payload[LastSeenKey] = targetTicks;

                if (isEnvelope)
                    root["payloadJson"] = payload.ToString(Formatting.None);

                string backupPath = null;
                if (createBackupBeforeWrite)
                    backupPath = CreateBackupFile(targetJsonPath, appendTimestampToBackup);

                string output = isEnvelope
                    ? root.ToString(Formatting.Indented)
                    : payload.ToString(Formatting.Indented);

                File.WriteAllText(targetJsonPath, output);

                string utc = new DateTime(targetTicks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
                if (hadKey)
                {
                    Debug.Log($"[OfflineAFK][Editor] Updated {LastSeenKey}={targetTicks} ({utc})\nfile={targetJsonPath}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[OfflineAFK][Editor] {LastSeenKey} key was missing. Added and set to {targetTicks} ({utc}).\n" +
                        $"file={targetJsonPath}");
                }

                if (verboseLog)
                {
                    Debug.Log($"[OfflineAFK][Editor] backup={(string.IsNullOrEmpty(backupPath) ? "(skipped)" : backupPath)}");
                    Debug.Log($"[OfflineAFK][Editor] detectedFormat={(isEnvelope ? "Envelope(payloadJson)" : "RawPayload")}, payloadType={payloadType}");
                }

                AssetDatabase.Refresh();
            }
            catch (JsonReaderException jex)
            {
                Debug.LogError($"[OfflineAFK][Editor] Invalid JSON format. {jex.Message}\nfile={targetJsonPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OfflineAFK][Editor] Failed to adjust lastSeenUtcTicks. {ex}");
            }
        }
        private static string CreateBackupFile(string sourcePath, bool appendTimestamp)
        {
            string directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            string fileName = Path.GetFileName(sourcePath);

            string backupFileName;
            if (appendTimestamp)
            {
                string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                backupFileName = $"{fileName}.{stamp}.bak";
            }
            else
            {
                backupFileName = fileName + ".bak";
            }

            string backupPath = Path.Combine(directory, backupFileName);
            File.Copy(sourcePath, backupPath, overwrite: true);
            return backupPath;
        }
    }
}
#endif
