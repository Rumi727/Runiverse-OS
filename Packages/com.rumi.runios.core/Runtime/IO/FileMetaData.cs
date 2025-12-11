namespace RuniOS.IO
{
    public record struct FileMetaData(string name, long size, DateTime modifiedTime)
    {
        /// <summary>
        /// 이 파일의 이름입니다.
        /// </summary>
        public string name { get; set; } = name;

        /// <summary>
        /// 이 파일의 바이트 단위 크기입니다. 지원하지 않는 경우, 0 입니다.
        /// </summary>
        public long size { get; set; } = size;

        /// <summary>
        /// 이 파일이 수정된 날짜입니다. 지원하지 않는 경우, <see cref="DateTime.MinValue"/> 입니다.
        /// </summary>
        public DateTime modifiedTime { get; set; } = modifiedTime;
    }
}