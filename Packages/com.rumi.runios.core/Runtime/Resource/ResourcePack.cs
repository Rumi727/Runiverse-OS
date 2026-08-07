#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Newtonsoft.Json;
using RuniOS.IO;
using RuniOS.Tasks;
using System.Collections.Immutable;

namespace RuniOS.Resource
{
    /// <summary>
    /// 리소스 팩(Resource Pack)의 정보를 담는 참조 클래스입니다.
    /// <br/>실제 에셋 데이터는 <see cref="IAssetRegistry"/>를 통해 로드 및 관리됩니다.
    /// </summary>
    public sealed partial class ResourcePack : IDisposable
    {
        /// <summary>
        /// 빈 <see cref="ResourcePack"/> 인스턴스를 초기화합니다.
        /// </summary>
        ResourcePack()
        {
            identifier = PackIdentifier.empty;
            
            rootFolder = default;
            assetFolder = default;
            infoFile = default;

            metaData = new PackMetaData(string.Empty);
        }

        /// <summary>
        /// 지정된 식별자와 I/O 폴더 노드를 사용하여 <see cref="ResourcePack"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="identifier">팩의 고유 식별자입니다.</param>
        /// <param name="provider">팩의 루트 폴더에 접근하는 <see cref="IIOProvider"/>입니다.</param>
        /// <param name="requiredSort">필수 리소스팩인지 여부와 정렬 기준입니다.</param>
        ResourcePack(PackIdentifier identifier, IIOProvider provider, RequiredPackSort requiredSort)
        {
            this.identifier = identifier;

            rootFolder = provider.rootNode;
            assetFolder = rootFolder.CreateChild(assetsFolderName);
            infoFile = rootFolder.CreateChild(infoPath);

            this.requiredSort = requiredSort;
        }
        
        /// <summary>
        /// 이 리소스 팩의 고유 식별자를 가져옵니다.
        /// </summary>
        public PackIdentifier identifier { get; }

        /// <summary>
        /// 이 팩의 루트 폴더에 접근하는 <see cref="IONode"/>를 가져옵니다.
        /// </summary>
        public IONode rootFolder { get; }
        
        /// <summary>
        /// 이 팩의 에셋 폴더에 접근하는 <see cref="IONode"/>를 가져옵니다.
        /// </summary>
        public IONode assetFolder { get; }
        
        /// <summary>
        /// 이 팩의 메타데이터 파일(<c>pack.json</c>)에 접근하는 <see cref="IONode"/>를 가져옵니다.
        /// </summary>
        public IONode infoFile { get; }

        /// <summary>
        /// 이 팩의 메타데이터(<c>pack.json</c>에 정의된)를 가져옵니다.
        /// </summary>
        public PackMetaData metaData { get; private set; }

        /// <summary>
        /// 이 리소스 팩이 성공적으로 로드되고 유효한지 여부를 가져옵니다.
        /// </summary>
        public bool isValid { get; private set; }

        public RequiredPackSort requiredSort { get; }

        public ImmutableArray<string> namespaces { get; private set; } = ImmutableArray<string>.Empty;
        
        public bool isDisposed { get; private set; }

        readonly AsyncReloadGate reloadGate = new();

        public UniTask Reload() => reloadGate.Run(ReloadCore);

        async UniTask ReloadCore()
        {
            if (!await infoFile.file.Exists())
            {
                metaData = new PackMetaData();
                namespaces = ImmutableArray<string>.Empty;

                return;
            }

            try
            {
                metaData = JsonConvert.DeserializeObject<PackMetaData>(await infoFile.file.ReadAllText());
                isValid = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                isValid = false;
            }
            
            if (!isValid)
                return;
            else if (await assetFolder.dir.Exists())
            {
                namespaces =
                [
                    ..
                    await assetFolder.dir.GetDirectories()
                    .Select(x => x.path.GetFileName())
                    .Where(x =>
                    {
                        if (!Identifier.IsNamespaceValid(x))
                        {
                            Debug.RuntimeLogWarning(Identifier.GetInvalidNamespaceMessage(x));
                            return false;
                        }

                        return true;
                    })
                    .ToArrayAsync()
                ];
            }
        }
        
        public IEnumerable<IONode> GetNamespaceNodes() => namespaces.Select(x => assetFolder.CreateChild(x));

        /// <summary>
        /// 이 리소스 팩을 정리하고 내부 리소스 관리자 목록에서 제거합니다.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(identifier.ToString());

            rootFolder.provider.Dispose();

            isDisposed = true;
            isValid = false;
            
            DisablePack(identifier);
            _loadedResourcePacks.Remove(identifier);
        }
    }
}