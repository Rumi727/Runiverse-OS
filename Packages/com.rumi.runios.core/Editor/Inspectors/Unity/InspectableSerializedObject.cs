#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Editor.Inspectors.Unity
{
    public class InspectableSerializedObject : IInspectableObject
    {
        public IInspectorVariableElement? parentElement { get; set; }
        
        public SerializedObject serializedObject { get; }
        public SerializedProperty targetProperty { get; }
        
        public bool instancesIsEmpty => serializedObject.targetObject == null;
        
        public bool instanceIsMultiple => serializedObject.isEditingMultipleObjects;
        
        public Type inspectionType { get; }
        public string inspectionDisplayName => inspectionType.GetTypeDisplayName();
        
        public int instanceCount => serializedObject.targetObjects.Length;

        public Action<IEnumerable<object?>>? onValueChanged { get; set; }

        public ImmutableArray<IInspectorAttribute> attributes => ImmutableArray<IInspectorAttribute>.Empty;

        public ImmutableArray<IInspectorElement> elements
        {
            get
            {
                if (_elements.IsDefault)
                {
                    SerializedProperty property = targetProperty.Copy();
                    int depth = property.depth + 1;
                    if (!property.Next(true))
                        return _elements = ImmutableArray<IInspectorElement>.Empty;
            
                    List<IInspectorElement> elements = [];
                    do
                    {
                        if (depth != property.depth)
                            break;

                        SerializedPropertyElement element = new SerializedPropertyElement(this, property.Copy());
                        if (property.isArray)
                            elements.Add(element);
                    }
                    while (property.Next(false));

                    return _elements = [..elements];
                }

                return _elements;
            }
        }
        ImmutableArray<IInspectorElement> _elements;
        
        public ImmutableDictionary<string, IInspectorVariableElement> variableElements =>
            _variableElements ??= elements
                .OfType<IInspectorVariableElement>()
                .ToImmutableDictionary(x => x.name, x => x);
        ImmutableDictionary<string, IInspectorVariableElement>? _variableElements;

        public InspectableSerializedObject(SerializedObject serializedObject, SerializedProperty? targetProperty = null)
        {
            this.serializedObject = serializedObject;
            this.targetProperty = targetProperty?.Copy() ?? serializedObject.GetIterator();
            
            ScriptAttributeUtilityBridge.GetFieldInfoFromProperty(this.targetProperty, out Type type);
            inspectionType = type;
        }
        
        public bool TryGetInspectionType([NotNullWhen(true)] out Type? type)
        {
            type = inspectionType;
            return true;
        }
        
        public void OnValueChangedInvoke()
        {
            onValueChanged?.SafeInvoke(Enumerable.Empty<object?>());
            parentElement?.inspectableObjectElement.OnValueChangedInvoke();
        }

        public IEnumerable<IInspectorElement> GetElements(InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List)
        {
            if (!flags.HasFlagFast(InspectorFlags.Public) || !flags.HasFlagFast(InspectorFlags.Instance))
                return ImmutableArray<IInspectorElement>.Empty;

            return elements.Where(x => x.HasFlags(flags));
        }

        public IInspectorVariableElement GetVariableElement(string name, InspectorFlags flags = InspectorFlags.All)
        {
            if 
            (
                !flags.HasFlagFast(InspectorFlags.Public) || !flags.HasFlagFast(InspectorFlags.Instance) ||
                !variableElements.TryGetValue(name, out IInspectorVariableElement? value)
            )
                throw new InvalidOperationException($"Could not find element named {name}!");

            return value;
        }

        /// <inheritdoc cref="IInspectableObject.Clone"/>
        public InspectableSerializedObject Clone() => new InspectableSerializedObject(new SerializedObject(serializedObject.targetObjects), new SerializedObject(targetProperty.serializedObject.targetObjects).FindProperty(targetProperty.propertyPath)) { parentElement = parentElement?.Clone(), onValueChanged = onValueChanged };
        IInspectableObject IInspectableObject.Clone() => Clone();

        public static implicit operator SerializedObject(InspectableSerializedObject inspectableObject) => inspectableObject.serializedObject;
        public static implicit operator InspectableSerializedObject(SerializedObject serializedObject) => new InspectableSerializedObject(serializedObject);
    }
}