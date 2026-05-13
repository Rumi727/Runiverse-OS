#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.IO.Virtual
{
    public abstract class VirtualNode
    {
        protected VirtualNode() => root = this as VirtualDirectoryBase;

        /// <summary>
        /// 이 노드가 루트 노드인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        [MemberNotNullWhen(true, nameof(fullPath))]
        public bool isRoot => this is VirtualDirectoryBase && parent == null;

        /// <summary>
        /// 이 디렉토리가 속한 가상 파일 시스템의 최상위 루트 디렉토리를 가져옵니다.<br/>
        /// 이 속성은 읽기 전용이며, 노드가 디렉토리가 아니거나 어태치 상태가 아니라면 <see langword="null"/>입니다.
        /// </summary>
        public VirtualDirectoryBase? root { get; private set; }

        /// <summary>
        /// 이 노드의 부모 디렉토리를 가져옵니다.<br/>
        /// 이 속성은 읽기 전용이며, 부모 디렉토리가 없을 경우 <see langword="null"/>입니다.
        /// </summary>
        public VirtualDirectoryBase? parent { get; private set; }

        /// <summary>
        /// 이 노드의 이름입니다.<br/>
        /// 이 속성은 읽기 전용이며, 노드가 어태치 상태가 아니라면 <see langword="null"/>입니다.
        /// </summary>
        public string? name { get; private set; }

        /// <summary>
        /// 이 노드의 전체 경로입니다.<br/>
        /// 이 속성은 읽기 전용이며, 노드가 디태치 (루트가 아니면서 어태치) 상태가 아니라면 <see langword="null"/>입니다.
        /// </summary>
        public FilePath? fullPath => isDetached ? null : parent?.fullPath + name;

        /// <summary>
        /// 이 노드의 메타 데이터입니다.<br/>
        /// 이 속성은 읽기 전용입니다.
        /// </summary>
        public IOMetaData metaData { get; private set; } = new IOMetaData
        {
            name = null,
            creationTime = DateTime.UtcNow,
            lastWriteTime = DateTime.UtcNow
        };

        /// <summary>
        /// 이 노드가 디렉토리인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isDirectory => this is VirtualDirectoryBase;

        /// <summary>
        /// 이 노드가 어태치 상태인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        [MemberNotNullWhen(true, nameof(root), nameof(parent), nameof(name), nameof(fullPath))]
        public bool isAttached => parent != null;

        /// <summary>
        /// 이 노드가 루트가 아니면서 어태치 상태가 아닌지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isDetached => !isRoot && !isAttached;

        /// <summary>
        /// 이 노드의 리소스가 해제되어 리소스가 유효하지 않은 상태인지 나타내는 값입니다.
        /// </summary>
        public bool isDeleted { get; private set; } = false;

        internal void OnAttached(string name, VirtualDirectoryBase parent)
        {
            ThrowIfDeletedException();
            ThrowIfAttachedException();

            root = parent.root;
            this.parent = parent;
            this.name = name;

            metaData = metaData with
            {
                name = name
            };

            if (this is VirtualDirectoryBase directory)
                directory.InvalidateCache();
        }

        public void Detach()
        {
            ThrowIfDeletedException();
            ThrowIfNotAttachedException();

            parent.OnDetachChild(this);

            root = this as VirtualDirectoryBase;
            parent = null;
            name = null;

            if (this is VirtualDirectoryBase directory)
                directory.InvalidateCache();
        }

        /// <summary>
        /// 이 노드를 삭제합니다.
        /// </summary>
        public void Delete()
        {
            ThrowIfDeletedException();

            OnDelete();

            if (isAttached)
                Detach();

            isDeleted = true;
        }

        public abstract void OnDelete();

        public VirtualDirectoryBase? AsDirectory() => this as VirtualDirectoryBase;

        public VirtualFileBase? AsFile() => this as VirtualFileBase;

        /// <summary>
        /// 이 노드의 <see cref="isAttached"/> 상태가 <see langword="true"/>일 때 예외를 던집니다.
        /// </summary>
        public void ThrowIfAttachedException()
        {
            if (isAttached)
                throw new InvalidOperationException( /* TODO 예외 메시지 적기 */);
        }

        /// <summary>
        /// 이 노드의 <see cref="isAttached"/> 상태가 <see langword="false"/>일 때 예외를 던집니다.
        /// </summary>
        [MemberNotNull(nameof(root), nameof(parent), nameof(name), nameof(fullPath))]
        public void ThrowIfNotAttachedException()
        {
            if (!isAttached)
                throw new InvalidOperationException( /* TODO 예외 메시지 적기 */);
        }

        /// <summary>
        /// 이 노드의 <see cref="isDeleted"/> 상태가 <see langword="true"/>일 때 예외를 던집니다.
        /// </summary>
        public void ThrowIfDeletedException()
        {
            if (isDeleted)
                throw new ObjectDisposedException(GetType().Name, "/* TODO 예외 메시지 적기 */");
        }

        /// <summary>
        /// 잘못된 노드 이름일 때 예외를 던집니다.
        /// </summary>
        public static void ThrowIfInvalidNodeName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(FilePath.directorySeparatorChars) >= 0)
                throw new InvalidOperationException($"The node name '{name}' contains invalid characters.");
        }

        /// <summary>
        /// 잘못된 파일 이름일 때 예외를 던집니다.
        /// </summary>
        public static void ThrowIfInvalidFileName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(FilePath.directorySeparatorChars) >= 0)
                throw new InvalidOperationException($"The file name '{name}' contains invalid characters.");
        }
    }
}