// Assets/03_Scripts/98_Settings/Fonts/FontBakeSettings.cs
using UnityEngine;

namespace Project.Settings.Fonts
{
    [CreateAssetMenu(menuName = "Settings/Fonts/Font Bake Settings", fileName = "FontBakeSettings")]
    public sealed class FontBakeSettings : ScriptableObject
    {
        [Header("Source")]
        public TextAsset uiStrings;

        [Header("Output (Project Relative Path)")]
        [Tooltip("예: Assets/04_ResourcesData/Fonts/Generated/UI_Characters_Unique.txt")]
        public string outputPath = "Assets/04_ResourcesData/Fonts/Generated/UI_Characters_Unique.txt";

        [Header("Options")]
        public bool includeSpace = true;
        public bool excludeLineBreaksAndTabs = true;
    }
}
