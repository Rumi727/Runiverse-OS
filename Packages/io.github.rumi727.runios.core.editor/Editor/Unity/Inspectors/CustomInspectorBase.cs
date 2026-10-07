#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI;
using System.Collections.Immutable;

namespace RuniOS.Editor.Unity.Inspectors
{
    public abstract class CustomInspectorBase<TTarget> : UnityEditor.Editor where TTarget : Object
    {
        protected new TTarget target => (TTarget)base.target;
        protected new ImmutableArray<TTarget?> targets { get; private set; }

        protected virtual bool repaintInEditor { get; } = false;

        [NonSerialized] bool repaint = false;

        /// <summary>
        /// Please put base.OnEnable() when overriding
        /// </summary>
        protected virtual void OnEnable()
        {
            repaint = true;
            Repainter().Forget();

            targets = [..base.targets.OfType<TTarget?>()];
        }

        /// <summary>
        /// Please put base.OnDisable() when overriding
        /// </summary>
        protected virtual void OnDisable() => repaint = false;

        async UniTaskVoid Repainter()
        {
            while (repaint)
            {
                if (Kernel.isPlaying || repaintInEditor)
                    Repaint();

                await UniTask.Delay(100, true);
            }
        }



        readonly Dictionary<string, SerializedProperty> propertyCache = new();
        readonly Dictionary<string, AnimatedReorderableList> animatedReorderableLists = new();

        public SerializedProperty? GetProperty(string propertyName)
        {
            if (!propertyCache.TryGetValue(propertyName, out SerializedProperty? tps))
                propertyCache[propertyName] = tps = serializedObject.FindProperty(propertyName);

            return tps;
        }

        public SerializedProperty? DrawPropertyLayout(string propertyName, params GUILayoutOption[] options) => InternalDrawPropertyLayout(propertyName, GUIContent.none, options);
        public SerializedProperty? DrawPropertyLayout(string propertyName, GUIContent label, params GUILayoutOption[] options) => InternalDrawPropertyLayout(propertyName, label, options);
        SerializedProperty? InternalDrawPropertyLayout(string propertyName, GUIContent? label, params GUILayoutOption[] options)
        {
            SerializedProperty? tps;

            try
            {
                if (!propertyCache.TryGetValue(propertyName, out tps))
                    propertyCache[propertyName] = tps = serializedObject.FindProperty(propertyName);
            }
            catch (Exception)
            {
                GUILayout.Label(GetTextOrKey("inspector.property_none").Replace("{name}", propertyName));
                return null;
            }

            if (tps != null)
            {
                EditorGUI.BeginChangeCheck();

                if (tps.isArray && tps.propertyType != SerializedPropertyType.String)
                {
                    AnimatedReorderableList animatedReorderableList;
                    {
                        string key = tps.GetGlobalIdentifier();
                        if (!animatedReorderableLists.TryGetValue(key, out animatedReorderableList))
                        {
                            animatedReorderableList = new AnimatedReorderableList(tps);
                            animatedReorderableLists[key] = animatedReorderableList;
                        }
                    }

                    if (label != null)
                        animatedReorderableList.DrawLayout(label);
                    else
                        animatedReorderableList.DrawLayout();
                }
                else
                {
                    if (label != null)
                        EditorGUILayout.PropertyField(tps, label, options);
                    else
                        EditorGUILayout.PropertyField(tps, options);
                }

                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }

            return tps;
        }

