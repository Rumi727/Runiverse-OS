#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System.Collections;
using UnityEditor;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(ICollection))]
    public class CollectionInspectorDrawer : ObjectInspectorDrawer
    {
        CollectionInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }

        static float helpBoxYSize => GetYSize(GetTextOrKey("inspector.invalid.collection"), EditorStyles.helpBox);
        
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false)
        {
            position.height = helpBoxYSize;
            EditorGUI.HelpBox(position, GetTextOrKey("inspector.invalid.collection"), MessageType.Warning);
            
            position.y += helpBoxYSize + 2;
            position.height = base.GetHeight(label, flags, isInArray);
            
            base.OnGUI(position, label, flags, isInArray);
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => helpBoxYSize + 2 + base.GetHeight(label, flags, isInArray);

    }
}