#nullable enable
using RuniOS.Undos;
using Undo = UnityEditor.Undo;
using RuniUndo = RuniOS.Undos.Undo;

namespace RuniOS.Editor
{
    /// <summary>
    /// Unity 에디터의 Undo 시스템과 런타임 Undo 로직을 연결하는 핸들러입니다.<br/>
    /// 스크립트가 리로드되면 인스턴스가 파괴되어 이전 기록은 초기화됩니다.
    /// </summary>
    public sealed class UndoHandler : IUndoRecorder
    {
        /// <summary>
        /// UndoHandler의 싱글톤 인스턴스입니다.
        /// </summary>
        public static UndoHandler instance { get; } = new UndoHandler();

        SerializableUndoHandler? serializableUndoHandler;

        [NonSerialized] readonly RuniUndo runiUndo = new RuniUndo();

        [NonSerialized] int lastUnityGroupId = -1;
        [NonSerialized] UndoGroupToken cachedGroupToken = new UndoGroupToken("UnityGroup_Initial");

        /// <summary>
        /// Unity의 현재 그룹 ID에 대응하는 토큰을 가져오거나 새로 생성합니다.
        /// </summary>
        public UndoGroupToken GetTokenForCurrentUnityGroup()
        {
            int currentUnityId = Undo.GetCurrentGroup();
            if (currentUnityId != lastUnityGroupId)
            {
                cachedGroupToken = new UndoGroupToken($"UnityGroup_{currentUnityId}");
                lastUnityGroupId = currentUnityId;
            }

            return cachedGroupToken;
        }

        /// <summary>
        /// 작업을 Unity Undo 히스토리에 기록하고 동기화합니다.<br/>
        /// Unity의 현재 Undo 그룹 ID와 연결된 토큰을 사용하여 자동으로 그룹을 관리합니다.
        /// </summary>
        /// <param name="undoAction">실행 취소(Undo) 시 호출될 작업입니다.</param>
        /// <param name="redoAction">재실행(Redo) 시 호출될 작업입니다.</param>
        /// <param name="name">Unity Undo 메뉴(Edit -> Undo [Name])에 표시될 이름입니다.</param>
        /// <param name="groupToken">
        /// 작업을 묶을 그룹 식별 토큰입니다.<br/>
        /// <see langword="null"/>이면 Unity의 현재 Undo 그룹 ID와 연결된 토큰을 자동 사용합니다.<br/>
        /// 연속된 작업이나 동시 다발적인 작업을 그룹화하려면 동일한 토큰 객체를 전달해야 합니다.
        /// </param>
        /// <param name="collapseKey">
        /// 병합을 위한 키 객체입니다.<br/>
        /// <see langword="string"/>, <see cref="UnityEngine.Object"/>, 또는 임의의 클래스 인스턴스를 사용할 수 있습니다.<br/>
        /// 이전 기록과 GroupToken이 같고, CollapseKey가 같은 객체라면(참조 비교) 덮어씁니다.
        /// </param>
        public void Record(Action undoAction, Action redoAction, string name, UndoGroupToken? groupToken = null, object? collapseKey = null)
        {
            // 1. RuniUndo에 기록
            runiUndo.Record(undoAction, redoAction, name, groupToken ?? GetTokenForCurrentUnityGroup(), collapseKey);

            if (serializableUndoHandler == null)
            {
                serializableUndoHandler = ScriptableObject.CreateInstance<SerializableUndoHandler>();
                serializableUndoHandler.hideFlags = HideFlags.HideAndDontSave;
                serializableUndoHandler.historyIndex = runiUndo.currentHistoryIndex;
                
                EditorUtility.SetDirty(serializableUndoHandler);
            }

            // 2. Unity에 상태 기록 (현재 시점의 인덱스를 저장)
            Undo.RecordObject(serializableUndoHandler, name);

            // 3. 인덱스 동기화
            // Unity가 나중에 이 값을 복원하면, RuniUndo도 이 인덱스로 돌아가야 함
            serializableUndoHandler.historyIndex = runiUndo.currentHistoryIndex;
            EditorUtility.SetDirty(serializableUndoHandler);
        }
        
        class SerializableUndoHandler : ScriptableObject
        {
            /// <summary>
            /// Unity가 저장하고 복원하는 '목표 인덱스'입니다.
            /// </summary>
            public int historyIndex;
            
            void Awake()
            {
                Undo.undoRedoPerformed += DetectUndoneOrRedoneAction;
                AssemblyReloadEvents.beforeAssemblyReload += () => DestroyImmediate(this);
            }

            void OnDestroy() => Undo.undoRedoPerformed -= DetectUndoneOrRedoneAction;
            
            void DetectUndoneOrRedoneAction()
            {
                // Unity가 복원한 '목표 인덱스'
                int targetIndex = historyIndex;

                // 현재 RuniUndo의 '실제 인덱스'
                int currentIndex = instance.runiUndo.currentHistoryIndex;

                if (targetIndex == currentIndex)
                    return;

                // 두 인덱스가 일치할 때까지 반복 수행 (Loop)
                // Undo 상황: 현재 인덱스가 목표보다 큼 -> 줄여야 함
                if (currentIndex > targetIndex)
                {
                    for (int i = 0; instance.runiUndo.currentHistoryIndex > targetIndex && i < 100; i++)
                    {
                        if (!instance.runiUndo.canUndo)
                            break;

                        instance.runiUndo.PerformUndo();
                    }
                }
                // Redo 상황: 현재 인덱스가 목표보다 작음 -> 늘려야 함
                else
                {
                    for (int i = 0; instance.runiUndo.currentHistoryIndex < targetIndex && i < 100; i++)
                    {
                        if (!instance.runiUndo.canRedo)
                            break;

                        instance.runiUndo.PerformRedo();
                    }
                }
            }
        }
    }
}