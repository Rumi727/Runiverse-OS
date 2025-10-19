#nullable enable
using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Csharp;
using RuniOS.Inspectors.Drawers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        
        [DisallowNull]
        public IInspectable? targetInspectable
        {
            get => _targetInspectable;
            set
            {
                if (targetInspectable == value)
                    return;

                _targetInspectable = value;
                _targetElement = null;
                
                Rebuild();
            }
        }
        IInspectable? _targetInspectable;
        
        [DisallowNull]
        public IInspectorElement? targetElement
        {
            get => _targetElement;
            set
            {
                if (targetElement == value)
                    return;

                _targetElement = value;
                _targetInspectable = null;
                
                Rebuild();
            }
        }
        IInspectorElement? _targetElement;
        
        public ImmutableArray<IInspectorElement> elements { get; private set; } = ImmutableArray<IInspectorElement>.Empty;
        public ImmutableArray<IMGUIInspectorDrawer?> drawers { get; private set; } = ImmutableArray<IMGUIInspectorDrawer?>.Empty;
        IEnumerable<InspectorDrawer?> IInspector.drawers => drawers.OfType<InspectorDrawer>();

        public InspectorFlags inspectorFlags
        {
            get => _inspectorFlags;
            set
            {
                if (_inspectorFlags == value)
                    return;
                
                _inspectorFlags = value;
                Rebuild();
            }
        }
        InspectorFlags _inspectorFlags = InspectorFlags.All;

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

        public Inspector(IInspectable inspectable) : this() => targetInspectable = inspectable;
        public Inspector(IInspectorElement inspectorElement) : this() => targetElement = inspectorElement;
        
        public void Rebuild()
        {
            lastException = null;
            
            if (targetElement != null)
            {
                IMGUIInspectorDrawer? drawer = null;
                if (targetElement is IInspectorVariableElement variableElement)
                    drawer = IMGUIInspectorDrawer.FindDrawer(variableElement, rootInspector);
                
                elements = ImmutableArray.Create(targetElement);
                drawers = ImmutableArray.Create(drawer);

                return;
            }

            if (targetInspectable == null)
                return;
            
            if (targetInspectable is IInspectableList inspectableList)
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
                    lastException = (targetInspectable.inspectionDisplayName, e.ToString());

                    return;
                }
            }
            else
            {
                try
                {
                    elements = targetInspectable.GetElements(inspectorFlags);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    lastException = (targetInspectable.inspectionDisplayName, e.ToString());

                    return;
                }

                drawers = elements.Select(x => IMGUIInspectorDrawer.FindDrawer(x as IInspectorVariableElement, rootInspector)).ToImmutableArray();
            }
        }

        public void DrawLayout(GUIContent? label = null, bool isInArray = false) => Draw(EditorGUILayout.GetControlRect(false, GetHeight()), label, isInArray);
        
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
                elementPosition.height += item.GetHeight();

                try
                {
                    if (targetInspectable is IInspectableList)
                        item.OnGUI(elementPosition, label, inspectorFlags, isInArray);
                    else
                        item.OnGUI(elementPosition, null, inspectorFlags, isInArray);
                }
                catch (Exception e)
                {
                    if (targetInspectable is IInspectableList)
                        EditorGUI.LabelField(elementPosition, label ?? new GUIContent(targetInspectable.inspectionDisplayName), new GUIContent(e.Message));
                    else
                        EditorGUI.LabelField(elementPosition, item.element?.displayName ?? string.Empty, e.Message);
                }
                
                elementPosition.y += item.GetHeight() + 2;
            }
        }

        public float GetHeight()
        {
            if (lastException != null)
                return EditorGUIUtility.singleLineHeight;
            
            return (drawers.WhereNotNull().Sum(item => item.GetHeight() + 2) - 2).Clamp(0);
        }
    }
}