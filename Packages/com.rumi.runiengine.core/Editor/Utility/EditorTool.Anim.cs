#nullable enable
using System;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public static bool FadeGroup(ref AnimBool animBool, Action action)
        {
            if (EditorGUILayout.BeginFadeGroup(animBool.faded))
            {
                try
                {
                    if (animBool.isAnimating)
                        RepaintCurrentWindow();

                    action.Invoke();
                }
                finally
                {
                    EditorGUILayout.EndFadeGroup();
                    Space(-2f.Lerp(0, animBool.faded).RoundToInt());
                }

                return true;
            }

            EditorGUILayout.EndFadeGroup();

            return false;
        }

        public static bool FadeGroup(ref AnimBool? animBool, bool target, Action action)
        {
            animBool ??= new AnimBool(target);
            animBool.target = target;

            return FadeGroup(ref animBool, action);
        }
    }
}
