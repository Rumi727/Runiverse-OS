using Cysharp.Threading.Tasks;

namespace RuniOS.IO
{
    public interface IPrecalculatedIOChecksum
    {
        UniTask<string> GetPrecalculatedChecksum();
    }
}