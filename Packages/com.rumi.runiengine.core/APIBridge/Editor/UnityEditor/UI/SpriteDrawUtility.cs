#nullable enable
using System;
using System.Reflection;
using UnityEngine;

namespace RuniEngine.Editor.APIBridge.UnityEditor.UI
{
    public static class SpriteDrawUtility
    {
        public static Type type { get; } = EditorAssemblyManager.UnityEditor_UI.GetType("UnityEditor.UI.SpriteDrawUtility");



        static MethodInfo? m_DrawSprite;
        static readonly object[] mp_DrawSprite = new object[3];
        static readonly Type[] mpt_DrawSprite = new Type[] { typeof(Sprite), typeof(Rect), typeof(Color) };
        public static void DrawSprite(Sprite sprite, Rect drawArea, Color color)
        {
            m_DrawSprite ??= type.GetMethod("DrawSprite", BindingFlags.Public | BindingFlags.Static, null, mpt_DrawSprite, null);

            mp_DrawSprite[0] = sprite;
            mp_DrawSprite[1] = drawArea;
            mp_DrawSprite[2] = color;

            m_DrawSprite.Invoke(null, mp_DrawSprite);
        }

        static MethodInfo? m2_DrawSprite;
        static readonly object[] m2p_DrawSprite = new object[5];
        static readonly Type[] m2pt_DrawSprite = new Type[] { typeof(Texture), typeof(Rect), typeof(Rect), typeof(Rect), typeof(Color) };
        public static void DrawSprite(Texture tex, Rect drawArea, Rect outer, Rect uv, Color color)
        {
            m2_DrawSprite ??= type.GetMethod("DrawSprite", BindingFlags.Public | BindingFlags.Static, null, m2pt_DrawSprite, null);

            m2p_DrawSprite[0] = tex;
            m2p_DrawSprite[1] = drawArea;
            m2p_DrawSprite[2] = outer;
            m2p_DrawSprite[3] = uv;
            m2p_DrawSprite[4] = color;

            m2_DrawSprite.Invoke(null, m2p_DrawSprite);
        }

        static MethodInfo? m3_DrawSprite;
        static readonly object?[] m3p_DrawSprite = new object?[8];
        static readonly Type[] m3pt_DrawSprite = new Type[] { typeof(Texture), typeof(Rect), typeof(Vector4), typeof(Rect), typeof(Rect), typeof(Rect), typeof(Color), typeof(Material) };
        public static void DrawSprite(Texture tex, Rect drawArea, Vector4 padding, Rect outer, Rect inner, Rect uv, Color color, Material? mat)
        {
            m3_DrawSprite ??= type.GetMethod("DrawSprite", BindingFlags.NonPublic | BindingFlags.Static, null, m3pt_DrawSprite, null);

            m3p_DrawSprite[0] = tex;
            m3p_DrawSprite[1] = drawArea;
            m3p_DrawSprite[2] = padding;
            m3p_DrawSprite[3] = outer;
            m3p_DrawSprite[4] = inner;
            m3p_DrawSprite[5] = uv;
            m3p_DrawSprite[6] = color;
            m3p_DrawSprite[7] = mat;

            m3_DrawSprite.Invoke(null, m3p_DrawSprite);
        }
    }
}
