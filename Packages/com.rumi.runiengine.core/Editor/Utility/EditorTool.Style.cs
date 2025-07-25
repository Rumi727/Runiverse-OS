#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public static GUIStyle labelStyle => GUI.skin.label;
        public static GUIStyle richLabelStyle
        {
            get
            {
                _richLabelStyle ??= new GUIStyle(labelStyle)
                {
                    richText = true
                };

                return _richLabelStyle;
            }
        }
        static GUIStyle? _richLabelStyle;
        public static GUIStyle boldLabelStyle => EditorStyles.boldLabel;
        public static GUIStyle largeLabelStyle => EditorStyles.largeLabel;
        public static GUIStyle labelButtonStyle
        {
            get
            {
                _labelButtonStyle ??= new GUIStyle(labelStyle)
                {
                    hover = new GUIStyleState()
                    {
                        textColor = new Color(0, 0.2352941176f, 0.5333333333f)
                    },
                    active = new GUIStyleState()
                    {
                        textColor = new Color(0, 0.2352941176f * 2, 0.5333333333f * 2)
                    }
                };

                return _labelButtonStyle;
            }
        }
        static GUIStyle? _labelButtonStyle;

        public static GUIStyle editorLabelStyle => EditorStyles.label;

        public static GUIStyle helpBoxStyle => EditorStyles.helpBox;

        public static GUIStyle otherHelpBoxStyle
        {
            get
            {
                _otherHelpBox ??= new GUIStyle(helpBoxStyle)
                {
                    padding = new RectOffset(10),
                    margin = new RectOffset(10)
                };

                return _otherHelpBox;
            }
        }
        static GUIStyle? _otherHelpBox;



        public static void BeginMinLabelWidth(float min = 120, float offset = 0) => BeginMinLabelWidth(min, APIBridge.UnityEditor.EditorGUIUtility.contextWidth, offset);
        public static void BeginMinLabelWidth(float min, float contextWidth, float offset) => BeginLabelWidth(Mathf.Max((contextWidth * 0.45f) - 40f, min) + offset);

        public static void BeginLabelWidth(string label) => BeginLabelWidth(new GUIContent(label));
        public static void BeginLabelWidth(GUIContent label) => BeginLabelWidth(label, editorLabelStyle);
        public static void BeginLabelWidth(string label, GUIStyle style) => BeginLabelWidth(new GUIContent(label), style);
        public static void BeginLabelWidth(GUIContent label, GUIStyle style) => BeginLabelWidth(GetXSize(label, style) + 2);

        public static void BeginLabelWidth(params string[] label) => BeginLabelWidth(label, editorLabelStyle);
        public static void BeginLabelWidth(params GUIContent[] label) => BeginLabelWidth(label, editorLabelStyle);
        public static void BeginLabelWidth(string[] label, GUIStyle style) => BeginLabelWidth(GetLabelXSize(label, style) + 2);
        public static void BeginLabelWidth(GUIContent[] label, GUIStyle style) => BeginLabelWidth(GetLabelXSize(label, style) + 2);

        static readonly Stack<float> labelWidthStack = new Stack<float>();
        public static void BeginLabelWidth(float width)
        {
            labelWidthStack.Push(APIBridge.UnityEditor.EditorGUIUtility.s_LabelWidth);
            EditorGUIUtility.labelWidth = width;
        }

        public static void EndLabelWidth()
        {
            if (labelWidthStack.TryPop(out float result))
                EditorGUIUtility.labelWidth = result;
            else
                EditorGUIUtility.labelWidth = 0;
        }

        public static float GetXSize(GUIStyle style) => GetXSize(new GUIContent(), style);
        public static float GetYSize(GUIStyle style) => GetYSize(new GUIContent(), style);

        public static float GetXSize(string label, GUIStyle style) => GetXSize(new GUIContent(label), style);
        public static float GetYSize(string label, GUIStyle style) => GetYSize(new GUIContent(label), style);

        public static float GetXSize(GUIContent content, GUIStyle style) => style.CalcSize(content).x;
        public static float GetYSize(GUIContent content, GUIStyle style) => style.CalcSize(content).y;

        public static float GetButtonYSize() => GetYSize(GUI.skin.button);

        public static float GetLabelXSize(string label) => GetLabelXSize(new GUIContent(label));
        public static float GetLabelXSize(GUIContent label) => GetXSize(label, labelStyle);

        public static float GetLabelYSize(string label) => GetLabelYSize(new GUIContent(label));
        public static float GetLabelYSize(GUIContent label) => GetYSize(label, labelStyle);

        public static float GetLabelXSize(params string[] label) => GetLabelXSize(label, labelStyle);
        public static float GetLabelXSize(params GUIContent[] label) => GetLabelXSize(label, labelStyle);
        public static float GetLabelXSize(string[] label, GUIStyle style)
        {
            float width = 0;
            for (int i = 0; i < label.Length; i++)
                width = width.Max(style.CalcSize(new GUIContent(label[i])).x);

            return width;
        }

        public static float GetLabelXSize(GUIContent[] label, GUIStyle style)
        {
            float width = 0;
            for (int i = 0; i < label.Length; i++)
                width = width.Max(style.CalcSize(label[i]).x);

            return width;
        }

        static readonly Stack<float> fieldWidthQueue = new Stack<float>();
        public static void BeginFieldWidth(float width)
        {
            fieldWidthQueue.Push(APIBridge.UnityEditor.EditorGUIUtility.s_FieldWidth);
            EditorGUIUtility.fieldWidth = width;
        }

        public static void EndFieldWidth()
        {
            if (fieldWidthQueue.TryPop(out float result))
                EditorGUIUtility.fieldWidth = result;
            else
                EditorGUIUtility.fieldWidth = 0;
        }



        static readonly Dictionary<GUIStyle, Stack<TextAnchor>> alignmentStacks = new Dictionary<GUIStyle, Stack<TextAnchor>>();
        public static void BeginAlignment(TextAnchor alignment, GUIStyle style)
        {
            if (!alignmentStacks.ContainsKey(style))
                alignmentStacks.Add(style, new Stack<TextAnchor>());

            alignmentStacks[style].Push(style.alignment);
            style.alignment = alignment;
        }

        public static void EndAlignment(GUIStyle style)
        {
            if (alignmentStacks.ContainsKey(style))
            {
                Stack<TextAnchor> stack = alignmentStacks[style];
                if (stack.TryPop(out TextAnchor result))
                    style.alignment = result;
                else
                    style.alignment = 0;

                if (stack.Count <= 0)
                    alignmentStacks.Remove(style);

                return;
            }
            else
                style.alignment = 0;
        }



        public static void BeginFontSize(int size) => BeginFontSize(size, labelStyle);
        public static void EndFontSize() => EndFontSize(labelStyle);

        static readonly Dictionary<GUIStyle, Stack<int>> fontSizeStacks = new Dictionary<GUIStyle, Stack<int>>();
        public static void BeginFontSize(int size, GUIStyle style)
        {
            if (!fontSizeStacks.ContainsKey(style))
                fontSizeStacks.Add(style, new Stack<int>());

            fontSizeStacks[style].Push(style.fontSize);
            style.fontSize = size;
        }

        public static void EndFontSize(GUIStyle style)
        {
            if (fontSizeStacks.ContainsKey(style))
            {
                Stack<int> stack = fontSizeStacks[style];
                if (stack.TryPop(out int result))
                    style.fontSize = result;
                else
                    style.fontSize = 0;

                if (stack.Count <= 0)
                    fontSizeStacks.Remove(style);

                return;
            }
            else
                style.fontSize = 0;
        }



        static readonly Stack<bool> wideModeStack = new Stack<bool>();
        public static void BeginWideMode(bool width)
        {
            wideModeStack.Push(EditorGUIUtility.wideMode);
            EditorGUIUtility.wideMode = width;
        }

        public static void EndWideMode()
        {
            if (wideModeStack.TryPop(out bool result))
                EditorGUIUtility.wideMode = result;
            else
                EditorGUIUtility.wideMode = false;
        }



        public static void BeginIndentLevel()
        {
            //++ 연산자는 바꾸기 전 값을 반환함
            indentLevelStack.Push(EditorGUI.indentLevel++);
        }

        static readonly Stack<int> indentLevelStack = new Stack<int>();
        public static void BeginIndentLevel(int indentLevel)
        {
            indentLevelStack.Push(EditorGUI.indentLevel);
            EditorGUI.indentLevel = indentLevel;
        }

        public static void EndIndentLevel()
        {
            if (indentLevelStack.TryPop(out int result))
                EditorGUI.indentLevel = result;
            else
                EditorGUI.indentLevel = 0;
        }



#if UNITY_2023_1_OR_NEWER
        public static string RichMSpace(object value) => RichMSpace(value, "7.6");
        public static string RichMSpace(object value, string width) => $"<mspace={width}>{value}</mspace>";

        public static string RichNumberMSpace(object value) => RichNumberMSpace(value, "7.6");
        public static string RichNumberMSpace(object value, string width)
        {
            System.Text.StringBuilder stringBuilder = StringBuilderCache.Acquire();

            string text = value.ToString();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsNumber(c))
                    stringBuilder.Append($"<mspace={width}>{c}</mspace>");
                else
                    stringBuilder.Append(c);
            }

            return StringBuilderCache.Release(stringBuilder);
        }
#else
        public static string RichMSpace(object value) => value.ToString();
        public static string RichMSpace(object value, string width) => value.ToString();

        public static string RichNumberMSpace(object value) => value.ToString();
        public static string RichNumberMSpace(object value, string width) => value.ToString();
#endif
    }
}
