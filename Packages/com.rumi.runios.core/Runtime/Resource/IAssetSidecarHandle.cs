namespace RuniOS.Resource
{
    /// <summary>
    /// 에셋과 연결된 사이드카를 제공하는 에셋 핸들을 나타냅니다.
    /// </summary>
    public interface IAssetSidecarHandle : IAssetHandle
    {
        /// <summary>
        /// 에셋과 연결된 사이드카를 가져옵니다.
        /// </summary>
        public AssetSidecar sidecar { get; }
    }
}