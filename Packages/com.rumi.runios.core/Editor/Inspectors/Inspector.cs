#nullable enable
using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using RuniOS.Inspectors.Drawers;
using RuniOS.Linq;
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

        public IInspectorElement? element => elements.Length == 1 ? elements[0] : null;
        public ImmutableArray<IInspectorElement> elements { get; private set; } = ImmutableArray<IInspectorElement>.Empty;
        
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
            Func<(Type type, CustomInspectorDrawerAttribute attribute), bool>? predicate = null;
            if (flags.HasFlagFast(InspectorFlags.Debug))
                predicate = x => x.attribute.allowInDebug;
            
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

                drawers = elements.Select(x => IMGUIInspectorDrawer.FindDrawer(x as IInspectorVariableElement, rootInspector, predicate)).ToImmutableArray();
            }
            
            this.inspectable = inspectable;
            inspectorFlags = flags;
        }

        public void Rebuild(IInspectorElement element, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool skipFlagCheck = false)
        {
            lastException = null;
            if (!element.HasFlags(flags) && !skipFlagCheck)
                return;
            
            Func<(Type type, CustomInspectorDrawerAttribute attribute), bool>? predicate = null;
            if (flags.HasFlagFast(InspectorFlags.Debug))
                predicate = x => x.attribute.allowInDebug;

            elements = ImmutableArray.Create(element);
            drawers = ImmutableArray.Create(IMGUIInspectorDrawer.FindDrawer(element as IInspectorVariableElement, rootInspector, predicate));

            inspectorFlags = flags;
        }

        public void Rebuild(IEnumerable<IInspectorElement> elements, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool skipFlagCheck = false)
        {
            lastException = null;

            if (!skipFlagCheck)
                elements = elements.Where(x => x.HasFlags(flags));

            Func<(Type type, CustomInspectorDrawerAttribute attribute), bool>? predicate = null;
            if (flags.HasFlagFast(InspectorFlags.Debug))
                predicate = x => x.attribute.allowInDebug;
            
            this.elements = elements.ToImmutableArray();
            drawers = elements.Select(x => IMGUIInspectorDrawer.FindDrawer(x as IInspectorVariableElement, rootInspector, predicate)).ToImmutableArray();
            
            inspectorFlags = flags;
        }


        public void DrawLayout(string? label = null, bool isInArray = false) => DrawLayout(label != null ? new GUIContent(label) : null, isInArray);
        public void DrawLayout(GUIContent? label, bool isInArray = false) => Draw(EditorGUILayout.GetControlRect(false, GetHeight(label, inspectorFlags, isInArray)), label, isInArray);

        public void Draw(Rect position, string? label = null, bool isInArray = false) => Draw(position, label != null ? new GUIContent(label) : null, isInArray);
        public void Draw(Rect position, GUIContent? label, bool isInArray = false)
        {
            if (lastException != null)
            {
                EditorGUI.LabelField(position, label ?? new GUIContent(lastException.Value.label), new GUIContent(lastException.Value.message));
                return;
            }
            
            GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + position.height));
            
            Rect elementPosition = position;
            foreach (var item in drawers.WhereNotNull())
            {
                GUIContent elementLabel;
                if (inspectable is IInspectableList)
                    elementLabel = label ?? GUIContent.none;
                else
                    elementLabel = (drawers.Length == 1 ? label : null) ?? new GUIContent(item.element?.displayName ?? string.Empty);

                try
                {
                    elementPosition.height = item.GetHeight(elementLabel, inspectorFlags, isInArray);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    elementPosition.height = EditorGUIUtility.singleLineHeight;
                }
                
                GUI.BeginClip(new Rect(0, 0, elementPosition.x + elementPosition.width, elementPosition.y + elementPosition.height));
                
                try
                {
                    item.OnGUI(elementPosition, elementLabel, inspectorFlags, isInArray);
                }
                catch (Exception e)
                {
                    EditorGUI.LabelField(elementPosition, elementLabel, new GUIContent($"{e.GetType().Name}: {e.Message}"));
                    Debug.LogException(e);
                }
                
                GUI.EndClip();
                
                elementPosition.y += elementPosition.height + 2;
            }
            
            GUI.EndClip();
        }

        public float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            if (lastException != null)
                return EditorGUIUtility.singleLineHeight;
            
            return (drawers.WhereNotNull().Sum(item =>
            {
                try
                {
                    return item.GetHeight(label, flags, isInArray) + 2;
                }
                catch (Exception e)
                {
                    if (item.element != null)
                        Debug.LogException(new InspectorElementException($"Exception occurred while getting height of {item.element.name}", item.element.name, e));
                    else
                        Debug.LogException(e);
                    
                    return EditorGUIUtility.singleLineHeight + 2;
                }
            }) - 2).Clamp(0);
        }
    }
}