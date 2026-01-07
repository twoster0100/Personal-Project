using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class GlobalBlueHeaderEditor : Editor
{
    // --- Colors ---
    private readonly Color _bgColor = new Color(0.06f, 0.07f, 0.08f);
    private readonly Color _hoverColor = new Color(0.12f, 0.14f, 0.16f);
    private readonly Color _btnColor = new Color(0.26f, 0.26f, 0.26f);
    private readonly Color _btnHoverColor = new Color(0.4f, 0.4f, 0.4f);

    // --- Layout Constants ---
    private const float HeaderHeight = 32f;
    private const float BtnSize = 24f;
    private const float BtnSpacing = 2f; // [중요] 모든 버튼 사이의 간격을 2px로 통일

    private static GUIStyle _textStyle;
    private static GUIStyle _btnTextStyle;
    private static Font _cachedFont;

    public override void OnInspectorGUI()
    {
        if (ShouldDrawDefault())
        {
            DrawDefaultInspector();
            return;
        }
        DrawCustomHeader();
        DrawCustomBody();
    }

    private bool ShouldDrawDefault()
    {
        if (target == null) return true;
        string ns = target.GetType().Namespace;
        if (string.IsNullOrEmpty(ns)) return false;
        return ns.StartsWith("UnityEngine") || ns.StartsWith("UnityEditor") ||
               ns.StartsWith("Sirenix") || ns.StartsWith("TMPro");
    }

    private void DrawCustomHeader()
    {
        InitializeStyles();

        Rect rect = GUILayoutUtility.GetRect(0, HeaderHeight + 4, GUILayout.ExpandWidth(true));
        Rect headerRect = new Rect(rect.x - 1, rect.y + 2, rect.width + 2, HeaderHeight);

        Event evt = Event.current;
        bool isHover = headerRect.Contains(evt.mousePosition);

        EditorGUI.DrawRect(headerRect, isHover ? _hoverColor : _bgColor);

        // --- [핵심 수정] 좌표 계산 로직 개선 (오른쪽 -> 왼쪽 순차 계산) ---
        // 오른쪽 끝에서 시작 (5px 여백)
        float currentX = headerRect.xMax - 5f;
        float btnY = headerRect.y + (HeaderHeight - BtnSize) / 2;

        // 1. Down Button (▼)
        currentX -= BtnSize;
        Rect downBtnRect = new Rect(currentX, btnY, BtnSize, BtnSize);

        // 2. Up Button (▲)
        currentX -= (BtnSize + BtnSpacing); // 버튼크기 + 간격(2px) 만큼 왼쪽으로 이동
        Rect upBtnRect = new Rect(currentX, btnY, BtnSize, BtnSize);

        // 3. Reset Button (?)
        currentX -= (BtnSize + BtnSpacing); // 동일하게 버튼크기 + 간격(2px) 만큼 이동 -> 간격 일정함 보장
        Rect resetBtnRect = new Rect(currentX, btnY, BtnSize, BtnSize);
        // -----------------------------------------------------------

        string title = ObjectNames.NicifyVariableName(target.GetType().Name).ToUpper();
        float labelWidth = resetBtnRect.xMin - headerRect.x - 10;
        Rect labelRect = new Rect(headerRect.x + 12, headerRect.y, labelWidth, headerRect.height);

        GUI.Label(labelRect, title, _textStyle);

        DrawFlatButton(upBtnRect, "▲", () => {
            foreach (var t in targets) ComponentUtility.MoveComponentUp((MonoBehaviour)t);
        });
        DrawFlatButton(downBtnRect, "▼", () => {
            foreach (var t in targets) ComponentUtility.MoveComponentDown((MonoBehaviour)t);
        });
        DrawFlatButton(resetBtnRect, "\u21BA", () => {
            foreach (var t in targets) ResetComponent((MonoBehaviour)t);
        });

        if (isHover && evt.type == EventType.MouseDown && evt.button == 0)
        {
            if (!upBtnRect.Contains(evt.mousePosition) &&
                !downBtnRect.Contains(evt.mousePosition) &&
                !resetBtnRect.Contains(evt.mousePosition))
            {
                MonoScript script = MonoScript.FromMonoBehaviour((MonoBehaviour)target);
                if (script != null) AssetDatabase.OpenAsset(script);
                evt.Use();
            }
        }

        if (isHover) EditorGUIUtility.AddCursorRect(headerRect, MouseCursor.Link);
        GUILayout.Space(2);
    }

    private void ResetComponent(MonoBehaviour targetComponent)
    {
        if (targetComponent == null) return;
        Undo.RecordObject(targetComponent, "Reset Component");
        GameObject tempGO = new GameObject();
        try
        {
            Component tempComponent = tempGO.AddComponent(targetComponent.GetType());
            EditorUtility.CopySerialized(tempComponent, targetComponent);
        }
        finally { DestroyImmediate(tempGO); }
    }

    private void DrawFlatButton(Rect rect, string icon, System.Action onClick)
    {
        Event evt = Event.current;
        bool isHover = rect.Contains(evt.mousePosition);
        EditorGUI.DrawRect(rect, isHover ? _btnHoverColor : _btnColor);
        GUI.Label(rect, icon, _btnTextStyle);
        if (isHover && evt.type == EventType.MouseDown && evt.button == 0)
        {
            onClick?.Invoke();
            evt.Use();
        }
    }

    private void DrawCustomBody()
    {
        serializedObject.Update();
        SerializedProperty prop = serializedObject.GetIterator();
        if (prop.NextVisible(true))
        {
            do
            {
                if (prop.name == "m_Script") continue;
                EditorGUILayout.PropertyField(prop, true);
            } while (prop.NextVisible(false));
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void InitializeStyles()
    {
        if (_textStyle == null)
        {
            _textStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 2, 0)
            };
            _textStyle.normal.textColor = Color.white;
            _textStyle.hover.textColor = Color.white;
            _textStyle.active.textColor = Color.white;
            _textStyle.onNormal.textColor = Color.white;
            _textStyle.onHover.textColor = Color.white;
        }

        if (_btnTextStyle == null)
        {
            _btnTextStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                font = EditorStyles.standardFont
            };
            _btnTextStyle.normal.textColor = Color.white;
            _btnTextStyle.hover.textColor = Color.white;
            _btnTextStyle.active.textColor = Color.white;
        }

        if (_cachedFont == null)
            _cachedFont = LoadFontByName("Oswald-Medium") ?? LoadFontByName("Oswald-Light");

        if (_cachedFont != null && _textStyle.font != _cachedFont)
            _textStyle.font = _cachedFont;
    }

    private Font LoadFontByName(string fontName)
    {
        string[] guids = AssetDatabase.FindAssets($"{fontName} t:Font");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }
}