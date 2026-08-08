#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using RuniOS.Tasks;

namespace RuniOS.Resource
{
    /// <summary>
    /// 단순 파일 검색 및 등록 로직을 사용하는 에셋 레지스트리의 기본 구현입니다.
    /// <br/>이 레지스트리는 파일 시스템을 직접 순회하며 에셋 핸들을 생성합니다.
    /// </summary>
    public abstract class SimpleAssetRegistry<THandle> : AssetRegistry<THandle> where THandle : class, IAssetHandle
    {
        /// <summary>
        /// 이 레지스트리가 관리하는 파일 구조 내 폴더의 이름을 가져옵니다.
        /// </summary>
        public virtual RuniPath registryName => registryId.path;

        /// <inheritdoc/>
        public override bool isSupportedImportData => true;

        /// <summary>
        /// 레지스트리의 리소스 로딩 진행 중인지 여부를 가져옵니다.
        /// </summary>
        public sealed override bool isLoading => reloadGate.isRunning;

        /// <summary>
        /// 에셋 파일 검색에 사용되는 와일드카드 패턴을 가져옵니다.
        /// </summary>
        public abstract WildcardPatterns assetFilter { get; }

        readonly AsyncReloadGate reloadGate = new();

        /// <summary>
        /// 지정된 <paramref name="resourcePack"/> 내에서 이 레지스트리의 데이터를 포함하는 폴더를 비동기적으로 열거합니다.
        /// <br/>각 폴더는 네임스페이스와 해당 레지스트리 노드를 반환합니다.
        /// </summary>
        /// <param name="resourcePack">검색할 리소스 팩입니다.</param>
        /// <returns>비동기적으로 네임스페이스 이름과 레지스트리 노드를 반환하는 열거자입니다.</returns>
        public IUniTaskAsyncEnumerable<(string nameSpace, IONode registryNode)> GetRegistryNodes(ResourcePack resourcePack) => UniTaskAsyncEnumerable.Create<(string nameSpace, IONode registryEntry)>(async (write, iterationToken) =>
        {
            foreach (var namespaceNode in resourcePack.GetNamespaceNodes())
            {
                IONode registryNode = namespaceNode.CreateChild(registryName);
                if (!await registryNode.dir.Exists(iterationToken))
                    continue;

                await write.YieldAsync((namespaceNode.name, registryNode));
            }
        });

        /// <summary>
        /// 지정된 I/O 핸들러와 MD5 해시를 사용하여 새로운 <see cref="AssetHandle{T}"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="node">에셋 파일에 접근하는 I/O 핸들러입니다.</param>
        /// <param name="fileMetaData">에셋 파일의 메타 데이터 값입니다.</param>
        /// <param name="importData">에셋 파일의 초기 임포트 설정입니다.</param>
        /// <returns>새로 생성된 <see cref="AssetHandle{T}"/> 인스턴스입니다.</returns>
        protected abstract UniTask<THandle> CreateHandle(IONode node, FileMetaData fileMetaData, AssetImportData importData);

        /// <summary>
        /// 레지스트리에 등록된 모든 에셋 핸들 정보를 지정된 <paramref name="resourcePacks"/>를 기반으로 다시 로드합니다.
        /// <br/>이 메서드는 로딩이 완료될 때까지 다른 호출을 대기시킵니다.
        /// </summary>
        /// <param name="resourcePacks">로드에 사용할 리소스 팩 컬렉션입니다.</param>
        /// <param name="progress">작업 진행률을 보고하는 데 사용되는 개체입니다.</param>
        /// <returns>비동기 작업을 나타내는 <see cref="UniTask"/>입니다.</returns>
        /// <exception cref="Exception">
        /// 로딩 중 발생할 수 있는 모든 예외입니다.
        /// </exception>
        public sealed override UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null) => reloadGate.Run(progress => ReloadCore(resourcePacks, progress), progress);

