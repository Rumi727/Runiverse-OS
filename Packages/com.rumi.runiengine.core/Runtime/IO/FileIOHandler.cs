#nullable enable
using Cysharp.Threading.Tasks;
using RuniEngine.Spans;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RuniEngine.IO
{
    /// <summary>
    /// 파일 시스템 경로를 처리하고 파일 및 디렉토리 작업에 대한 기능을 제공하는 핸들러입니다.
    /// </summary>
    public class FileIOHandler : IOHandler
    {
        /// <summary>
        /// 지정된 대상 경로를 사용하여 <see cref="FileIOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="targetPath">이 핸들러가 나타내는 파일 또는 디렉토리의 실제 경로입니다.</param>
        public FileIOHandler(FilePath targetPath) : base() => this.targetPath = targetPath;

        /// <summary>
        /// 부모 핸들러와 자식 경로를 사용하여 <see cref="FileIOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="parent">이 핸들러의 부모 <see cref="FileIOHandler"/>입니다.</param>
        /// <param name="childPath">이 핸들러의 자식 경로입니다.</param>
        FileIOHandler(FileIOHandler? parent, FilePath childPath) : base(parent, childPath) => targetPath = parent?.targetPath + childPath;

        /// <summary>
        /// 이 핸들러의 최상위 <see cref="FileIOHandler"/>를 가져옵니다.
        /// </summary>
        public new FileIOHandler root => (FileIOHandler)base.root;
        /// <summary>
        /// 이 핸들러의 부모 <see cref="FileIOHandler"/>를 가져옵니다.
        /// </summary>
        public new FileIOHandler? parent => (FileIOHandler?)base.parent;

        /// <summary>
        /// 이 핸들러가 독립적인지 여부를 나타내는 값을 가져옵니다. 이 구현에서는 항상 <see langword="false"/>를 반환합니다.
        /// </summary>
        public override bool isIndependent => false;

        /// <summary>
        /// 이 핸들러가 나타내는 실제 파일 또는 디렉토리 경로를 가져옵니다.
        /// </summary>
        public FilePath targetPath { get; } = string.Empty;

        /// <summary>
        /// 현재 위치를 최상위 경로로 취급하는 새 <see cref="FileIOHandler"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>현재 위치를 기반으로 하는 새 <see cref="FileIOHandler"/> 인스턴스입니다.</returns>
        public override IOHandler Recreate() => new FileIOHandler(targetPath + fullPath);

        /// <summary>
        /// 지정된 경로를 사용하여 이 핸들러의 자식 <see cref="FileIOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="path">자식 핸들러의 경로입니다.</param>
        /// <returns>생성된 <see cref="FileIOHandler"/> 인스턴스입니다.</returns>
        public override IOHandler CreateChild(FilePath path)
        {
            FileIOHandler handler = this;
            if (path.IsEmpty())
                return handler;

            foreach (var item in path.value.AsSpan().SplitAny(FilePath.directorySeparatorChars))
            {
                FilePath childPath = new FilePath(item);
                handler = new FileIOHandler(handler, childPath);
            }

            return handler;
        }

        /// <summary>
        /// 이 핸들러의 경로에 지정된 확장자를 추가하여 새 <see cref="FileIOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="extension">추가할 확장자입니다.</param>
        /// <returns>확장자가 추가된 새 <see cref="FileIOHandler"/> 인스턴스입니다.</returns>
        public override IOHandler AddExtension(FileExtension extension) => new FileIOHandler(parent, name + extension);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리가 존재하는지 비동기적으로 확인합니다.
        /// </summary>
        /// <returns>디렉토리가 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환하는 <see cref="bool"/>입니다.</returns>
        public override UniTask<bool> DirectoryExists() => UniTask.RunOnThreadPool(() => Directory.Exists(targetPath));

        /// <summary>
        /// 이 핸들러가 나타내는 파일이 존재하는지 비동기적으로 확인합니다.
        /// </summary>
        /// <returns>파일이 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환하는 <see cref="bool"/>입니다.</returns>
        public override UniTask<bool> FileExists() => UniTask.RunOnThreadPool(() => File.Exists(targetPath));

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리의 모든 서브디렉토리 이름을 비동기적으로 열거합니다.
        /// </summary>
        /// <returns>디렉토리의 서브디렉토리 이름 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="string"/>입니다.</returns>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<string>> GetDirectories() => UniTask.RunOnThreadPool(() => Directory.EnumerateDirectories(targetPath).Select(x =>
        {
            if (x.ToPath().TryTrimStartPath(targetPath, out FilePath result))
                return result.ToString();

            return null;
        }).WhereNotNull());

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리의 모든 서브디렉토리 경로(재귀적으로)를 비동기적으로 열거합니다.
        /// </summary>
        /// <returns>디렉토리의 모든 서브디렉토리 경로 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="FilePath"/>입니다.</returns>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<FilePath>> GetAllDirectories() => UniTask.RunOnThreadPool(() => Directory.EnumerateDirectories(targetPath, "*", SearchOption.AllDirectories).Select(x => x - targetPath));

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리의 모든 파일 이름을 비동기적으로 열거합니다.
        /// </summary>
        /// <returns>디렉토리의 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="string"/>입니다.</returns>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<string>> GetFiles() => UniTask.RunOnThreadPool(() => Directory.EnumerateFiles(targetPath).Select(x =>
        {
            if (x.ToPath().TryTrimStartPath(targetPath, out FilePath result))
                return result.ToString();

            return null;
        }).WhereNotNull());

        /// <summary>
        /// 지정된 와일드카드 패턴과 일치하는, 이 핸들러가 나타내는 디렉토리의 모든 파일 이름을 비동기적으로 열거합니다.
        /// </summary>
        /// <param name="wildcardPatterns">파일 이름과 일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>지정된 패턴과 일치하는 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="string"/>입니다.</returns>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<string>> GetFiles(WildcardPatterns wildcardPatterns) => UniTask.RunOnThreadPool(() => DirectoryUtility.EnumerateFiles(targetPath, wildcardPatterns).Select(x =>
        {
            FilePath path = x - targetPath;
            if (path != targetPath)
                return path.ToString();

            return null;
        }).WhereNotNull());

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리의 모든 파일 경로(재귀적으로)를 비동기적으로 열거합니다.
        /// </summary>
        /// <returns>디렉토리의 모든 파일 경로 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="FilePath"/>입니다.</returns>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<FilePath>> GetAllFiles() => UniTask.RunOnThreadPool(() => Directory.EnumerateFiles(targetPath, "*", SearchOption.AllDirectories).Select(x => x - targetPath));

        /// <summary>
        /// 지정된 와일드카드 패턴과 일치하는, 이 핸들러가 나타내는 디렉토리의 모든 파일 경로(재귀적으로)를 비동기적으로 열거합니다.
        /// </summary>
        /// <param name="wildcardPatterns">파일 경로와 일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>지정된 패턴과 일치하는 파일 경로 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="FilePath"/>입니다.</returns>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<FilePath>> GetAllFiles(WildcardPatterns wildcardPatterns) => UniTask.RunOnThreadPool(() => DirectoryUtility.EnumerateFiles(targetPath, wildcardPatterns, SearchOption.AllDirectories).Select(x => x - targetPath));

        /// <summary>
        /// 이 핸들러가 나타내는 파일의 모든 바이트를 비동기적으로 읽습니다.
        /// </summary>
        /// <returns>파일의 모든 바이트를 포함하는 <see cref="byte"/> 배열입니다.</returns>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="FileNotFoundException">파일을 찾을 수 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="NotSupportedException">경로에 콜론(:)이 포함된 경우 발생합니다.</exception>
        public override UniTask<byte[]> ReadAllBytes() => File.ReadAllBytesAsync(targetPath).AsUniTask();

        /// <summary>
        /// 이 핸들러가 나타내는 파일의 모든 텍스트를 비동기적으로 읽습니다.
        /// </summary>
        /// <returns>파일의 모든 텍스트를 포함하는 <see cref="string"/>입니다.</returns>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="FileNotFoundException">파일을 찾을 수 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="NotSupportedException">경로에 콜론(:)이 포함된 경우 발생합니다.</exception>
        public override UniTask<string> ReadAllText() => File.ReadAllTextAsync(targetPath).AsUniTask();

        /// <summary>
        /// 이 핸들러가 나타내는 파일의 모든 줄을 비동기적으로 읽습니다.
        /// </summary>
        /// <returns>파일의 모든 줄을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="string"/>입니다.</returns>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="FileNotFoundException">파일을 찾을 수 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="NotSupportedException">경로에 콜론(:)이 포함된 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<string>> ReadLines() => UniTask.RunOnThreadPool(() => File.ReadLines(targetPath));

        /// <summary>
        /// 이 핸들러가 나타내는 파일을 읽기 모드로 열어 스트림을 비동기적으로 반환합니다.
        /// </summary>
        /// <returns>지정된 파일에 대한 읽기 전용 <see cref="FileStream"/>입니다.</returns>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="FileNotFoundException">파일을 찾을 수 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="NotSupportedException">경로에 콜론(:)이 포함된 경우 발생합니다.</exception>
        public override UniTask<Stream> OpenRead() => UniTask.RunOnThreadPool(() => (Stream)File.OpenRead(targetPath));
    }
}