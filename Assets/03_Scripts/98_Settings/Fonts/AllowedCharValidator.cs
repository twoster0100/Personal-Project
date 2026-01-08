
using TMPro;
using UnityEngine;

namespace Project.Settings.Fonts
{
    [DisallowMultipleComponent]
    public sealed class AllowedCharValidator : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private TMP_InputField input;

        [Header("Rules")]
        [SerializeField] private bool allowSpace = true;      // 채팅: true / 닉네임: 취향(보통 false)
        [SerializeField] private bool allowNewLine = false;   // 멀티라인 채팅이면 true
        private void Reset()
        {
            input = GetComponent<TMP_InputField>();
        }

        private void OnEnable()
        {
            if (input != null)
                input.onValidateInput += ValidateChar;
        }

        private void OnDisable()
        {
            if (input != null)
                input.onValidateInput -= ValidateChar;
        }

        private char ValidateChar(string text, int charIndex, char addedChar)
        {
            return IsAllowed(addedChar) ? addedChar : '\0';
        }

        private bool IsAllowed(char c)
        {
            // 공백/줄바꿈
            if (c == ' ') return allowSpace;
            if (c == '\n' || c == '\r') return allowNewLine;

            // ASCII(영문/숫자/기호): '!'(33) ~ '~'(126)
            if (c >= 33 && c <= 126) return true;

            // 한글 완성형(가-힣)
            if (c >= '\uAC00' && c <= '\uD7A3') return true;

            // 한글 호환 자모(ㄱ-ㅣ 등)
            if (c >= '\u3131' && c <= '\u318E') return true;

            // 한글 자모(초/중/종성)
            if (c >= '\u1100' && c <= '\u11FF') return true;

            // 한글 자모 확장 A/B
            if (c >= '\uA960' && c <= '\uA97F') return true;
            if (c >= '\uD7B0' && c <= '\uD7FF') return true;

            return false;
        }
    }
}
