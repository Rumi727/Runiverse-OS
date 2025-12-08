#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Newtonsoft.Json;
using RuniOS.Collections.Generic;
using RuniOS.IO;
using RuniOS.Linq;
using System.Collections.Immutable;

namespace RuniOS.Resource
{
    /// <summary>
    /// 리소스 팩(Resource Pack)의 정보를 담는 참조 클래스입니다.
    /// <br/>실제 에셋 데이터는 <see cref="IAssetRegistry"/>를 통해 로드 및 관리됩니다.
    /// </summary>
    public sealed class ResourcePack : IDisposable
    {
        /// <summary>
        /// 에셋이 저장되는 기본 폴더 이름("assets")을 가져옵니다.
        /// </summary>
        public const string assetsFolderName = "assets";

        /// <summary>
        /// 팩의 메타데이터 파일 이름("pack.json")을 가져옵니다.
        /// </summary>
        public const string infoPath = "pack.json";

        /// <summary>
        /// 식별자가 비어 있고 데이터에 접근할 수 없는 빈 <see cref="ResourcePack"/> 인스턴스를 가져옵니다.
        /// </summary>
        public static readonly ResourcePack emptyPack = new ResourcePack();
        static ResourcePack? defaultPack;

        public static readonly PackIdentifier defaultPackIdentifier = PackIdentifier.CreateByID("vanilla");
        
        
        static readonly Dictionary<PackIdentifier, ResourcePack> _loadedResourcePacks = new();
        public static IReadOnlyDictionary<PackIdentifier, ResourcePack> loadedResourcePacks { get; } = _loadedResourcePacks.AsReadOnly();
        
        /*
         * TODO
         * 임시
         */
        internal static readonly HashSet<PackIdentifier> _enabledPackIdentifiers = new();
        public static ReadOnlySet<PackIdentifier> enabledPackIdentifiers { get; } = _enabledPackIdentifiers.AsReadOnly();

        public static IEnumerable<ResourcePack> enabledPacks => loadedResourcePacks
            .Where(x => enabledPackIdentifiers.Contains(x.Key))
            .Select(x => x.Value);

        /// <summary>
        /// 시스템의 기본 리소스 팩을 비동기적으로 가져옵니다.
        /// <br/>기본 팩이 아직 생성되지 않은 경우 <c>"vanilla"</c> 식별자를 사용하여 생성됩니다.
        /// </summary>
        /// <returns>기본 <see cref="ResourcePack"/> 인스턴스 입니다.</returns>
        public static async UniTask<ResourcePack> GetDefaultPack()
        {
            defaultPack ??= await Create(defaultPackIdentifier, StreamingIOHandler.instance);
            EnablePack(defaultPackIdentifier);

            return defaultPack;
        }

        /// <summary>
        /// 시스템의 기본 리소스팩을 가져오려고 시도합니다.
        /// <br/>기본 팩이 아직 생성되지 않은 경우, <see langword="null"/>을 반환합니다.
        /// </summary>
        /// <returns>기본 <see cref="ResourcePack"/> 인스턴스 또는 유효하지 않은 경우 <see langword="null"/>을 반환합니다.</returns>
        public static ResourcePack? TryGetDefaultPack() => defaultPack;

        /// <summary>
        /// 지정된 <see cref="FileIOHandler"/>를 사용하여 리소스 팩을 생성합니다.
        /// <br/>팩 식별자는 핸들러의 경로를 기반으로 생성됩니다.
        /// </summary>
        /// <param name="handler">팩 루트 폴더에 접근하는 <see cref="FileIOHandler"/>입니다.</param>
        /// <returns>생성된 <see cref="ResourcePack"/> 인스턴스 또는 유효하지 않은 경우 <see langword="null"/>을 반환합니다.</returns>
        public static UniTask<ResourcePack> Create(FileIOHandler handler) => Create(PackIdentifier.CreateByPath(handler.targetPath), handler);
        
