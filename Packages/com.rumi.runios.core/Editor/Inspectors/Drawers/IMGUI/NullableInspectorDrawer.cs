#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Reflection;
using UnityEditor.AnimatedValues;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    // 이쪽은 원래의 UI가 따로 있어서 allowInDebug = true 안했습니다.
    [CustomInspectorDrawer(typeof(Nullable<>), true)]
    [CustomInspectorDrawer(typeof(ISerializableNullable<>), true)]
    public class NullableInspectorDrawer : IMGUIInspectorDrawer
    {
        public NullableInspectorDrawer(IInspectorVariableElement element) : base(element)
        {
            // 가독성 꼬라지ㅋㅋ

            valueElement = element.inspectableObjectElement.FindVariableElement(nameof(Nullable<int>.Value));
            valueElement = new CustomAccessVariableElement.Builder(valueElement)
                .AddWriteAction((_, value) => element.value = Activator.CreateInstance(element.variableType, value))
                .AddSetValuesAction((_, values) => element.SetValues(values.Select(x => Activator.CreateInstance(element.variableType, x))))
                .SetIsReadableFunc((_, flags, _) => element.IsReadable(flags, true))
                .SetIsWritableFunc((_, flags, _) => element.IsWritable(flags, true))
                .Build();

            hasValueElement = element.inspectableObjectElement.FindVariableElement(nameof(Nullable<int>.HasValue));
            hasValueElement = new CustomAccessVariableElement.Builder(hasValueElement)
                // 닷넷의 Nullable<T>를 null로 만들면 구조체이지만 Nullable<T>의 Equals(null)가 true가 되면서 Nullable<T> 인스턴스를 가져오지 못하는 현상이 있습니다.
                .SetReadFunc(x => !x.inspectable.instancesIsEmpty && (bool)x.value!)
                .AddWriteAction((_, value) =>
                {
                    if (Equals(hasValueElement.value, value))
                        return;

                    if ((bool)value!)
                        element.value = Activator.CreateInstance(element.variableType, valueElement.variableType.GetDefaultValueNotNull());
                    else
                        element.value = null;
                })
                .AddSetValuesAction((_, values) =>
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
                })
                .SetIsReadableFunc((_, flags, _) => element.IsReadable(flags, true))
                .SetIsWritableFunc((_, flags, _) => element.IsWritable(flags, true))
                .Build();

            valueDrawer = FindDrawer(valueElement);
        }

        public override bool isField => valueDrawer.isField;

        public IInspectorVariableElement hasValueElement { get; }
        public IInspectorVariableElement valueElement { get; }

        readonly IMGUIInspectorDrawer valueDrawer;
        readonly AnimFloat nullableAnimFloat = new AnimFloat(1);
        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null)
        {
            CheckVariableElement();

            Type? underlyingType = variableElement.variableType.GetNullableUnderlyingType();
            if (underlyingType == null)
                throw new InvalidOperationException("It is not a nullable type.");

            label ??= GUIContent.none;

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

            valueDrawer.OnGUI(position, label, flags, isInArray, clipping);
        }

        float lastInspectorHeight;
        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            CheckVariableElement();

            float height = valueDrawer.GetHeight(label, flags, isInArray);
            bool valueIsNull = hasValueElement.IsReadable(flags) && !(bool)hasValueElement.value!;
            nullableAnimFloat.target = valueIsNull ? 1 : 0;

            if (!isInArray && nullableAnimFloat.isAnimating)
            {
                RepaintCurrentWindow();
                return height.Lerp(EditorGUIUtility.singleLineHeight, nullableAnimFloat.value);
            }

            return !valueIsNull ? height : EditorGUIUtility.singleLineHeight;
        }
    }
}