#nullable enable
using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Editor.Inspectors.Drawers.IMGUI.Collections;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Csharp;
using RuniOS.Inspectors.Drawers;
using RuniOS.Linq;
using RuniOS.Undos;
using System.Collections.Immutable;

namespace RuniOS.Editor.Inspectors
{
    public sealed class Inspector : IInspector
    {
        public IInspectable? inspectable { get; private set; }

        public IInspectorElement? element => elements.Length == 1 ? elements[0] : null;
        public ImmutableArray<IInspectorElement> elements { get; private set; } = ImmutableArray<IInspectorElement>.Empty;

        public IMGUIInspectorDrawer? drawer => drawers.Length == 1 ? drawers[0] : null;
        public ImmutableArray<IMGUIInspectorDrawer?> drawers { get; private set; } = ImmutableArray<IMGUIInspectorDrawer?>.Empty;

        public InspectorFlags inspectorFlags { get; private set; }
        
        public bool debugMode => inspectorFlags.HasFlagFast(InspectorFlags.Debug);

        public ImmutableArray<IInspectorAttribute> inheritedAttributes { get; } = ImmutableArray<IInspectorAttribute>.Empty;

        public IUndoRecorder? undoRecorder { get; }

        (string label, string message)? lastException = null;

        public Inspector() => undoRecorder = UndoHandler.instance;

        public Inspector(IUndoRecorder? undoRecorder) => this.undoRecorder = undoRecorder;

        public Inspector(IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder)
        {
            if (inheritedAttributes is ImmutableArray<IInspectorAttribute> array)
                this.inheritedAttributes = array;
            else
                this.inheritedAttributes = inheritedAttributes.ToImmutableArray();
            
            this.undoRecorder = undoRecorder;
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
                    ListInspectorDrawer drawer = new ListInspectorDrawer(inspectableList, inheritedAttributes, undoRecorder);

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

                drawers = elements.Select(x => IMGUIInspectorDrawer.FindDrawer(x as IInspectorVariableElement, inheritedAttributes, undoRecorder, predicate)).ToImmutableArray();
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
            drawers = ImmutableArray.Create(IMGUIInspectorDrawer.FindDrawer(element as IInspectorVariableElement, inheritedAttributes, undoRecorder, predicate));

            inspectable = null;
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
            drawers = elements.Select(x => IMGUIInspectorDrawer.FindDrawer(x as IInspectorVariableElement, inheritedAttributes, undoRecorder, predicate)).ToImmutableArray();

            inspectable = null;
            inspectorFlags = flags;
        }


        public void DrawLayout(string? label = null, bool isInArray = false) => DrawLayout(Vector2.zero, label != null ? new GUIContent(label) : null, isInArray);
        public void DrawLayout(GUIContent? label, bool isInArray = false) => DrawLayout(Vector2.zero, label, isInArray);
        public void DrawLayout(Vector2 offset, string? label = null, bool isInArray = false) => DrawLayout(offset, label != null ? new GUIContent(label) : null, isInArray);
        public void DrawLayout(Vector2 offset, GUIContent? label, bool isInArray = false)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, GetHeight(label, inspectorFlags, isInArray));
            rect.xMin += offset.x;
            rect.yMin += offset.y;

            Draw(rect, label, isInArray);
        }

        public void Draw(Rect position, string? label = null, bool isInArray = false, Rect? clipping = null) => Draw(position, label != null ? new GUIContent(label) : null, isInArray, clipping);
        public void Draw(Rect position, GUIContent? label, bool isInArray = false, Rect? clipping = null)
        {
            if (lastException != null)
            {
                EditorGUI.LabelField(position, label ?? new GUIContent(lastException.Value.label), new GUIContent(lastException.Value.message));
                return;
            }

            clipping ??= position;

            GUI.BeginClip(new Rect(0, 0, clipping.Value.x + clipping.Value.width, position.y + position.height));

            if (drawers.Length > 1)
                label = null;

            Rect elementPosition = position;
            foreach (var item in drawers.WhereNotNull())
            {
                if (drawers.Length > 1)
                {
                    try
                    {
                        elementPosition.height = item.GetHeight(null, inspectorFlags, isInArray);
                    }
                    catch (ExitGUIException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        elementPosition.height = EditorGUIUtility.singleLineHeight;
                    }
                }

                GUI.BeginClip(new Rect(0, 0, clipping.Value.x + clipping.Value.width, elementPosition.y + elementPosition.height));

                try
                {
                    item.Draw(elementPosition, label, inspectorFlags, isInArray, clipping);
                }
                catch (ExitGUIException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    GUIContent elementLabel;
                    if (inspectable is IInspectableList)
                        elementLabel = label ?? GUIContent.none;
                    else
                        elementLabel = (drawers.Length > 1 ? null : label) ?? new GUIContent(item.element?.displayName ?? string.Empty);
                    
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

            if (drawers.Length > 1)
                label = null;

            return (drawers.WhereNotNull().Sum(item =>
            {
                try
                {
                    return item.GetHeight(label, flags, isInArray) + 2;
                }
                catch (ExitGUIException)
                {
                    throw;
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