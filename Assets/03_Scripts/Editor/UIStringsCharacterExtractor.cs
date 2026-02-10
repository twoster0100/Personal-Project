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
    public static class FontBakeExtractor
    {
        private const string SettingsAssetPath = "Assets/01_Data/FontBakeSettings.asset";

        [MenuItem("Tools/Fonts/Generate Character Sets (All Profiles)")]
        public static void GenerateAll()
        {
            FontBakeSettings settings = LoadOrCreateSettings();
            if (settings == null || settings.profiles == null || settings.profiles.Length == 0)
                return;

            foreach (var profile in settings.profiles)
            {
                GenerateOne(profile);
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Fonts/Generate Character Set (Selected Profile Index 0)")]
        private static void GenerateProfile0() => GenerateByIndex(0);

        [MenuItem("Tools/Fonts/Generate Character Set (Selected Profile Index 1)")]
        private static void GenerateProfile1() => GenerateByIndex(1);

        [MenuItem("Tools/Fonts/Generate Character Set (Selected Profile Index 2)")]
        private static void GenerateProfile2() => GenerateByIndex(2);

        private static void GenerateByIndex(int index)
        {
            FontBakeSettings settings = LoadOrCreateSettings();
            if (settings == null || settings.profiles == null) return;
            if (index < 0 || index >= settings.profiles.Length) return;

            GenerateOne(settings.profiles[index]);
            AssetDatabase.Refresh();
        }

        private static void GenerateOne(FontBakeProfile profile)
        {
            if (profile == null) return;
            if (profile.sourceText == null) return;
            if (string.IsNullOrWhiteSpace(profile.outputPath)) return;

            string raw = profile.sourceText.text ?? string.Empty;
            raw = raw.Replace("\uFEFF", ""); // BOM 제거

            var unique = new HashSet<char>();
            foreach (char c in raw)
            {
                if (profile.excludeLineBreaksAndTabs && (c == '\r' || c == '\n' || c == '\t'))
                    continue;

                if (!profile.includeSpace && c == ' ')
                    continue;

                unique.Add(c);
            }

            List<char> sorted = unique.ToList();
            sorted.Sort();
            string result = new string(sorted.ToArray());

            string dir = Path.GetDirectoryName(profile.outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(profile.outputPath, result, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static FontBakeSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<FontBakeSettings>(SettingsAssetPath);
            if (settings != null) return settings;

            string dir = Path.GetDirectoryName(SettingsAssetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            settings = ScriptableObject.CreateInstance<FontBakeSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
#endif
