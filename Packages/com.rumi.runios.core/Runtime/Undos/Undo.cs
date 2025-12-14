#nullable enable
namespace RuniOS.Undos
{
    /// <summary>
    /// 실행 취소(Undo) 및 재실행(Redo) 기능을 관리하는 클래스입니다.
    /// </summary>
    public sealed class Undo : IUndoRecorder
    {
        public IReadOnlyList<UndoableAction> history { get; }
        readonly List<UndoableAction> _history;

        /// <summary>
        /// 현재 기록 포인터의 인덱스입니다.<br/>
        /// -1은 기록이 없는 상태를 의미하며, 0 이상은 해당 인덱스의 작업이 실행된 상태임을 나타냅니다.
        /// </summary>
        public int currentHistoryIndex { get; private set; } = -1;

        /// <summary>
        /// 저장할 수 있는 최대 작업 기록 수입니다.
        /// </summary>
        public int maxUndoableActions { get; }

        /// <summary>
        /// 현재 실행 취소(Undo)가 가능한지 여부를 확인합니다.
        /// </summary>
        public bool canUndo => currentHistoryIndex >= 0;

        /// <summary>
        /// 현재 재실행(Redo)이 가능한지 여부를 확인합니다.
        /// </summary>
        public bool canRedo => currentHistoryIndex < history.Count - 1;

        public event Action? undoPerformed;
        public event Action? redoPerformed;

        /// <summary>
        /// 기본 크기(50)로 <see cref="Undo"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public Undo() : this(50) { }

        /// <summary>
        /// 지정된 최대 작업 수를 사용하여 <see cref="Undo"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="maxUndoableActions">저장할 최대 작업 기록 수입니다.</param>
        public Undo(int maxUndoableActions)
        {
            this.maxUndoableActions = maxUndoableActions;

            _history = new List<UndoableAction>(maxUndoableActions);
            history = _history.AsReadOnly();
        }

        /// <inheritdoc/>
        public void Record(Action undoAction, Action redoAction, string name, UndoGroupToken? groupToken = null, object? collapseKey = null)
        {
            groupToken ??= new UndoGroupToken("Auto");

            // 1. 현재 위치 뒤에 있는(Redo 가능한) 미래의 기록들을 모두 제거 (가지치기)
            if (history.Count > 0 && currentHistoryIndex < history.Count - 1)
            {
                // currentHistoryIndex가 -1이면 0부터, 0이면 1부터 삭제
                int removeStartIndex = currentHistoryIndex + 1;
                int countToRemove = history.Count - removeStartIndex;
                _history.RemoveRange(removeStartIndex, countToRemove);
            }

            // Fake Null 상태나 Serializable Nullable 객체가 Equals(,) 비교에서 의도치 않게 동작하는 것을 막기 위해
            // 실제 null 값으로 명시적으로 초기화합니다.
            if (collapseKey.IsNull())
                collapseKey = null;

            // 2. 병합(Collapsing) 로직 확인
            // 현재 인덱스가 유효하고, 현재 그룹 내에서, 같은 collapseKey를 가진 경우
            if (currentHistoryIndex >= 0 && collapseKey != null)
            {
                // 현재 인덱스부터 뒤로 가면서 탐색
                int searchIndex = currentHistoryIndex;

                // 탐색 범위: 리스트 범위 안이고 && 그룹이 같은 동안만
                while (searchIndex >= 0)
                {
                    var action = history[searchIndex];

                    // 다른 그룹을 만나면 병합 중단 (트랜잭션 보호)
                    if (action.groupToken != groupToken)
                        break;

                    // 키가 같다면 병합 (덮어쓰기)
                    if (Equals(action.collapseKey, collapseKey))
                    {
                        var mergedAction = new UndoableAction(
                            action.undoAction, // Undo: 최초 상태 유지
                            redoAction, // Redo: 최신 상태로 교체
                            name,
                            groupToken,
                            collapseKey
                        );

                        // 리스트에 추가하지 않고 현재 위치를 덮어씀
                        _history[searchIndex] = mergedAction;
                        return;
                    }

                    searchIndex--;
                }
            }

            // 3. 새 작업 추가
            _history.Add(new UndoableAction(undoAction, redoAction, name, groupToken, collapseKey));
            currentHistoryIndex++;

            // 4. 최대 개수 제한
            if (history.Count > maxUndoableActions)
            {
                _history.RemoveAt(0);
                currentHistoryIndex--;
            }
        }

        /// <summary>
        /// 현재 작업을 취소하고 이전 상태로 되돌립니다.<br/>
        /// 같은 그룹 ID를 가진 작업들을 한 번에 일괄 취소합니다.
        /// </summary>
        public void PerformUndo()
        {
            if (!canUndo)
                return;

            // 취소해야 할 대상 그룹 ID 확인
            UndoGroupToken targetToken = _history[currentHistoryIndex].groupToken;

            // 그룹 ID가 같은 동안 계속 반복
            while (currentHistoryIndex >= 0 && _history[currentHistoryIndex].groupToken == targetToken)
            {
                _history[currentHistoryIndex].undoAction?.SafeInvoke();
                currentHistoryIndex--;
            }

            undoPerformed?.SafeInvoke();
        }

        /// <summary>
        /// 취소된 작업을 다시 실행합니다.<br/>
        /// 같은 그룹 ID를 가진 작업들을 한 번에 일괄 실행합니다.
        /// </summary>
        public void PerformRedo()
        {
            if (!canRedo)
                return;

            // 다시 실행해야 할 대상 그룹 ID 확인 (다음 작업의 ID)
            UndoGroupToken targetToken = _history[currentHistoryIndex + 1].groupToken;

            // 그룹 ID가 같은 동안 계속 반복
            while (currentHistoryIndex < _history.Count - 1 && _history[currentHistoryIndex + 1].groupToken == targetToken)
            {
                currentHistoryIndex++;
                _history[currentHistoryIndex].redoAction?.SafeInvoke();
            }

            redoPerformed?.SafeInvoke();
        }
    }
}