        async UniTask ReloadCore(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress)
        {
            BeginTracking();

            try
            {
                progress.SafeReport(0);

                await OnBeginAssetLoop();

                WildcardPatterns patterns = new WildcardPatterns(assetFilter.Concat(WildcardPatterns.jsonFileFilter));

                // 모든 리소스 팩을 순회하며 로드할 에셋을 비동기적으로 인덱싱
                Dictionary<Identifier, AssetLoadTarget> loadTargetDict = [];
                Dictionary<Identifier, AssetImportData> importDataDict = [];
                foreach (var resourcePack in resourcePacks)
                {
                    await foreach ((string nameSpace, IONode registryNode) in GetRegistryNodes(resourcePack))
                    {
                        await foreach (IOEntry fileEntry in registryNode.dir.GetAllFiles(patterns))
                        {
                            try
                            {
                                RuniPath path = fileEntry.path.GetRelativePath(registryNode.path);
                                Identifier identifier = new Identifier(nameSpace, path.GetPathWithoutExtension());
                                IONode node = registryNode.Bind(fileEntry);

                                if (RuniPathUtility.GetExtension(path.value) is ".json")
                                    importDataDict.TryAdd(identifier, new AssetImportData(node, fileEntry.metaData));
                                else
                                    loadTargetDict.TryAdd(identifier, new AssetLoadTarget(resourcePack, node, fileEntry.metaData));
                            }
                            catch (Exception e)
                            {
                                Debug.RuntimeLogError($"An exception occurred while indexing {fileEntry.path} resources from the resource pack {resourcePack.identifier}. The exception is: {e}", GetType().Name);
                            }
                        }
                    }
                }

                // 인덱싱한 모든 에셋을 에셋 핸들로 순서대로 등록함
                int count = 0;
                foreach (var target in loadTargetDict)
                {
                    try
                    {
                        if (!importDataDict.TryGetValue(target.Key, out AssetImportData importData))
                            importData = new AssetImportData(target.Value.node.SetExtension(".json"));

                        THandle handle = await CreateHandle(target.Value.node, target.Value.fileMetaData, importData);
                        await OnAssetLoop(target.Key, target.Value.node, handle);
                    }
                    catch (Exception e)
                    {
                        Debug.RuntimeLogError($"An exception occurred while loading {target.Value.node.path} resources from the resource pack {target.Value.resourcePack.identifier}. The exception is: {e}", GetType().Name);
                    }

                    // 로드 대상 처리 진행률 보고
                    progress.SafeReport((float)++count / loadTargetDict.Count);
                }

                await OnEndAssetLoop();
            }
            catch (Exception e)
            {
                Debug.RuntimeLogError($"An unexpected exception occurred while reloading resources. The exception is: {e}");
            }
            finally
            {
                progress.SafeReport(1);

                EndTracking();
            }
        }

        protected virtual UniTask OnBeginAssetLoop() => UniTask.CompletedTask;
        protected virtual UniTask OnEndAssetLoop() => UniTask.CompletedTask;

        /// <summary>
        /// 각 에셋을 순회하며 핸들을 등록하는 로직을 수행합니다.
        /// <br/>파생 클래스에서 이 메서드를 오버라이드하여 추가적인 등록 로직을 구현할 수 있습니다.
        /// </summary>
        /// <param name="identifier">에셋을 식별하는 고유 ID입니다.</param>
        /// <param name="node">에셋 파일에 접근하는 I/O 노드입니다.</param>
        /// <param name="assetHandle">생성된 <see cref="AssetHandle{T}"/>입니다.</param>
        /// <returns>비동기 작업을 나타내는 <see cref="UniTask"/>입니다.</returns>
        protected virtual UniTask OnAssetLoop(Identifier identifier, IONode node, THandle assetHandle)
        {
            RecordAssetHandle(identifier, assetHandle);
            return UniTask.CompletedTask;
        }

        readonly record struct AssetLoadTarget(ResourcePack resourcePack, IONode node, FileMetaData fileMetaData);
    }
}