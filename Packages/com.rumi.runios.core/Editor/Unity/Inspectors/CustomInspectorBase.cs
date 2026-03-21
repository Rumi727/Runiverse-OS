#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI;
using RuniOS.Linq;
using System.Collections.Immutable;

namespace RuniOS.Editor.Unity.Inspectors
{
    public abstract class CustomInspectorBase<TTarget> : UnityEditor.Editor where TTarget : Object
    {
        protected new TTarget target => (TTarget)base.target;
        protected new ImmutableArray<TTarget?> targets { get; private set; }

        [NonSerialized] bool repaint = false;

        /// <summary>
        /// Please put base.OnEnable() when overriding
        /// </summary>
        protected virtual void OnEnable()
        {
            if (Kernel.isPlaying)
            {
                repaint = true;
                Repainter().Forget();
            }
            
            targets = base.targets.OfType<TTarget?>().ToImmutableArray();
        }

        /// <summary>
        /// Please put base.OnDisable() when overriding
        /// </summary>
        protected virtual void OnDisable() => repaint = false;

        async UniTaskVoid Repainter()
        {
            while (repaint)
            {
                Repaint();
                await UniTask.Delay(100, true);
            }
        }



        readonly Dictionary<string, SerializedProperty> propertyCache = new();
        readonly Dictionary<string, AnimatedReorderableList> animatedReorderableLists = new();

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
            if (targets.Length <= 0)
                return true;

            // 첫 번째 타겟의 값을 기준값으로 추출
            TValue? firstValue = targets.WhereNotNull().Select(readFunc).FirstOrDefault();
            
            // 모든 타겟의 값이 기준값과 동일한지 검사
            return targets.WhereNotNull().All(x => Equals(readFunc(x), firstValue));
        }

        /// <summary>
        /// 타겟들의 공통된 값을 문자열로 반환합니다.
        /// <br/>값이 서로 다를 경우(Mixed Value)에는 "-"를 반환하여 시각적으로 표현합니다.
        /// </summary>
        /// <typeparam name="TValue">값의 타입입니다.</typeparam>
        /// <param name="readFunc">값을 읽어오는 함수(Getter)입니다.</param>
        /// <returns>값이 모두 같으면 해당 값의 문자열, 다르면 "-", 타겟이 없으면 "null"을 반환합니다.</returns>
        public string GetCommonValueString<TValue>(Func<TTarget, TValue> readFunc)
        {
            if (targets.Length <= 0)
                return "null";

            // 값이 혼합되어 있는지 확인
            if (!HasSameValue(readFunc))
                return "-";

            // 값이 모두 같다면 첫 번째 값을 문자열로 변환
            return targets.WhereNotNull().Select(readFunc).FirstOrDefault()?.ToString() ?? "null";
        }

        /// <summary>
        /// 유효한 모든 타겟에 대해 지정된 작업을 수행합니다.
        /// </summary>
        /// <param name="action">각 타겟에 대해 실행할 작업입니다.</param>
        public void ForEach(Action<TTarget> action)
        {
            foreach (var item in targets.WhereNotNull())
                action.Invoke(item);
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
        public void EditValue<TValue>(Func<TTarget, TValue> readFunc, Func<TTarget, TValue> drawFunc, Action<TTarget, TValue> writeFunc)
        {
            if (targets.Length <= 0)
                return;

            // UI를 그리기 위한 대표 타겟 선정
            TTarget? target = targets.FirstOrDefault(x => x != null);
            if (target == null)
                return;

            // 값이 서로 다르면 UI에 Mixed Value(회색 처리 등)를 표시하도록 설정
            EditorGUI.showMixedValue = !HasSameValue(readFunc);
            EditorGUI.BeginChangeCheck();

            // UI 그리기 (값 수정 시도)
            TValue value = drawFunc.Invoke(target);

            // 사용자가 값을 변경했다면 모든 타겟에 적용
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var item in targets.WhereNotNull())
                    writeFunc.Invoke(item, value);
            }

            // Mixed Value 설정 초기화
            EditorGUI.showMixedValue = false;
        }
    }
}
