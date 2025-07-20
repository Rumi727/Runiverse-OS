#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System;
using RuniEngine.Spans;
using System.Linq;

namespace RuniEngine.IO
{
    /// <summary>
    /// 가상 메모리 내의 파일 및 디렉토리 구조를 처리하는 핸들러입니다. 이 클래스는 상속될 수 없습니다.
    /// </summary>
    public sealed class MemoryIOHandler : IOHandler
    {
        /// <summary>
        /// 지정된 가상 디렉토리를 사용하여 <see cref="MemoryIOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="virtualDirectory">이 핸들러의 루트 가상 디렉토리입니다.</param>
        public MemoryIOHandler(VirtualDirectory virtualDirectory) => rootDirectory = virtualDirectory;

        /// <summary>
        /// 루트 가상 디렉토리, 부모 핸들러 및 자식 경로를 사용하여 <see cref="MemoryIOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="rootDirectory">이 핸들러의 루트 가상 디렉토리입니다.</param>
        /// <param name="parent">이 핸들러의 부모 <see cref="MemoryIOHandler"/>입니다.</param>
        /// <param name="childPath">이 핸들러의 자식 경로입니다.</param>
        MemoryIOHandler(VirtualDirectory rootDirectory, MemoryIOHandler? parent, string childPath) : base(parent, childPath) => this.rootDirectory = rootDirectory;

        /// <summary>
        /// 이 핸들러의 최상위 <see cref="MemoryIOHandler"/>를 가져옵니다.
        /// </summary>
        public new MemoryIOHandler? root => (MemoryIOHandler?)base.root;
        /// <summary>
        /// 이 핸들러의 부모 <see cref="MemoryIOHandler"/>를 가져옵니다.
        /// </summary>
        public new MemoryIOHandler? parent => (MemoryIOHandler?)base.parent;

        /// <summary>
        /// 이 핸들러가 독립적인지 여부를 나타내는 값을 가져옵니다. 이 값은 <see cref="rootDirectory"/>의 <see cref="VirtualDirectory.isIndependent"/> 값에 따라 결정됩니다.
        /// </summary>
        public override bool isIndependent => rootDirectory.isIndependent;

        /// <summary>
        /// 이 핸들러의 루트 가상 디렉토리를 가져옵니다.
        /// </summary>
        readonly VirtualDirectory rootDirectory;

        /// <summary>
        /// 현재 위치를 최상위 경로로 취급하는 새 <see cref="MemoryIOHandler"/> 인스턴스를 생성합니다.
        /// <br/>
        /// 주의: <see cref="VirtualDirectory"/>는 복제하지 않습니다.
        /// </summary>
        /// <returns>현재 위치를 기반으로 하는 새 <see cref="MemoryIOHandler"/> 인스턴스입니다.</returns>
        public override IOHandler Recreate() => new MemoryIOHandler(rootDirectory.GetDirectory(fullPath) ?? new VirtualDirectory());

        /// <summary>
        /// 지정된 경로를 사용하여 이 핸들러의 자식 <see cref="MemoryIOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="path">자식 핸들러의 경로입니다.</param>
        /// <returns>생성된 <see cref="MemoryIOHandler"/> 인스턴스입니다.</returns>
        public override IOHandler CreateChild(FilePath path)
        {
            MemoryIOHandler handler = this;
            if (string.IsNullOrEmpty(path))
                return handler;

            foreach (var item in path.value.AsSpan().SplitAny(FilePath.directorySeparatorChars))
            {
                string childPath = new string(item);
                handler = new MemoryIOHandler(rootDirectory, handler, childPath);
            }

            return handler;
        }

        /// <summary>
        /// 이 핸들러의 경로에 지정된 확장자를 추가하여 새 <see cref="MemoryIOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="extension">추가할 확장자입니다.</param>
        /// <returns>확장자가 추가된 새 <see cref="MemoryIOHandler"/> 인스턴스입니다.</returns>
        public override IOHandler AddExtension(FileExtension extension) => new MemoryIOHandler(rootDirectory, parent, name + extension);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리가 가상 디렉토리 내에 존재하는지 비동기적으로 확인합니다.
        /// </summary>
        /// <returns>디렉토리가 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환하는 <see cref="bool"/>입니다.</returns>
        public override UniTask<bool> DirectoryExists() => UniTask.FromResult(rootDirectory.GetDirectory(fullPath) != null);

