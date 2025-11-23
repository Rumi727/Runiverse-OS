#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;

namespace RuniOS.Resource
{
    /// <summary>
    /// 단순 파일 검색 및 등록 로직을 사용하는 에셋 레지스트리의 기본 구현입니다.
    /// <br/>이 레지스트리는 파일 시스템을 직접 순회하며 에셋 핸들을 생성합니다.
    /// </summary>
    public abstract class SimpleAssetRegistry : AssetRegistry
    {
        /// <summary>
        /// 레지스트리의 리소스 로딩 진행 중인지 여부를 가져옵니다.
        /// </summary>
        public sealed override bool isLoading => _isLoading;
        bool _isLoading;
        
        /// <summary>
        /// 에셋 파일 검색에 사용되는 와일드카드 패턴을 가져옵니다.
        /// </summary>
        public abstract WildcardPatterns assetFilter { get; }
        
        /// <summary>
        /// 지정된 I/O 핸들러와 MD5 해시를 사용하여 새로운 <see cref="AssetHandle"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="ioHandler">에셋 파일에 접근하는 I/O 핸들러입니다.</param>
        /// <param name="md5Hash">에셋 파일의 MD5 해시 값입니다.</param>
        /// <returns>새로 생성된 <see cref="AssetHandle"/> 인스턴스입니다.</returns>
        protected abstract AssetHandle CreateHandle(IOHandler ioHandler, string md5Hash);

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
        public sealed override async UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null)
        {
            if (isLoading)
            {
                await UniTask.WaitWhile(() => isLoading);
                return;
            }

            _isLoading = true;
            BeginTracking();

            try
            {
                progress?.Report(0);

                await OnBeginAssetLoop();

                List<UniTask> uniTasks = new List<UniTask>();
                int count = 0;
                
                // 모든 리소스 팩을 순회하며 에셋 핸들을 비동기적으로 로드 및 등록
                foreach (var resourcePack in resourcePacks)
                {
                    await foreach ((string nameSpace, IOHandler registryHandler) in GetRegistryFolder(resourcePack))
                    {
                        await foreach (var ioHandler in registryHandler.GetAllFileHandlers(assetFilter))
                        {
                            uniTasks.Add(UniTask.Defer(Method));

                            async UniTask Method()
                            {
                                try
                                {
                                    FilePath path = ioHandler.fullPath.TrimStartPath(registryHandler.fullPath).GetPathWithoutExtension();
                                    await OnAssetLoop(new Identifier(nameSpace, path), ioHandler, CreateHandle(ioHandler, BitConverter.ToString(await ioHandler.GetMD5Hash())));
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError($"An exception occurred while loading {ioHandler.fullPath} resources from the resource pack {resourcePack.identifier}. The exception is: {e}");
                                }

                                // UniTask.WhenAll이 대기하는 작업의 진행률 보고
                                progress?.Report((float)++count / uniTasks.Count);
                            }
                        }
                    }
                }

                // 모든 에셋 등록 작업을 병렬로 대기
                await UniTask.WhenAll(uniTasks);
                await OnEndAssetLoop();
            }
            finally
            {
                try
                {
                    progress?.Report(1);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                EndTracking();
                _isLoading = false;
            }
        }

        protected virtual UniTask OnBeginAssetLoop() => UniTask.CompletedTask;
        protected virtual UniTask OnEndAssetLoop() => UniTask.CompletedTask;

        /// <summary>
        /// 각 에셋을 순회하며 핸들을 등록하는 로직을 수행합니다.
        /// <br/>파생 클래스에서 이 메서드를 오버라이드하여 추가적인 등록 로직을 구현할 수 있습니다.
        /// </summary>
        /// <param name="identifier">에셋을 식별하는 고유 ID입니다.</param>
        /// <param name="ioHandler">에셋 파일에 접근하는 I/O 핸들러입니다.</param>
        /// <param name="assetHandle">생성된 <see cref="AssetHandle"/>입니다.</param>
        /// <returns>비동기 작업을 나타내는 <see cref="UniTask"/>입니다.</returns>
        protected virtual UniTask OnAssetLoop(Identifier identifier, IOHandler ioHandler, AssetHandle assetHandle)
        {
            RecordAssetHandle(identifier, assetHandle);
            return UniTask.CompletedTask;
        }
    }
}