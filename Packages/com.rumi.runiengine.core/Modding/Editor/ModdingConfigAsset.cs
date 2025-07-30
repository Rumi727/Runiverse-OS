#nullable enable
namespace RuniEngine.Editor.Modding
{
    public sealed class ModdingConfigAsset : RuniOSConfigObject<ModdingConfigAsset>
    {
        /// <summary>
        /// 에디터에서 패치 로그를 표시할지 여부를 결정합니다.
        /// </summary>
        public bool logInEditor { get; set; } = false;
    }
}
