// Assets/03_Scripts/99_Editor/Fonts/UIStringsCharacterExtractor.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Project.Settings.Fonts;

namespace Project.Editor.Fonts
{
    public static class UIStringsCharacterExtractor
    {
        private const string SettingsAssetPath = "Assets/01_Data/FontBakeSettings.asset";

        [MenuItem("Tools/Fonts/Extract Unique Characters (Settings)")]
        public static void ExtractUniqueCharacters()
        {
            FontBakeSettings settings = LoadOrCreateSettings();
            if (settings == null) return;

            if (settings.uiStrings == null) return;
            if (string.IsNullOrWhiteSpace(settings.outputPath)) return;

            string raw = settings.uiStrings.text ?? string.Empty;
            raw = raw.Replace("\uFEFF", ""); // BOM 제거

            var unique = new HashSet<char>();

            foreach (char c in raw)
            {
                if (settings.excludeLineBreaksAndTabs)
                {
                    if (c == '\r' || c == '\n' || c == '\t')
                        continue;
                }

                if (!settings.includeSpace && c == ' ')
                    continue;

                unique.Add(c);
            }

            // 재현성을 위한 정렬
            List<char> sorted = unique.ToList();
            sorted.Sort();

            string result = new string(sorted.ToArray());

            // 출력 폴더 보장
            string dir = Path.GetDirectoryName(settings.outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(settings.outputPath, result, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            AssetDatabase.Refresh();
        }

        private static FontBakeSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<FontBakeSettings>(SettingsAssetPath);
            if (settings != null) return settings;

            // 없으면 자동 생성
            string dir = Path.GetDirectoryName(SettingsAssetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            settings = ScriptableObject.CreateInstance<FontBakeSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return settings;
        }
    }
}
#endif