        /// <summary>
        /// 지정된 식별자와 I/O 핸들러를 사용하여 리소스 팩을 생성하고 메타데이터를 로드합니다.
        /// <br/>팩의 정보 파일(<c>pack.json</c>)이 유효하지 않으면 생성이 실패합니다.
        /// </summary>
        /// <param name="packIdentifier">팩의 고유 식별자입니다.</param>
        /// <param name="handler">팩 루트 폴더에 접근하는 <see cref="IOHandler"/>입니다.</param>
        /// <returns>생성된 <see cref="ResourcePack"/> 인스턴스를 반환합니다.</returns>
        public static async UniTask<ResourcePack> Create(PackIdentifier packIdentifier, IOHandler handler)
        {
            if (_loadedResourcePacks.TryGetValue(packIdentifier, out var loadedPack))
                return loadedPack;
            
            ResourcePack resourcePack = new ResourcePack(packIdentifier, handler.Recreate());
            await resourcePack.Reload();

            _loadedResourcePacks.Add(packIdentifier, resourcePack);
            return resourcePack;
        }

        public static UniTask ReloadAll() => UniTask.WhenAll(loadedResourcePacks.Select(x => UniTask.Defer(x.Value.Reload)));

        public static void EnablePack(PackIdentifier identifier) => _enabledPackIdentifiers.Add(identifier);
        
        public static void DisablePack(PackIdentifier identifier) => _enabledPackIdentifiers.Remove(identifier);
        
        /// <summary>
        /// 빈 <see cref="ResourcePack"/> 인스턴스를 초기화합니다.
        /// </summary>
        ResourcePack()
        {
            identifier = PackIdentifier.empty;
            
            rootFolder = IOHandler.empty;
            assetFolder = IOHandler.empty;
            infoFile = IOHandler.empty;

            metaData = new PackMetaData(string.Empty);
        }

        /// <summary>
        /// 지정된 식별자와 I/O 폴더 핸들러를 사용하여 <see cref="ResourcePack"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="identifier">팩의 고유 식별자입니다.</param>
        /// <param name="folder">팩의 루트 폴더에 접근하는 <see cref="IOHandler"/>입니다.</param>
        ResourcePack(PackIdentifier identifier, IOHandler folder)
        {
            this.identifier = identifier;
            
            rootFolder = folder.Recreate();
            assetFolder = folder.CreateChild(assetsFolderName);
            infoFile = folder.CreateChild(infoPath);
        }
        
        /// <summary>
        /// 이 리소스 팩의 고유 식별자를 가져옵니다.
        /// </summary>
        public PackIdentifier identifier { get; }

        /// <summary>
        /// 이 팩의 루트 폴더에 접근하는 <see cref="IOHandler"/>를 가져옵니다.
        /// </summary>
        public IOHandler rootFolder { get; }
        
        /// <summary>
        /// 이 팩의 에셋 폴더에 접근하는 <see cref="IOHandler"/>를 가져옵니다.
        /// </summary>
        public IOHandler assetFolder { get; }
        
        /// <summary>
        /// 이 팩의 메타데이터 파일(<c>pack.json</c>)에 접근하는 <see cref="IOHandler"/>를 가져옵니다.
        /// </summary>
        public IOHandler infoFile { get; }

        /// <summary>
        /// 이 팩의 메타데이터(<c>pack.json</c>에 정의된)를 가져옵니다.
        /// </summary>
        public PackMetaData metaData { get; private set; }

        /// <summary>
        /// 이 리소스 팩이 성공적으로 로드되고 유효한지 여부를 가져옵니다.
        /// </summary>
        public bool isValid { get; private set; }

        public ImmutableArray<string> namespaces { get; private set; } = ImmutableArray<string>.Empty;
        
        public bool isDisposed { get; private set; }

        public async UniTask Reload()
        {
            metaData = new PackMetaData();
            namespaces = ImmutableArray<string>.Empty;
            
            isValid = false;
            
            if (!await infoFile.FileExists())
                return;
            
            try
            {
                metaData = JsonConvert.DeserializeObject<PackMetaData>(await infoFile.ReadAllText());
                isValid = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                isValid = false;
            }
            
            if (!isValid)
                return;
            else if (await assetFolder.DirectoryExists())
                namespaces = (await assetFolder.GetDirectories().ToArrayAsync()).ToImmutableArray();
        }
        
        public IEnumerable<IOHandler> GetNamespaceHandlers() => namespaces.Select(x => assetFolder.CreateChild(x));

        /// <summary>
        /// 이 리소스 팩을 정리하고 내부 리소스 관리자 목록에서 제거합니다.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                throw new ObjectDisposedException(identifier.ToString());
            
            isDisposed = true;
            isValid = false;
            
            DisablePack(identifier);
            _loadedResourcePacks.Remove(identifier);
        }
    }
}