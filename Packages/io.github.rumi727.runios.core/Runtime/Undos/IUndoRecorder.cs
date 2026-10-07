namespace RuniOS.Undos
{
    public interface IUndoRecorder
    {
        /// <summary>
        /// 실행 취소(Undo) 및 재실행(Redo) 작업을 히스토리에 기록합니다.
        /// </summary>
        /// <param name="undoAction">실행 취소(Undo) 시 호출될 작업입니다.</param>
        /// <param name="redoAction">재실행(Redo) 시 호출될 작업입니다.</param>
        /// <param name="name">히스토리에 기록할 이름입니다.</param>
        /// <param name="groupToken">
        /// 작업을 묶을 그룹 식별 토큰입니다.<br/>
        /// <see langword="null"/>이면 내부적으로 새로운 1회용 토큰을 생성하여 할당합니다.<br/>
        /// 연속된 작업이나 동시 다발적인 작업을 그룹화하려면 동일한 토큰 객체를 전달해야 합니다.
        /// </param>
        /// <param name="collapseKey">
        /// 병합을 위한 키 객체입니다.<br/>
        /// <see langword="string"/>, <see cref="UnityEngine.Object"/>, 또는 임의의 클래스 인스턴스를 사용할 수 있습니다.<br/>
        /// 이전 기록과 GroupToken이 같고, CollapseKey가 같은 객체라면 덮어씁니다.
        /// </param>
        void Record(Action undoAction, Action redoAction, string name, UndoGroupToken? groupToken = null, object? collapseKey = null);
    }
}