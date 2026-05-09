#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;

namespace RuniOS.IONew
{
    /// <summary>
    /// 파일의 체크섬 값을 사전에 계산하여 제공할 수 있는 프로바이더 기능 인터페이스입니다.
    /// </summary>
    public interface IPrecalculatedIOChecksum
    {
        /// <summary>
        /// 지정된 경로의 사전 계산된 체크섬 값을 가져옵니다.
        /// </summary>
        /// <param name="path">체크섬을 가져올 파일 경로입니다.</param>
        UniTask<string> GetPrecalculatedChecksum(FilePath path);
    }
}
