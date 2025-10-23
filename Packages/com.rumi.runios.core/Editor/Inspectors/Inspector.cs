#nullable enable
using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using RuniOS.Inspectors.Drawers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors
{
    public sealed class Inspector : IInspector
    {
        /// <summary>
        /// 루트 인스펙터를 가져옵니다.
        /// </summary>
        public Inspector rootInspector { get; }
        
        public IInspectable? inspectable { get; private set; }
        public ImmutableArray<IInspectorElement> elements { get; private set; }
        
        public ImmutableArray<IMGUIInspectorDrawer?> drawers { get; private set; } = ImmutableArray<IMGUIInspectorDrawer?>.Empty;
        IEnumerable<InspectorDrawer?> IInspector.drawers => drawers;
        
        public InspectorFlags inspectorFlags { get; private set; }
        
        (string label, string message)? lastException = null;

        public Inspector() => rootInspector = this;
        
        public Inspector(Inspector? rootInspector) : this()
        {
            if (rootInspector != null)
                this.rootInspector = rootInspector;
        }
        
        public Inspector(object instance) : this(new InspectableObject(instance)) { }
        public Inspector(Type type) : this(new InspectableObject(type)) { }
        public Inspector(Type type, params object[] instances) : this(new InspectableObject(type, instances)) { }
        public Inspector(Type type, IEnumerable<object> instances) : this(new InspectableObject(type, instances)) { }

        public Inspector(IInspectable inspectable, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List) : this() => Rebuild(inspectable, flags);
        public Inspector(IEnumerable<IInspectorElement> elements, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List) : this() => Rebuild(elements, flags);

        public void Rebuild(IInspectable inspectable, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (inspectable is IInspectableList inspectableList && flags.HasFlagFast(InspectorFlags.Public | InspectorFlags.Instance | InspectorFlags.List))
            {
                try
                {
                    ListInspectorDrawer drawer = new ListInspectorDrawer(inspectableList, rootInspector);
                    
                    elements = ImmutableArray<IInspectorElement>.Empty;
                    drawers = ImmutableArray.Create<IMGUIInspectorDrawer?>(drawer);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    lastException = (inspectable.inspectionDisplayName, e.ToString());

                    return;
                }
            }
            else
            {
                try
                {
                    elements = inspectable.GetElements(flags).ToImmutableArray();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    lastException = (inspectable.inspectionDisplayName, e.ToString());

                    return;
                }

                drawers = elements.Select(x => IMGUIInspectorDrawer.FindDrawer(x as IInspectorVariableElement, rootInspector)).ToImmutableArray();
            }
            
            this.inspectable = inspectable;
            inspectorFlags = flags;
        }

        public void Rebuild(IEnumerable<IInspectorElement> elements, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            lastException = null;

            elements = elements.Where(x => x.HasFlags(flags));
            
            this.elements = elements.ToImmutableArray();
            drawers = elements.Select(x => IMGUIInspectorDrawer.FindDrawer(x as IInspectorVariableElement, rootInspector)).ToImmutableArray();
            
            inspectorFlags = flags;
        }

        public void DrawLayout(GUIContent? label = null, bool isInArray = false) => Draw(EditorGUILayout.GetControlRect(false, GetHeight(label, inspectorFlags, isInArray)), label, isInArray);
        
        public void Draw(Rect position, GUIContent? label = null, bool isInArray = false)
        {
            if (lastException != null)
            {
                EditorGUI.LabelField(position, label ?? new GUIContent(lastException.Value.label), new GUIContent(lastException.Value.message));
                return;
            }
            
            Rect elementPosition = position;
            foreach (var item in drawers.WhereNotNull())
            {
                GUIContent elementLabel;
                if (inspectable is IInspectableList)
                    elementLabel = label ?? GUIContent.none;
                else
                    elementLabel = new GUIContent(item.element?.displayName ?? string.Empty);
                
                elementPosition.height = item.GetHeight(elementLabel, inspectorFlags, isInArray);

                try
                {
                    item.OnGUI(elementPosition, elementLabel, inspectorFlags, isInArray);
                }
                catch (Exception e)
                {
                    EditorGUI.LabelField(elementPosition, elementLabel, new GUIContent(e.Message));
                }
                
                elementPosition.y += item.GetHeight(label, inspectorFlags, isInArray) + 2;
            }
        }

        public float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            if (lastException != null)
                return EditorGUIUtility.singleLineHeight;
            
            return (drawers.WhereNotNull().Sum(item => item.GetHeight(label, flags, isInArray) + 2) - 2).Clamp(0);
        }
    }
}