        /// <summary>
        /// 이 핸들러가 나타내는 파일이 가상 디렉토리 내에 존재하는지 비동기적으로 확인합니다.
        /// </summary>
        /// <returns>파일이 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환하는 <see cref="bool"/>입니다.</returns>
        public override UniTask<bool> FileExists() => UniTask.FromResult(rootDirectory.GetFile(fullPath) != null);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리의 모든 서브디렉토리 이름을 비동기적으로 가져옵니다.
        /// </summary>
        /// <returns>디렉토리의 서브디렉토리 이름 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="string"/>입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<string>> GetDirectories() => UniTask.FromResult(rootDirectory.GetDirectories(fullPath));

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리의 모든 서브디렉토리 경로(재귀적으로)를 비동기적으로 가져옵니다.
        /// </summary>
        /// <returns>디렉토리의 모든 서브디렉토리 경로 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="FilePath"/>입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<FilePath>> GetAllDirectories() => UniTask.FromResult(rootDirectory.GetAllDirectories(fullPath));

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리의 모든 파일 이름을 비동기적으로 가져옵니다.
        /// </summary>
        /// <returns>디렉토리의 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="string"/>입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<string>> GetFiles() => UniTask.FromResult(rootDirectory.GetFiles(fullPath));

        /// <summary>
        /// 지정된 와일드카드 패턴과 일치하는, 이 핸들러가 나타내는 디렉토리의 모든 파일 이름을 비동기적으로 가져옵니다.
        /// </summary>
        /// <param name="wildcardPatterns">파일 이름과 일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>지정된 패턴과 일치하는 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="string"/>입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<string>> GetFiles(WildcardPatterns wildcardPatterns) => UniTask.FromResult(rootDirectory.GetFiles(fullPath).Where(x => WildcardUtility.IsMatch(x, wildcardPatterns)));

        /// <summary>
        /// 이 핸들러가 나타내는 디렉토리의 모든 파일 경로(재귀적으로)를 비동기적으로 가져옵니다.
        /// </summary>
        /// <returns>디렉토리의 모든 파일 경로 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="FilePath"/>입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<FilePath>> GetAllFiles() => UniTask.FromResult(rootDirectory.GetAllFiles(fullPath));

        /// <summary>
        /// 지정된 와일드카드 패턴과 일치하는, 이 핸들러가 나타내는 디렉토리의 모든 파일 경로(재귀적으로)를 비동기적으로 가져옵니다.
        /// </summary>
        /// <param name="wildcardPatterns">파일 경로와 일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>지정된 패턴과 일치하는 파일 경로 목록을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="FilePath"/>입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<FilePath>> GetAllFiles(WildcardPatterns wildcardPatterns) => UniTask.FromResult(rootDirectory.GetAllFiles(fullPath).Where(x => WildcardUtility.IsMatch(x, wildcardPatterns)));

        /// <summary>
        /// 이 핸들러가 나타내는 가상 파일의 모든 바이트를 비동기적으로 읽습니다.
        /// </summary>
        /// <returns>가상 파일의 모든 바이트를 포함하는 <see cref="byte"/> 배열입니다.</returns>
        /// <exception cref="FileNotFoundException">지정된 경로의 파일을 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<byte[]> ReadAllBytes() => rootDirectory.GetFile(fullPath)?.ReadAllBytesAsync() ?? throw new FileNotFoundException();

        /// <summary>
        /// 이 핸들러가 나타내는 가상 파일의 모든 텍스트를 비동기적으로 읽습니다.
        /// </summary>
        /// <returns>가상 파일의 모든 텍스트를 포함하는 <see cref="string"/>입니다.</returns>
        /// <exception cref="FileNotFoundException">지정된 경로의 파일을 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<string> ReadAllText() => rootDirectory.GetFile(fullPath)?.ReadAllTextAsync() ?? throw new FileNotFoundException();

        /// <summary>
        /// 이 핸들러가 나타내는 가상 파일의 모든 줄을 비동기적으로 읽습니다.
        /// </summary>
        /// <returns>가상 파일의 모든 줄을 포함하는 <see cref="IEnumerable{T}"/> of <see cref="string"/>입니다.</returns>
        /// <exception cref="FileNotFoundException">지정된 경로의 파일을 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<IEnumerable<string>> ReadLines() => rootDirectory.GetFile(fullPath)?.ReadLines() ?? throw new FileNotFoundException();

        /// <summary>
        /// 이 핸들러가 나타내는 가상 파일을 읽기 모드로 열어 스트림을 비동기적으로 반환합니다.
        /// </summary>
        /// <returns>지정된 가상 파일에 대한 읽기 전용 <see cref="Stream"/>입니다.</returns>
        /// <exception cref="FileNotFoundException">지정된 경로의 파일을 찾을 수 없는 경우 발생합니다.</exception>
        public override UniTask<Stream> OpenRead() => rootDirectory.GetFile(fullPath)?.OpenRead() ?? throw new FileNotFoundException();
    }
}