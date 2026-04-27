using System.IO;

namespace RuniOS.IONew
{
    public readonly record struct IOMetaData(string name, long? size, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, FileAttributes? attributes)
    {
        /// <summary>
        /// 이 파일의 이름입니다.
        /// </summary>
        public string name => _name ?? string.Empty;
        readonly string? _name = name;
    }
}