using System;
using UnityEngine;

namespace MyGame.Application.Save
{
    /// <summary>
    /// "설정 전용" 세이브 데이터.
    /// - 게임 진행(스테이지/골드 등)과 분리해서 덮어쓰기 사고를 방지한다.
    /// </summary>
    [Serializable]
    public sealed class SettingsSaveData
    {
        public const string TypeId = "settings_v1";

        [Header("Settings")]
        public bool autoMode = true;
        public int targetFpsMode = 0;
    }
}
