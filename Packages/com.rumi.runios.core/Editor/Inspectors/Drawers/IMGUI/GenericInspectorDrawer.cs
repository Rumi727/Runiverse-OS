#nullable enable
using RuniOS.Inspectors;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public abstract class GenericInspectorDrawer : IMGUIInspectorDrawer
    {
        protected GenericInspectorDrawer(IInspectorVariableElement element, IUndoRecorder? undoRecorder = null) : base(element, undoRecorder) { }

        public override bool isField => true;

        public sealed override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List,
            bool isInArray = false, Rect? clipping = null)
        {
            CheckVariableElement();
             
            using (new EditorGUI.MixedValueScope(!variableElement.IsReadable(flags) || variableElement.isMixedValue))
            {
                EditorGUI.BeginDisabledGroup(!variableElement.IsWritable(flags));
                EditorGUI.BeginChangeCheck();
                
                // 1. 현재 값 가져오기
                object? value = variableElement.GetValueOrDefault(flags);
                
                // 2. [변경 전] 상태 캡처 (스냅샷 생성)
                object? undoSnapshot = CreateSnapshot(value);
                
                // 3. 필드 그리기 및 값 변경
                object? changedValue = DrawField(position, label ?? GUIContent.none, value, isInArray);
                if (EditorGUI.EndChangeCheck())
                {
                    // 4. [변경 후] 상태 캡처
                    object? redoSnapshot = CreateSnapshot(changedValue);
                    
                    variableElement.value = changedValue;
                    RecordUndo(undoSnapshot, redoSnapshot, flags);
                }
                
                EditorGUI.EndDisabledGroup();
            }
        }

        protected abstract object? DrawField(Rect position, GUIContent label, object? value, bool isInArray);

        /// <summary>
        /// 현재 값에서 언도/리도에 사용할 상태(스냅샷)를 추출합니다. <br/>
        /// 기본 구현은 값 자체를 반환합니다.
        /// </summary>
        protected virtual object? CreateSnapshot(object? value) => value;
        
        /// <summary>
        /// 캡처된 스냅샷을 실제 변수에 적용합니다. <br/>
        /// 기본 구현은 값을 통째로 교체합니다.
        /// </summary>
        protected virtual void ApplySnapshot(IInspectorVariableElement variableElement, object? value, InspectorFlags flags) => variableElement.value = value;

        protected virtual void RecordUndo(object? undoValue, object? redoValue, InspectorFlags flags)
        {
            if (undoRecorder == null)
                return;
            
            CheckVariableElement();

            IInspectorVariableElement variableElement = this.variableElement.Clone();
            undoRecorder.Record(() => ApplySnapshot(variableElement, undoValue, flags), () => ApplySnapshot(variableElement, redoValue, flags), GetVariableUndoName(variableElement), UndoHandler.instance.GetTokenForCurrentUnityGroup(), variableElement.path);
        }
    }
}