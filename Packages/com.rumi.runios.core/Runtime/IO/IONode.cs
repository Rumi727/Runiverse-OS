#nullable enable
namespace RuniOS.IO
{
    /// <summary>
    /// 특정 파일 시스템(<see cref="IIOProvider"/>)의 특정 경로를 가리키고 제어하는 <b>읽기 전용</b> 노드입니다.
    /// 경로 조합 및 데이터 읽기 작업을 위한 진입점 역할을 합니다.
    /// </summary>
    public readonly partial record struct IONode(IIOProvider provider, FilePath path = default)
    {
        /// <summary>
        /// 항상 빈 파일 또는 빈 디렉토리처럼 동작하는 노드입니다.
        /// </summary>
        public static IONode empty => EmptyIOProvider.instance.rootNode;

        /// <summary>
        /// 이 노드가 유효한 프로바이더를 참조하고 있는지 여부를 가져옵니다.
        /// </summary>
        public bool isValid => _provider != null;

        /// <summary>
        /// 이 노드가 속한 파일 시스템 프로바이더입니다.
        /// </summary>
        public IIOProvider provider => _provider ?? throw new InvalidOperationException("Invalid node! (provider is null)");
        readonly IIOProvider? _provider = provider;

        /// <summary>
        /// 현재 프로바이더의 최상위(Root) 경로를 가리키는 노드를 가져옵니다.
        /// </summary>
        public IONode rootNode => provider.rootNode;

        /// <summary>
        /// 이 노드가 가리키는 가상 파일 시스템 상의 경로입니다.
        /// </summary>
        public FilePath path { get; } = path;

        /// <summary>
        /// 이 노드의 파일 또는 디렉토리 이름입니다.
        /// </summary>
        public string name => path.GetFileName();

        /// <summary>
        /// 이 노드를 디렉토리로 취급하여 디렉토리 관련 읽기 작업을 수행할 수 있는 객체를 가져옵니다.
        /// </summary>
        public Directory dir => new Directory(this);

        /// <summary>
        /// 이 노드를 파일로 취급하여 파일 관련 읽기 작업을 수행할 수 있는 객체를 가져옵니다.
        /// </summary>
        public File file => new File(this);

        /// <summary>
        /// 현재 경로의 부모 경로를 가리키는 새 노드를 반환합니다.
        /// </summary>
        public IONode GetParent() => new IONode(provider, path.GetParentPath());

        /// <summary>
        /// 현재 경로 아래에 지정된 자식 경로를 덧붙인 새 노드를 반환합니다.
        /// </summary>
        public IONode CreateChild(FilePath childPath)
        {
            if (childPath.IsEmpty())
                return this;

            return new IONode(provider, path + childPath);
        }

        /// <summary>
        /// 현재 경로의 끝에 지정된 확장자를 추가한 새 노드를 반환합니다.
        /// </summary>
        public IONode AddExtension(FileExtension extension) => new IONode(provider, path.value + extension);

        /// <summary>
        /// 현재 노드가 가리키는 위치를 새 루트로 삼는 노드를 생성합니다.
        /// </summary>
        public IONode Recreate() => provider.Recreate(path).rootNode;

        /// <summary>
        /// 두 노드가 같은 실제 대상 경로를 가리키는지 확인합니다.
        /// </summary>
        public bool IsSameTarget(IONode other)
        {
            if (!isValid || !other.isValid)
                return isValid == other.isValid && path == other.path;

            return provider.IsSameTarget(other.provider) && path == other.path;
        }

        /// <summary>
        /// 검색된 데이터 스냅샷(<see cref="IOEntry"/>)을 바탕으로, 해당 위치를 가리키고 조작할 수 있는 새 노드를 생성합니다.
        /// </summary>
        /// <param name="entry">노드로 변환할 대상 엔트리 정보입니다.</param>
        public IONode Bind(IOEntry entry) => new IONode(provider, entry.path);
    }
}
