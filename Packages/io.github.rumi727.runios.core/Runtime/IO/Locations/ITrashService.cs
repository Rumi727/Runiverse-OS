#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Services;
using System.IO;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Moves physical paths to the system trash.<br/>
    /// 실제 경로를 시스템 휴지통으로 보냅니다.
    /// </summary>
    public interface ITrashService : IService
    {
        /// <summary>
        /// Determines whether the specified path can be moved to the system trash.<br/>
        /// 지정한 경로를 시스템 휴지통으로 보낼 수 있는지 확인합니다.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the path can be moved to the trash; <see langword="false"/> if it can only be deleted permanently.<br/>
        /// 휴지통으로 보낼 수 있으면 <see langword="true"/>, 영구 삭제만 가능하면 <see langword="false"/>를 반환합니다.
        /// </returns>
        UniTask<bool> CanMoveToTrash(PhysicalPath path);

        /// <summary>
        /// Moves the specified items to the system trash. Never deletes them permanently instead.<br/>
        /// 지정한 항목을 시스템 휴지통으로 보냅니다. 대신 영구 삭제하지 않습니다.
        /// </summary>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation was aborted before every item was moved to the trash.<br/>
        /// 모든 항목을 휴지통으로 보내기 전에 작업이 중단되었을 때 발생합니다.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown when an item cannot be moved to the trash.<br/>
        /// 항목을 휴지통으로 보낼 수 없을 때 발생합니다.
        /// </exception>
        UniTask MoveToTrash(IEnumerable<PhysicalPath> paths);
    }
}