        /// <summary>
        /// 모든 타겟 객체가 특정 프로퍼티에 대해 동일한 값을 가지고 있는지 확인합니다.
        /// <br/>다중 선택 시 "Mixed Value(값이 섞임)" 상태인지 판단할 때 사용합니다.
        /// </summary>
        /// <typeparam name="TValue">비교할 값의 타입입니다.</typeparam>
        /// <param name="readFunc">각 타겟에서 비교할 값을 읽어오는 함수(Getter)입니다.</param>
        /// <returns>
        /// 모든 타겟의 값이 동일하거나 타겟이 없으면 <see langword="true"/>, 
        /// 하나라도 값이 다르면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool HasSameValue<TValue>(Func<TTarget, TValue> readFunc)
        {
            bool hasFirstValue = false;
            TValue? firstValue = default;

            foreach (TTarget? item in targets)
            {
                if (item == null)
                    continue;

                TValue value = readFunc.Invoke(item);
                if (!hasFirstValue)
                {
                    firstValue = value;
                    hasFirstValue = true;
                }
                else if (!EqualityComparer<TValue>.Default.Equals(value, firstValue!))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 타겟들의 공통된 값을 문자열로 반환합니다.
        /// <br/>값이 서로 다를 경우(Mixed Value)에는 "—"를 반환하여 시각적으로 표현합니다.
        /// </summary>
        /// <typeparam name="TValue">값의 타입입니다.</typeparam>
        /// <param name="readFunc">값을 읽어오는 함수(Getter)입니다.</param>
        /// <returns>값이 모두 같으면 해당 값의 문자열, 다르면 "—", 타겟이 없으면 "null"을 반환합니다.</returns>
        public string GetCommonValueString<TValue>(Func<TTarget, TValue> readFunc)
        {
            if (targets.Length <= 0)
                return "null";

            // 값이 혼합되어 있는지 확인
            if (!HasSameValue(readFunc))
                return "—";

            foreach (TTarget? item in targets)
            {
                if (item != null)
                    return readFunc.Invoke(item)?.ToString() ?? "null";
            }

            return "null";
        }

        /// <summary>
        /// 유효한 모든 타겟에 대해 지정된 작업을 수행합니다.
        /// </summary>
        /// <param name="action">각 타겟에 대해 실행할 작업입니다.</param>
        public void ForEach(Action<TTarget> action)
        {
            foreach (TTarget? item in targets)
            {
                if (item != null)
                    action.Invoke(item);
            }
        }

        /// <summary>
        /// 다중 선택된 타겟들에 대해 즉시 모드(Immediate Mode) GUI를 그리고 값을 수정합니다.
        /// <br/>Unity의 <see cref="SerializedProperty"/>를 통하지 않고 C# 프로퍼티를 직접 제어할 때 사용합니다.
        /// </summary>
        /// <typeparam name="TValue">편집할 값의 타입입니다.</typeparam>
        /// <param name="readFunc">현재 값을 읽어오는 함수(Getter)입니다. Mixed Value 판별에 사용됩니다.</param>
        /// <param name="drawFunc">현재 값을 받아 UI를 그리고, 사용자 입력을 통해 변경된 값을 반환하는 함수입니다.</param>
        /// <param name="writeFunc">값이 변경되었을 때 타겟에 값을 적용하는 함수(Setter)입니다.</param>
        /// <example>
        /// 사용 예시:
        /// <code>
        /// EditorGUI.BeginProperty(rect, label, property);
        /// EditValue(
        ///     readFunc: x => x.toggle, 
        ///     drawFunc: val => EditorGUI.Toggle(rect, label, val), 
        ///     writeFunc: (target, newVal) => target.toggle = newVal
        /// );
        /// EditorGUI.EndProperty();
        /// </code>
        /// </example>
        public bool EditValue<TValue>(Func<TTarget, TValue> readFunc, Func<TTarget, TValue> drawFunc, Action<TTarget, TValue> writeFunc)
        {
            if (targets.Length <= 0)
                return false;

            // UI를 그리기 위한 대표 타겟 선정
            if (target == null)
                return false;

            // 값이 서로 다르면 UI에 Mixed Value(회색 처리 등)를 표시하도록 설정
            EditorGUI.showMixedValue = !HasSameValue(readFunc);
            EditorGUI.BeginChangeCheck();

            // UI 그리기 (값 수정 시도)
            TValue value = drawFunc.Invoke(target);

            // 사용자가 값을 변경했다면 모든 타겟에 적용
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                foreach (TTarget? item in targets.OfType<TTarget>())
                    writeFunc.Invoke(item, value);
            }

            // Mixed Value 설정 초기화
            EditorGUI.showMixedValue = false;
            return changed;
        }

        /// <summary>
        /// Draws a C# property value while retaining Unity's serialized-property decoration.<br/>
        /// Unity의 직렬화 프로퍼티 장식을 유지하면서 C# 프로퍼티 값을 그립니다.
        /// </summary>
        /// <remarks>
        /// The serialized property is used only by <see cref="EditorGUI.BeginProperty(Rect, GUIContent, SerializedProperty)"/> and
        /// <see cref="EditorGUI.EndProperty"/>. The actual value is read and written through <paramref name="readFunc"/> and
        /// <paramref name="writeFunc"/>.<br/>
        /// 직렬화 프로퍼티는 <see cref="EditorGUI.BeginProperty(Rect, GUIContent, SerializedProperty)"/>와
        /// <see cref="EditorGUI.EndProperty"/>에만 사용됩니다. 실제 값은 <paramref name="readFunc"/>와
        /// <paramref name="writeFunc"/>를 통해 읽고 씁니다.
        /// </remarks>
        /// <typeparam name="TValue">The type of the edited value.<br/>편집할 값의 타입입니다.</typeparam>
        /// <param name="propertyName">The backing serialized field name used for Unity decoration.<br/>Unity 장식에 사용할 직렬화된 백킹 필드 이름입니다.</param>
        /// <param name="readFunc">The getter used to read each target's current value.<br/>각 타겟의 현재 값을 읽는 getter입니다.</param>
        /// <param name="drawFunc">
        /// The function that draws the control in the allocated position and returns its edited value.<br/>
        /// 할당된 위치에 컨트롤을 그리고 편집된 값을 반환하는 함수입니다.
        /// </param>
        /// <param name="writeFunc">The setter used to write a changed value to each target.<br/>변경된 값을 각 타겟에 쓰는 setter입니다.</param>
        /// <param name="getHeightFunc">
        /// The optional function that returns the control height. The single-line height is used when omitted.<br/>
        /// 컨트롤 높이를 반환하는 선택적 함수입니다. 생략하면 단일 행 높이를 사용합니다.
        /// </param>
        /// <returns><see langword="true"/> when the control changed; otherwise, <see langword="false"/>.<br/>컨트롤이 변경되었으면 <see langword="true"/>, 아니면 <see langword="false"/>입니다.</returns>
        public bool EditPropertyValue<TValue>
        (
            string propertyName,
            Func<TTarget, TValue> readFunc,
            Func<Rect, TTarget, TValue> drawFunc,
            Action<TTarget, TValue> writeFunc,
            Func<TTarget, float>? getHeightFunc = null
        )
        {
            float height = getHeightFunc?.Invoke(target) ?? EditorGUIUtility.singleLineHeight;
            Rect position = EditorGUILayout.GetControlRect(true, height);
            SerializedProperty? property = GetProperty(propertyName);
            if (property == null)
            {
                EditorGUI.LabelField(position, GetTextOrKey("inspector.property_none").Replace("{name}", propertyName));
                return false;
            }

            EditorGUI.BeginProperty(position, null, property);
            try
            {
                return EditValue(readFunc, x => drawFunc.Invoke(position, x), (target, newValue) =>
                {
                    string undoText;
                    if (targets.Length > 1)
                        undoText = $"Modified {property.displayName} in {targets.Length} Objects";
                    else
                        undoText = $"Modified {property.displayName} in {target.name}";

                    Undo.RecordObject(target, undoText);
                    writeFunc.Invoke(target, newValue);

                    if (PrefabUtility.IsPartOfPrefabInstance(target))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(target);

                    EditorUtility.SetDirty(target);
                });
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }
    }
}
