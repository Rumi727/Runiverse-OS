#nullable enable
namespace RuniOS.Undos
{
    /// <summary>
    /// 실행 로직과 취소 로직을 포함하는 작업 단위 구조체입니다.
    /// </summary>
    /// <param name="undoAction">작업을 취소할 때 호출되는 대리자입니다.</param>
    /// <param name="redoAction">작업을 수행하거나 재실행할 때 호출되는 대리자입니다.</param>
    /// <param name="groupToken">이 작업이 속한 그룹의 토큰입니다. 같은 토큰을 가진 연속된 작업은 한 번에 언도됩니다.</param>
    /// <param name="collapseKey">
    /// 병합 식별자입니다. 같은 그룹 내에서 이 키가 같은 작업이 연속되면 기록을 덮어씁니다.<br/>
    /// <see langword="null"/>이면 병합하지 않습니다.
    /// </param>
    public readonly record struct UndoableAction(Action? undoAction, Action? redoAction, string name, UndoGroupToken groupToken, object? collapseKey = null);
}