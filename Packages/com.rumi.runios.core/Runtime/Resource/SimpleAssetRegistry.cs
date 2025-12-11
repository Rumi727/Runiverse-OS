#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

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
        public abstract string registryName { get; }

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
        /// 지정된 <paramref name="resourcePack"/> 내에서 이 레지스트리의 데이터를 포함하는 폴더를 비동기적으로 열거합니다.
        /// <br/>각 폴더는 네임스페이스와 해당 레지스트리 핸들러를 반환합니다.
        /// </summary>
        /// <param name="resourcePack">검색할 리소스 팩입니다.</param>
        /// <returns>비동기적으로 네임스페이스 이름과 레지스트리 핸들러를 반환하는 열거자입니다.</returns>
        public IUniTaskAsyncEnumerable<(string nameSpace, IOHandler registryHandler)> GetRegistryFolder(ResourcePack resourcePack) => UniTaskAsyncEnumerable.Create<(string nameSpace, IOHandler registryHandler)>(async (write, _) =>
        {
            foreach (var namespaceHandler in resourcePack.GetNamespaceHandlers())
            {
                IOHandler registryHandler = namespaceHandler.CreateChild(registryName);
                if (!await registryHandler.DirectoryExists())
                    continue;

                await write.YieldAsync((namespaceHandler.name, registryHandler));
            }
        });
        
        /// <summary>
        /// 지정된 I/O 핸들러와 MD5 해시를 사용하여 새로운 <see cref="AssetHandle{T}"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="ioHandler">에셋 파일에 접근하는 I/O 핸들러입니다.</param>
        /// <param name="metaData">에셋 파일의 메타 데이터 값입니다.</param>
        /// <returns>새로 생성된 <see cref="AssetHandle{T}"/> 인스턴스입니다.</returns>
        protected abstract UniTask<THandle> CreateHandle(IOHandler ioHandler, FileMetaData metaData);

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
                        await foreach ((FilePath relativePath, FileMetaData metaData) in registryHandler.GetAllFilesWithMetaData(assetFilter))
                        {
                            IOHandler ioHandler = registryHandler.CreateChild(relativePath);
                            uniTasks.Add(UniTask.Defer(Method));

                            async UniTask Method()
                            {
                                try
                                {
                                    FilePath path = ioHandler.fullPath.TrimStartPath(registryHandler.fullPath).GetPathWithoutExtension();
                                    await OnAssetLoop(new Identifier(nameSpace, path), ioHandler, await CreateHandle(ioHandler, metaData));
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
        /// <param name="assetHandle">생성된 <see cref="AssetHandle{T}"/>입니다.</param>
        /// <returns>비동기 작업을 나타내는 <see cref="UniTask"/>입니다.</returns>
        protected virtual UniTask OnAssetLoop(Identifier identifier, IOHandler ioHandler, THandle assetHandle)
        {
            RecordAssetHandle(identifier, assetHandle);
            return UniTask.CompletedTask;
        }
        
        Regex? prefixRegex;
        Regex? filterRegex;

        [MemberNotNull(nameof(prefixRegex), nameof(filterRegex))]
        void InitializeRegex()
        {
            // A. 경로 검사용: asset/namespace/registryName 패턴
            string prefixPattern = $"^asset/[^/]+/{Regex.Escape(registryName)}(?:$|/(.*)$)";
            prefixRegex = new Regex(prefixPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture);

            // B. 필터 검사용: WildcardPatterns에 있는 모든 패턴을 하나의 정규식으로 통합
            //    예: *.png, *.jpg -> \.(?:png|jpg)$
            string filterPattern;

            if (assetFilter.Contains("*") || assetFilter.Contains("*.*"))
                filterPattern = ".*";
            else
            {
                // 각 와일드카드(*.png)를 정규식(.*\.png)으로 변환 후 OR(|) 연결
                var regexParts = assetFilter.Select(x => Regex.Escape(x).Replace("\\*", ".*").Replace("\\?", "."));
                // ^ 와 $ 를 붙여서 전체 일치 유도
                filterPattern = $"^(?:{string.Join("|", regexParts)})$";
            }

            filterRegex = new Regex(filterPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        }

        public override bool IsMatch(FilePath relativePath)
        {
            if (prefixRegex == null || filterRegex == null)
                InitializeRegex();

            // 1. 경로 앞부분(asset/ns/regName) 검사
            Match match = prefixRegex.Match(relativePath);
            if (!match.Success) 
                return false;

            // 2. 뒷부분(파일명) 추출
            string suffix = match.Groups[1].Value;

            // 3. 폴더 자체인 경우 (뒤가 비어있음) -> True
            if (string.IsNullOrEmpty(suffix))
                return true;

            // 4. 파일인 경우 -> 미리 합쳐둔 필터 정규식으로 한 번에 검사
            return filterRegex.IsMatch(suffix);
        }
    }
}