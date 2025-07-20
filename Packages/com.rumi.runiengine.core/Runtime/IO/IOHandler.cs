#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RuniEngine.IO
{
    /// <summary>
    /// 파일 시스템 작업을 처리하는 추상 기본 클래스입니다.
    /// </summary>
    public abstract class IOHandler
    {
        /// <summary>
        /// 아무 작업도 수행하지 않는 빈 <see cref="IOHandler"/> 인스턴스를 가져옵니다.
        /// </summary>
        public static readonly IOHandler empty = new EmptyIOHandler();



        /// <summary>
        /// <see cref="IOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        protected IOHandler() => root = this;

        /// <summary>
        /// 지정된 상위 핸들러와 하위 경로를 사용하여 <see cref="IOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="parent">이 핸들러의 상위 핸들러입니다. 최상위 핸들러인 경우 <see langword="null"/>일 수 있습니다.</param>
        /// <param name="name">상위 핸들러에 대한 상대적인 이 핸들러의 경로입니다.</param>
        protected IOHandler(IOHandler? parent, string name)
        {
            root = parent?.root ?? this;
            this.parent = parent;

            this.name = name;
            fullPath = parent?.fullPath + this.name;
        }


        /// <summary>
        /// 이 핸들러의 최상위 핸들러를 가져옵니다.
        /// </summary>
        public IOHandler root { get; }

        /// <summary>
        /// 이 핸들러의 상위 핸들러를 가져옵니다. 최상위 핸들러인 경우 <see langword="null"/>입니다.
        /// </summary>
        public IOHandler? parent { get; }

        /// <summary>
        /// 이 <see cref="IOHandler"/> 인스턴스가 참조하는 파일 시스템의 구조가 **외부 요인에 의해 임의로 변경되지 않는 독립적인 상태**인지 여부를 나타내는 값을 가져옵니다.<br/>
        /// 즉, 이 핸들러가 가리키는 경로 내부의 구조가 해당 핸들러 또는 개발자에 의해 제어되며, OS나 다른 외부 프로그램에 의해 마음대로 바뀔 수 없는 경우 <see langword="true"/>를 반환합니다.
        /// </summary>
        /// <remarks>
        /// <para>이 속성은 <see cref="IOHandler"/>의 구체적인 구현에 따라 다르게 동작합니다:</para>
        /// <list type="bullet">
        /// <item><description>
        ///   <see langword="true"/>를 반환하는 경우: <see cref="IOHandler"/>가 에셋 번들, 압축 파일(.zip, .jar 등),
        ///   또는 <see cref="VirtualDirectory"/>와 같이 자체적인 내부 구조를 가지며 외부에서 구조 변경이 어려운 대상을 참조할 때.<br/>
        ///   개발자가 직접 코드를 통해 구조를 정의하거나 변경하는 가상 파일 시스템 또한 여기에 해당합니다.
        /// </description></item>
        /// <item><description>
        ///   <see langword="false"/>를 반환하는 경우: <see cref="IOHandler"/>가 파일 시스템의 일반적인 경로를 참조할 때.<br/>
        ///   이러한 경로는 OS나 다른 프로그램에 의해 디렉토리 구조나 파일이 임의로 생성, 삭제, 이동될 수 있으므로 독립적이지 않습니다.
        /// </description></item>
        /// </list>
        /// </remarks>
        public abstract bool isIndependent { get; }

        /// <summary>
        /// 이 핸들러가 참조하고 있는 디렉토리/파일 이름입니다.
        /// </summary>
        public string name { get; } = string.Empty;

        /// <summary>
        /// 전체 경로를 가져옵니다.
        /// </summary>
        public FilePath fullPath { get; } = new FilePath();



        /// <summary>
        /// 현재 위치를 최상위 경로로 취급하는 새 <see cref="IOHandler"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>현재 위치를 기반으로 하는 새 <see cref="IOHandler"/> 인스턴스입니다.</returns>
        public abstract IOHandler Recreate();



        /// <summary>
        /// 지정된 경로를 사용하여 이 핸들러의 자식 <see cref="IOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="path">자식 핸들러의 경로입니다.</param>
        /// <returns>생성된 <see cref="IOHandler"/> 인스턴스입니다.</returns>
        public abstract IOHandler CreateChild(FilePath path);

        /// <summary>
        /// 지정된 경로를 사용하여 이 핸들러의 자식 <see cref="IOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="path">자식 핸들러의 경로입니다.</param>
        /// <returns>생성된 <see cref="IOHandler"/> 인스턴스입니다.</returns>
        public IOHandler CreateChild(FilePath path1, FilePath path2) => CreateChild(path1).CreateChild(path2);

        /// <summary>
        /// 지정된 경로를 사용하여 이 핸들러의 자식 <see cref="IOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="path">자식 핸들러의 경로입니다.</param>
        /// <returns>생성된 <see cref="IOHandler"/> 인스턴스입니다.</returns>
        public IOHandler CreateChild(FilePath path1, FilePath path2, FilePath path3) => CreateChild(path1).CreateChild(path2).CreateChild(path3);

        /// <summary>
        /// 지정된 경로를 사용하여 이 핸들러의 자식 <see cref="IOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="path">자식 핸들러의 경로입니다.</param>
        /// <returns>생성된 <see cref="IOHandler"/> 인스턴스입니다.</returns>
        public IOHandler CreateChild(FilePath path1, FilePath path2, FilePath path3, FilePath path4) => CreateChild(path1).CreateChild(path2).CreateChild(path3).CreateChild(path4);

        /// <summary>
        /// 지정된 경로를 사용하여 이 핸들러의 자식 <see cref="IOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="path">자식 핸들러의 경로입니다.</param>
        /// <returns>생성된 <see cref="IOHandler"/> 인스턴스입니다.</returns>
        public IOHandler CreateChild(FilePath path1, FilePath path2, FilePath path3, FilePath path4, FilePath path5) => CreateChild(path1).CreateChild(path2).CreateChild(path3).CreateChild(path4).CreateChild(path5);

        /// <summary>
        /// 지정된 경로를 사용하여 이 핸들러의 자식 <see cref="IOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="path">자식 핸들러의 경로입니다.</param>
        /// <returns>생성된 <see cref="IOHandler"/> 인스턴스입니다.</returns>
        public IOHandler CreateChild(params FilePath[] paths)
        {
            IOHandler handler = this;
            for (int i = 0; i < paths.Length; i++)
                handler = handler.CreateChild(paths[i]);

            return handler;
        }

        /// <summary>
        /// 이 핸들러의 경로에 지정된 확장자를 추가하여 새 <see cref="IOHandler"/>를 생성합니다.
        /// </summary>
        /// <param name="extension">추가할 확장자입니다.</param>
        /// <returns>확장자가 추가된 새 <see cref="IOHandler"/> 인스턴스입니다.</returns>
        public abstract IOHandler AddExtension(FileExtension extension);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리가 존재하는지 확인합니다.
        /// </summary>
        /// <returns>디렉터리가 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        public abstract UniTask<bool> DirectoryExists();

        /// <summary>
        /// 이 핸들러가 나타내는 파일이 존재하는지 확인합니다.
        /// </summary>
        /// <returns>파일이 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        public abstract UniTask<bool> FileExists();

        /// <summary>
        /// 지정된 와일드카드 패턴과 일치하는 파일이 존재하는지 확인합니다.
        /// </summary>
        /// <param name="wildcardPatterns">파일 존재 여부를 확인할 와일드카드 패턴입니다.</param>
        /// <returns>일치하는 파일이 있으면 해당 파일의 <see cref="IOHandler"/>를 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.</returns>
        public async UniTask<IOHandler?> FileExists(WildcardPatterns wildcardPatterns)
        {
            for (int i = 0; i < wildcardPatterns.patterns.Count; i++)
            {
                string extension = wildcardPatterns.patterns[i];

                IOHandler extensionHandler = AddExtension(extension);
                if (await extensionHandler.FileExists())
                    return extensionHandler;
            }

            return null;
        }

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 내의 모든 디렉터리 이름을 가져옵니다.
        /// </summary>
        /// <returns>디렉터리 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public abstract UniTask<IEnumerable<string>> GetDirectories();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내의 모든 디렉터리 이름을 가져옵니다.
        /// </summary>
        /// <returns>모든 디렉터리 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public abstract UniTask<IEnumerable<FilePath>> GetAllDirectories();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 내의 모든 파일 이름을 가져옵니다.
        /// </summary>
        /// <returns>파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public abstract UniTask<IEnumerable<string>> GetFiles();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 내에서 지정된 와일드카드 패턴과 일치하는 모든 파일 이름을 가져옵니다.
        /// </summary>
        /// <param name="wildcardPatterns">일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>일치하는 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public abstract UniTask<IEnumerable<string>> GetFiles(WildcardPatterns wildcardPatterns);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내의 모든 파일 이름을 가져옵니다.
        /// </summary>
        /// <returns>모든 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public abstract UniTask<IEnumerable<FilePath>> GetAllFiles();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내에서 지정된 와일드카드 패턴과 일치하는 모든 파일 이름을 가져옵니다.
        /// </summary>
        /// <param name="wildcardPatterns">일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>일치하는 모든 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public abstract UniTask<IEnumerable<FilePath>> GetAllFiles(WildcardPatterns wildcardPatterns);

        /// <summary>
        /// 이 핸들러가 나타내는 파일의 모든 바이트를 읽습니다.
        /// </summary>
        /// <returns>파일의 모든 바이트를 포함하는 <see cref="byte"/>[]입니다.</returns>
        public abstract UniTask<byte[]> ReadAllBytes();

        /// <summary>
        /// 이 핸들러가 나타내는 파일의 모든 텍스트를 읽습니다.
        /// </summary>
        /// <returns>파일의 모든 텍스트를 포함하는 <see cref="string"/>입니다.</returns>
        public abstract UniTask<string> ReadAllText();

        /// <summary>
        /// 이 핸들러가 나타내는 파일의 모든 줄을 읽습니다.
        /// </summary>
        /// <returns>파일의 모든 줄을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        public abstract UniTask<IEnumerable<string>> ReadLines();

        /// <summary>
        /// 이 핸들러가 나타내는 파일에서 읽기 위한 스트림을 엽니다.
        /// </summary>
        /// <returns>파일에서 열린 <see cref="Stream"/>입니다.</returns>
        public abstract UniTask<Stream> OpenRead();



        public override string ToString() => fullPath;

        sealed class EmptyIOHandler : IOHandler
        {
            public EmptyIOHandler() { }
            EmptyIOHandler(IOHandler? parent, string name) : base(parent, name) { }

            public override bool isIndependent => true;

            public override IOHandler Recreate() => new EmptyIOHandler();

            public override IOHandler CreateChild(FilePath path) => new EmptyIOHandler(this, path);
            public override IOHandler AddExtension(FileExtension extension) => new EmptyIOHandler(parent, name + extension);

            public override UniTask<bool> DirectoryExists() => UniTask.FromResult(false);

            public override UniTask<bool> FileExists() => UniTask.FromResult(false);

            public override UniTask<IEnumerable<string>> GetDirectories() => UniTask.FromResult(Enumerable.Empty<string>());

            public override UniTask<IEnumerable<FilePath>> GetAllDirectories() => UniTask.FromResult(Enumerable.Empty<FilePath>());

            public override UniTask<IEnumerable<string>> GetFiles() => UniTask.FromResult(Enumerable.Empty<string>());
            public override UniTask<IEnumerable<string>> GetFiles(WildcardPatterns wildcardPatterns) => UniTask.FromResult(Enumerable.Empty<string>());

            public override UniTask<IEnumerable<FilePath>> GetAllFiles() => UniTask.FromResult(Enumerable.Empty<FilePath>());
            public override UniTask<IEnumerable<FilePath>> GetAllFiles(WildcardPatterns wildcardPatterns) => UniTask.FromResult(Enumerable.Empty<FilePath>());

            public override UniTask<byte[]> ReadAllBytes() => new UniTask<byte[]>(Array.Empty<byte>());

            public override UniTask<string> ReadAllText() => new UniTask<string>(string.Empty);

            public override UniTask<IEnumerable<string>> ReadLines() => UniTask.FromResult(Enumerable.Empty<string>());

            public override UniTask<Stream> OpenRead() => UniTask.FromResult(Stream.Null);
        }
    }
}
