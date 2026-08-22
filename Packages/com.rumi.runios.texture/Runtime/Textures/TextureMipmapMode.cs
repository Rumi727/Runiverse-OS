#nullable enable
namespace RuniOS.Textures
{
    /// <summary>
    /// Identifies how many mipmap levels are generated.<br/>
    /// 생성할 밉맵 레벨 수를 식별합니다.
    /// </summary>
    public enum TextureMipmapMode
    {
        full = 0,
        none = 1,
        explicitCount = 2
    }
}