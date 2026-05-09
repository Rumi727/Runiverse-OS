#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.IONew
{
    partial record struct IOWriteNode
    {
        /// <summary>
        /// 이 노드가 나타내는 파일의 MD5 해시 값을 계산합니다.
        /// </summary>
        /// <returns>파일의 MD5 해시 문자열입니다.</returns>
        public UniTask<string> GetFileChecksum() => ((IONode)this).GetFileChecksum();
    }
}
