#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Reflection;
using RuniOS.Undos;
using UnityEditor.AnimatedValues;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    // 이쪽은 원래의 UI가 따로 있어서 allowInDebug = true 안했습니다.
    [CustomInspectorDrawer(typeof(Nullable<>), true)]
    [CustomInspectorDrawer(typeof(ISerializableNullable<>), true)]
    public class NullableInspectorDrawer : IMGUIInspectorDrawer
    {
        public NullableInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder)
        {
            // 가독성 꼬라지ㅋㅋ

            valueElement = element.inspectableObjectElement.GetVariableElement(nameof(Nullable<int>.Value));
            valueElement.accessor.writeAction = value => element.value = Activator.CreateInstance(element.variableType, value);
            valueElement.accessor.setValuesAction = values => element.SetValues(values.Select(x => Activator.CreateInstance(element.variableType, x)));
            valueElement.accessor.isReadableFunc = (flags, noInstanceCheck) => (noInstanceCheck || !valueElement.inspectable.instancesIsEmpty) && element.IsReadable(flags, true);
            valueElement.accessor.isWritableFunc = (flags, _) => element.IsWritable(flags, true);

            hasValueElement = element.inspectableObjectElement.GetVariableElement(nameof(Nullable<int>.HasValue));
            
            // 닷넷의 Nullable<T>를 null로 만들면 구조체이지만 Nullable<T>의 Equals(null)가 true가 되면서 Nullable<T> 인스턴스를 가져오지 못하는 현상이 있습니다.
            hasValueElement.accessor.readFunc = orgMethod => !hasValueElement.inspectable.instancesIsEmpty && (bool)orgMethod.Invoke()!;
            hasValueElement.accessor.writeAction = value =>
            {
                if (Equals(hasValueElement.value, value))
                    return;

                if ((bool)value!)
                    element.value = Activator.CreateInstance(element.variableType, valueElement.variableType.GetDefaultValueNotNull());
                else
                    element.value = null;
            };
            hasValueElement.accessor.setValuesAction = values =>
            {
                element.SetValues(values.Select(x =>
                {
                    if (Equals(hasValueElement.value, x))
                        return x;

                    if ((bool)x!)
                        return Activator.CreateInstance(element.variableType, valueElement.variableType.GetDefaultValueNotNull());
                    else
                        return null;
                }));
            };
            hasValueElement.accessor.isReadableFunc = (flags, _) => element.IsReadable(flags, true);
            hasValueElement.accessor.isWritableFunc = (flags, _) => element.IsWritable(flags, true);

            valueDrawer = FindDrawer(valueElement, attributes.Where(x => !x.applyToSelf), undoRecorder);
        }

        public override bool isField => valueDrawer.isField;

        public IInspectorVariableElement hasValueElement { get; }
        public IInspectorVariableElement valueElement { get; }

        readonly IMGUIInspectorDrawer valueDrawer;
        readonly AnimFloat nullableAnimFloat = new AnimFloat(1);
        protected override void OnGUI(Rect position, GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            CheckVariableElement();

            Type? underlyingType = variableElement.variableType.GetNullableUnderlyingType();
            if (underlyingType == null)
                throw new InvalidOperationException("It is not a nullable type.");

            label ??= new GUIContent(element.displayName);

            if
            (
                NullToggleField
                (
                    position,
                    out position,
                    label,
                    hasValueElement.IsReadable(flags) ? (bool)hasValueElement.value! : null,
                    hasValueElement.IsWritable(flags) ? (x =>
                    {
                        if (x)
                        {
                            hasValueElement.value = true;
                            object? value = valueElement.GetValueOrDefault(flags);

                            IInspectorVariableElement clonedHasValueElement = hasValueElement.Clone();
                            IInspectorVariableElement clonedValueElement = valueElement.Clone();
                            
                            undoRecorder?.Record
                            (
                                () => clonedHasValueElement.value = false,
                                () =>
                                {
                                    clonedHasValueElement.value = true;
                                    if (clonedValueElement.IsWritable(flags))
                                        clonedValueElement.value = value;
                                },
                                GetVariableUndoName(variableElement),
                                UndoHandler.instance.GetTokenForCurrentUnityGroup(),
                                variableElement.path
                            );
                        }
                        else
                        {
                            object? value = valueElement.GetValueOrDefault(flags);
                            hasValueElement.value = false;
                            
                            IInspectorVariableElement clonedHasValueElement = hasValueElement.Clone();
                            IInspectorVariableElement clonedValueElement = valueElement.Clone();
                            
                            undoRecorder?.Record
                            (
                                () =>
                                {
                                    clonedHasValueElement.value = true;
                                    if (clonedValueElement.IsWritable(flags))
                                        clonedValueElement.value = value;
                                },
                                () => clonedHasValueElement.value = false,
                                GetVariableUndoName(variableElement),
                                UndoHandler.instance.GetTokenForCurrentUnityGroup(),
                                variableElement.path
                            );
                        }
                    }) : null,
                    valueElement.variableType.HasDefaultConstructor(flags.HasFlagFast(InspectorFlags.NonPublic)),
                    NullabilityState.Nullable,
                    nullText ?? $"null ({underlyingType.GetTypeDisplayName()})"
                )
            )
                return;

            valueDrawer.Draw(position, label, flags, context);
        }

        float lastInspectorHeight;
        public override float GetHeight(GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            CheckVariableElement();

            float height = valueDrawer.GetHeight(label, flags, context);
            bool valueIsNull = hasValueElement.IsReadable(flags) && !(bool)hasValueElement.value!;
            nullableAnimFloat.target = valueIsNull ? 1 : 0;

            if (!context.isInArray && nullableAnimFloat.isAnimating)
            {
                RepaintCurrentWindow();
                return height.Lerp(EditorGUIUtility.singleLineHeight, nullableAnimFloat.value);
            }

            return !valueIsNull ? height : EditorGUIUtility.singleLineHeight;
        }
    }
}