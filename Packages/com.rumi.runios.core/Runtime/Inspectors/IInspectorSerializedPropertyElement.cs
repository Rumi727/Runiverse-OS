#nullable enable
#if UNITY_EDITOR
using System.ComponentModel;

namespace RuniOS.Inspectors
{
    /// <summary>
    /// 이 요소가 속한 <see cref="UnityEditor.SerializedProperty"/>를 가져옵니다. (Editor-only)
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public interface IInspectorSerializedPropertyElement
    {
        /// <summary>
        /// 이 요소가 속한 <see cref="UnityEditor.SerializedProperty"/>를 가져옵니다. (Editor-only)
        /// </summary>
        UnityEditor.SerializedProperty property { get; }
    }
}
#endif