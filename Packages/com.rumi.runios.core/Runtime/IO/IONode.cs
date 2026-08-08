#nullable enable
namespace RuniOS.IO
{
    /// <summary>
    /// Represents a read-only handle to a provider-relative path in an <see cref="IIOProvider"/>.<br/>
    /// Path navigation and file reads are delegated to the provider referenced by this node.
    /// <br/><br/>
    /// <see cref="IIOProvider"/> 안의 프로바이더 기준 경로를 가리키는 읽기 전용 핸들을 나타냅니다.<br/>
    /// 경로 탐색과 파일 읽기 작업은 이 노드가 참조하는 프로바이더에 위임됩니다.
    /// </summary>
    public readonly partial record struct IONode(IIOProvider provider, RuniPath path = default)
    {
        /// <summary>
        /// Gets a node backed by the empty provider.<br/>
        /// 빈 프로바이더가 지원하는 노드를 가져옵니다.
        /// </summary>
        public static IONode empty => EmptyIOProvider.instance.rootNode;

        /// <summary>
        /// Gets a value indicating whether this node references a provider.<br/>
        /// 이 노드가 프로바이더를 참조하는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool isValid => _provider != null;

        /// <summary>
        /// Gets the provider referenced by this node.<br/>
        /// 이 노드가 참조하는 프로바이더를 가져옵니다.
        /// </summary>
        public IIOProvider provider => _provider ?? EmptyIOProvider.instance;
        readonly IIOProvider? _provider = provider;

        /// <summary>
        /// Gets the root node of the current provider.<br/>
        /// 현재 프로바이더의 루트 노드를 가져옵니다.
        /// </summary>
        public IONode rootNode => provider.rootNode;

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
        /// Gets directory-oriented read operations for this node.<br/>
        /// 이 노드에 대한 디렉터리 중심 읽기 작업을 가져옵니다.
        /// </summary>
        public Directory dir => new Directory(this);

        /// <summary>
        /// Gets file-oriented read operations for this node.<br/>
        /// 이 노드에 대한 파일 중심 읽기 작업을 가져옵니다.
        /// </summary>
        public File file => new File(this);

        /// <summary>
        /// Gets a new node that points to this node's parent path.<br/>
        /// 이 노드의 상위 경로를 가리키는 새 노드를 가져옵니다.
        /// </summary>
        public IONode GetParent() => new IONode(provider, path.GetParentPath());

        /// <summary>
        /// Creates a new node by appending the specified child path to this node's path.<br/>
        /// 이 노드의 경로에 지정된 자식 경로를 덧붙인 새 노드를 생성합니다.
        /// </summary>
        public IONode CreateChild(RuniPath childPath)
        {
            if (childPath.IsEmpty())
                return this;

            return new IONode(provider, path / childPath);
        }

        /// <summary>
        /// Creates a new node by appending the specified child path string to this node's path.<br/>
        /// 이 노드의 경로에 지정된 자식 경로 문자열을 덧붙인 새 노드를 생성합니다.
        /// </summary>
        public IONode CreateChild(string childName)
        {
            if (string.IsNullOrEmpty(childName))
                return this;

            return new IONode(provider, path / childName);
        }

        /// <summary>
        /// Creates a new node by appending the specified extension to this node's path.<br/>
        /// 이 노드의 경로에 지정된 확장자를 덧붙인 새 노드를 생성합니다.
        /// </summary>
        public IONode AddExtension(FileExtension extension) => new IONode(provider, path.AddExtension(extension));

        /// <summary>
        /// Creates a new node by appending the specified extension to this node's path.<br/>
        /// 이 노드의 경로에 지정된 확장자를 덧붙인 새 노드를 생성합니다.
        /// </summary>
        public IONode AddExtension(string extension) => new IONode(provider, path.AddExtension(extension));

        public IONode SetExtension(FileExtension extension) => new IONode(provider, path.SetExtension(extension));
        public IONode SetExtension(string extension) => new IONode(provider, path.SetExtension(extension));

        public IONode RemoveExtension() => new IONode(provider, path.GetPathWithoutExtension());

        /*/// <summary>
        /// Creates a root node from a provider recreated at this node's path.<br/>
        /// 이 노드의 경로를 새 루트로 재생성한 프로바이더의 루트 노드를 생성합니다.
        /// </summary>
        public IONode Recreate() => provider.Recreate(path).rootNode;*/

        /// <summary>
        /// Determines whether this node and another node refer to the same provider target and path.<br/>
        /// 이 노드와 다른 노드가 같은 프로바이더 대상과 경로를 참조하는지 확인합니다.
        /// </summary>
        public bool IsSameTarget(IONode other)
        {
            if (!isValid || !other.isValid)
                return isValid == other.isValid && path == other.path;

            return provider.IsSameTarget(other.provider) && path == other.path;
        }

        /// <summary>
        /// Creates a new node bound to the path stored in the specified entry.<br/>
        /// 지정된 엔트리에 저장된 경로에 바인딩된 새 노드를 생성합니다.
        /// </summary>
        /// <param name="entry">
        /// The entry whose path should be bound to the new node.<br/>
        /// 새 노드에 바인딩할 경로를 가진 엔트리입니다.
        /// </param>
        public IONode Bind(IOEntry entry) => new IONode(provider, entry.path);
    }
}
