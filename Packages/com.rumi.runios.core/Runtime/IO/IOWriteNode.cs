#nullable enable
namespace RuniOS.IO
{
    /// <summary>
    /// Represents a writable handle to a provider-relative path in an <see cref="IWritableIOProvider"/>.<br/>
    /// Path navigation and file write operations are delegated to the provider referenced by this node.
    /// <br/><br/>
    /// <see cref="IWritableIOProvider"/> 안의 프로바이더 기준 경로를 가리키는 쓰기 가능 핸들을 나타냅니다.<br/>
    /// 경로 탐색과 파일 쓰기 작업은 이 노드가 참조하는 프로바이더에 위임됩니다.
    /// </summary>
    public readonly partial record struct IOWriteNode(IWritableIOProvider provider, RuniPath path = default)
    {
        /// <summary>
        /// Gets a writable node backed by the empty provider.<br/>
        /// 빈 프로바이더가 지원하는 쓰기 가능 노드를 가져옵니다.
        /// </summary>
        public static IOWriteNode empty => EmptyIOProvider.instance.rootNode;

        /// <summary>
        /// Gets a value indicating whether this node references a provider.<br/>
        /// 이 노드가 프로바이더를 참조하는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isValid => _provider != null;

        /// <summary>
        /// Gets the writable provider referenced by this node.<br/>
        /// 이 노드가 참조하는 쓰기 가능 프로바이더를 가져옵니다.
        /// </summary>
        public IWritableIOProvider provider => _provider ?? EmptyIOProvider.instance;
        readonly IWritableIOProvider? _provider = provider;

        /// <summary>
        /// Gets the root node of the current provider.<br/>
        /// 현재 프로바이더의 루트 노드를 가져옵니다.
        /// </summary>
        public IOWriteNode rootNode => provider.rootNode;

        /// <summary>
        /// Gets the provider-relative path referenced by this node.<br/>
        /// 이 노드가 참조하는 프로바이더 기준 경로를 가져옵니다.
        /// </summary>
        public RuniPath path { get; } = path;
        
        /// <summary>
        /// Gets the last path segment of this node.<br/>
        /// 이 노드 경로의 마지막 세그먼트를 가져옵니다.
        /// </summary>
        public string name => path.GetFileName();

        /// <summary>
        /// Gets directory-oriented read and write operations for this node.<br/>
        /// 이 노드에 대한 디렉터리 중심 읽기 및 쓰기 작업을 가져옵니다.
        /// </summary>
        public Directory dir => new Directory(this);

        /// <summary>
        /// Gets file-oriented read and write operations for this node.<br/>
        /// 이 노드에 대한 파일 중심 읽기 및 쓰기 작업을 가져옵니다.
        /// </summary>
        public File file => new File(this);

        /// <summary>
        /// Gets a new writable node that points to this node's parent path.<br/>
        /// 이 노드의 상위 경로를 가리키는 새 쓰기 가능 노드를 가져옵니다.
        /// </summary>
        public IOWriteNode GetParent() => new IOWriteNode(provider, path.GetParentPath());

        /// <summary>
        /// Creates a new writable node by appending the specified child path to this node's path.<br/>
        /// 이 노드의 경로에 지정된 자식 경로를 덧붙인 새 쓰기 가능 노드를 생성합니다.
        /// </summary>
        public IOWriteNode CreateChild(RuniPath childPath)
        {
            if (childPath.IsEmpty())
                return this;

            return new IOWriteNode(provider, path / childPath);
        }

        /// <summary>
        /// Creates a new writable node by appending the specified child path string to this node's path.<br/>
        /// 이 노드의 경로에 지정된 자식 경로 문자열을 덧붙인 새 쓰기 가능 노드를 생성합니다.
        /// </summary>
        public IOWriteNode CreateChild(string childName)
        {
            if (string.IsNullOrEmpty(childName))
                return this;

            return new IOWriteNode(provider, path / childName);
        }

        /// <summary>
        /// Creates a new writable node by appending the specified extension to this node's path.<br/>
        /// 이 노드의 경로에 지정된 확장자를 덧붙인 새 쓰기 가능 노드를 생성합니다.
        /// </summary>
        public IOWriteNode AddExtension(FileExtension extension) => new IOWriteNode(provider, path.AddExtension(extension));

        /// <summary>
        /// Creates a new node by appending the specified extension to this node's path.<br/>
        /// 이 노드의 경로에 지정된 확장자를 덧붙인 새 노드를 생성합니다.
        /// </summary>
        public IOWriteNode AddExtension(string extension) => new IOWriteNode(provider, path.AddExtension(extension));

        public IOWriteNode SetExtension(FileExtension extension) => new IOWriteNode(provider, path.SetExtension(extension));
        public IOWriteNode SetExtension(string extension) => new IOWriteNode(provider, path.SetExtension(extension));

        public IOWriteNode RemoveExtension() => new IOWriteNode(provider, path.GetPathWithoutExtension());

        /*/// <summary>
        /// Creates a root node from a writable provider recreated at this node's path.<br/>
        /// 이 노드의 경로를 새 루트로 재생성한 쓰기 가능 프로바이더의 루트 노드를 생성합니다.
        /// </summary>
        public IOWriteNode Recreate() => provider.Recreate(path).rootNode;*/

        /// <summary>
        /// Determines whether this node and another node refer to the same provider target and path.<br/>
        /// 이 노드와 다른 노드가 같은 프로바이더 대상과 경로를 참조하는지 확인합니다.
        /// </summary>
        public bool IsSameTarget(IONode other) => ((IONode)this).IsSameTarget(other);

        /// <summary>
        /// Creates a new writable node bound to the path stored in the specified entry.<br/>
        /// 지정된 엔트리에 저장된 경로에 바인딩된 새 쓰기 가능 노드를 생성합니다.
        /// </summary>
        /// <param name="entry">
        /// The entry whose path should be bound to the new node.<br/>
        /// 새 노드에 바인딩할 경로를 가진 엔트리입니다.
        /// </param>
        public IOWriteNode Bind(IOEntry entry) => new IOWriteNode(provider, entry.path);

        /// <summary>
        /// Converts a writable node to a read-only node that references the same provider and path.<br/>
        /// 같은 프로바이더와 경로를 참조하는 읽기 전용 노드로 쓰기 가능 노드를 변환합니다.
        /// </summary>
        /// <param name="node">
        /// The writable node to convert.<br/>
        /// 변환할 쓰기 가능 노드입니다.
        /// </param>
        public static implicit operator IONode(IOWriteNode node) => new IONode(node.provider, node.path);
    }
}
