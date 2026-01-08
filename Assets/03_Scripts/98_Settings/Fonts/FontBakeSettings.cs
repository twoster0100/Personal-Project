// Assets/03_Scripts/98_Settings/Fonts/FontBakeSettings.cs
using UnityEngine;

namespace Project.Settings.Fonts
{
    [System.Serializable]
    public sealed class FontBakeProfile
    {
        [Tooltip("프로파일 식별용")]
        public string name = "Profile";

        [Header("Source")]
        public TextAsset sourceText;

        [Header("Output (Project Relative Path)")]
        [Tooltip("예: Assets/04_ResourcesData/Fonts/Generated/UI_Characters_Unique.txt")]
        public string outputPath = "Assets/04_ResourcesData/Fonts/Generated/Characters_Unique.txt";

        [Header("Options")]
        public bool includeSpace = true;
        public bool excludeLineBreaksAndTabs = true;
    }

    [CreateAssetMenu(menuName = "Settings/Fonts/Font Bake Settings", fileName = "FontBakeSettings")]
    public sealed class FontBakeSettings : ScriptableObject
    {
        [Header("Profiles (UI / Item / Skill / Conversation)")]
        public FontBakeProfile[] profiles = new FontBakeProfile[]
        {
            new FontBakeProfile{ name = "UI",
                outputPath = "Assets/04_ResourcesData/Fonts/Generated/UI_Characters_Unique.txt" },
            new FontBakeProfile { name="Item",
                outputPath="Assets/04_ResourcesData/Fonts/Generated/Item_Characters_Unique.txt" },
            new FontBakeProfile { name="Skill",
                outputPath="Assets/04_ResourcesData/Fonts/Generated/Skill_Characters_Unique.txt" },
            new FontBakeProfile { name="Conversation",
                outputPath="Assets/04_ResourcesData/Fonts/Generated/Conversation_Characters_Unique.txt" },

        };
    }
}
