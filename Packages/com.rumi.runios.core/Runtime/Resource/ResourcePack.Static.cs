#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Linq;

namespace RuniOS.Resource
{
    public sealed partial class ResourcePack
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

        /// <summary>
        /// 시스템의 기본 리소스 팩을 가져옵니다.
        /// </summary>
        public static readonly ResourcePack defaultPack = Create(PackIdentifier.CreateByID("vanilla"), StreamingIOProvider.instance, RequiredPackSort.BeforeVanilla);

        static readonly Dictionary<PackIdentifier, ResourcePack> _loadedResourcePacks = new();
        public static IReadOnlyDictionary<PackIdentifier, ResourcePack> loadedResourcePacks { get; } = _loadedResourcePacks.AsReadOnly();

        /*
         * TODO
         * 임시
         */
        static readonly List<PackIdentifier> _enabledPackIdentifiers = [];
        public static IReadOnlyList<PackIdentifier> enabledPackIdentifiers { get; } = _enabledPackIdentifiers.AsReadOnly();

        /// <summary>
        /// 지정된 <see cref="PhysicalIOProvider"/>를 사용하여 리소스 팩을 생성합니다.
        /// <br/>팩 식별자는 핸들러의 경로를 기반으로 생성됩니다.
        /// </summary>
        /// <param name="provider">팩 루트 폴더에 접근하는 <see cref="PhysicalIOProvider"/>입니다.</param>
        public static ResourcePack Create(PhysicalIOProvider provider) => Create(PackIdentifier.CreateByPath(provider.targetPath), provider);

        /// <summary>
        /// 지정된 식별자와 I/O 핸들러를 사용하여 리소스 팩을 생성합니다.<br/>
        /// 메타데이터는 자동으로 로드되지 않습니다.
        /// </summary>
        /// <param name="identifier">팩의 고유 식별자입니다.</param>
        /// <param name="provider">팩 루트 폴더에 접근하는 <see cref="IIOProvider"/>입니다.</param>
        /// <param name="requiredSort">필수 리소스팩인지 여부와 정렬 기준입니다.</param>
        /// <returns>생성된 <see cref="ResourcePack"/> 인스턴스를 반환합니다.</returns>
        public static ResourcePack Create(Identifier identifier, IIOProvider provider, RequiredPackSort requiredSort = RequiredPackSort.NotRequired) => Create(PackIdentifier.CreateByID(identifier), provider, requiredSort);

        /// <summary>
        /// 지정된 식별자와 I/O 핸들러를 사용하여 리소스 팩을 생성합니다.<br/>
        /// 메타데이터는 자동으로 로드되지 않습니다.
        /// </summary>
        /// <param name="packIdentifier">팩의 고유 식별자입니다.</param>
        /// <param name="provider">팩 루트 폴더에 접근하는 <see cref="IIOProvider"/>입니다.</param>
        /// <param name="requiredSort">필수 리소스팩인지 여부와 정렬 기준입니다.</param>
        /// <returns>생성된 <see cref="ResourcePack"/> 인스턴스를 반환합니다.</returns>
        public static ResourcePack Create(PackIdentifier packIdentifier, IIOProvider provider, RequiredPackSort requiredSort = RequiredPackSort.NotRequired)
        {
            if (_loadedResourcePacks.TryGetValue(packIdentifier, out var loadedPack))
                return loadedPack;

            ResourcePack resourcePack = new ResourcePack(packIdentifier, provider, requiredSort);
            _loadedResourcePacks.Add(packIdentifier, resourcePack);

            return resourcePack;
        }

        public static UniTask ReloadAll()
        {
            EnablePack(defaultPack.identifier, _enabledPackIdentifiers.Count);

            return UniTask.WhenAll(loadedResourcePacks.Values.Select(pack =>
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (pack.requiredSort)
                {
                    // 0번 인덱스가 제일 우선순위가 높습니다.
                    case RequiredPackSort.BeforeVanilla:
                    {
                        int vanillaIndex = _enabledPackIdentifiers.LastIndexOf(pack.identifier);
                        if (vanillaIndex < 0)
                            EnablePack(pack.identifier, 0);
                        else
                            EnablePack(pack.identifier, vanillaIndex + 1);

                        break;
                    }
                    case RequiredPackSort.AfterVanilla:
                        EnablePack(pack.identifier);
                        break;
                }

                return UniTask.Defer(pack.Reload);
            }));
        }

        public static void EnablePack(PackIdentifier identifier)
        {
            int insertIndex = _enabledPackIdentifiers.LastIndexOf(defaultPack.identifier);
            if (insertIndex < 0)
                insertIndex = 0;

            EnablePack(identifier, insertIndex);
        }

        public static void EnablePack(PackIdentifier identifier, int index)
        {
            if (!_enabledPackIdentifiers.Contains(identifier))
                _enabledPackIdentifiers.Insert(index, identifier);
        }

        public static void DisablePack(PackIdentifier identifier) => _enabledPackIdentifiers.Remove(identifier);

        public static IEnumerable<ResourcePack> GetEnabledPacks() => _enabledPackIdentifiers
            .Where(x => loadedResourcePacks.ContainsKey(x))
            .Select(x => loadedResourcePacks[x])
            .Where(x => x.isValid);

        public static ResourcePack[] GetEnabledPacksSnapshot() => GetEnabledPacks().ToArray();
    }
}