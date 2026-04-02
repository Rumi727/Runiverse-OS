using Cysharp.Threading.Tasks;
using System.IO;

namespace RuniOS.IO
{
    public interface IIOHandler : IIOEntry
    {
        /// <inheritdoc cref="IIOEntry.root"/>
        new IIOHandler root { get; }

        /// <inheritdoc cref="IIOEntry.parent"/>
        new IIOHandler? parent { get; }
        
        /// <inheritdoc cref="IIOEntry.Recreate"/>
        new IIOHandler Recreate();



        /// <inheritdoc cref="IIOEntry.CreateChild(FilePath)"/>
        new IIOHandler CreateChild(FilePath path);

        /// <inheritdoc cref="IIOEntry.AddExtension(FileExtension)"/>
        new IIOHandler AddExtension(FileExtension extension);



        /// <summary>
        /// 이 핸들러가 나타내는 파일에 지정된 바이트 배열을 씁니다. 파일이 이미 존재하면 덮어씁니다.
        /// </summary>
        /// <param name="bytes">파일에 기록할 바이트 배열입니다.</param>
        UniTask WriteAllBytes(byte[] bytes);

        /// <summary>
        /// 이 핸들러가 나타내는 파일에 지정된 문자열을 씁니다. 파일이 이미 존재하면 덮어씁니다.
        /// </summary>
        /// <param name="text">파일에 기록할 문자열입니다.</param>
        UniTask WriteAllText(string text);

        /// <summary>
        /// 이 핸들러가 나타내는 파일에 문자열 목록을 한 줄씩 씁니다. 파일이 이미 존재하면 덮어씁니다.
        /// </summary>
        /// <param name="lines">파일에 기록할 문자열 목록입니다.</param>
        UniTask WriteLines(IEnumerable<string> lines);
        
        /// <summary>
        /// 이 핸들러가 나타내는 파일에 데이터를 쓰기 위한 스트림을 엽니다. 파일이 이미 존재하면 기존 내용을 덮어씁니다.
        /// </summary>
        /// <returns>파일에 쓰기 위해 열린 <see cref="Stream"/>입니다.</returns>
        UniTask<Stream> OpenWrite();

        UniTask DirectoryDelete();
        UniTask FileDelete();
    }
}