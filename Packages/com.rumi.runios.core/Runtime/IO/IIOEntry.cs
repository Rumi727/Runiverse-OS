#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;

namespace RuniOS.IO
{
    public interface IIOEntry
    {
        /// <summary>
        /// 이 핸들러의 최상위 핸들러를 가져옵니다.
        /// </summary>
        IIOEntry root { get; }

        /// <summary>
        /// 이 핸들러의 상위 핸들러를 가져옵니다. 최상위 핸들러인 경우 <see langword="null"/>입니다.
        /// </summary>
        IIOEntry? parent { get; }

        /// <summary>
        /// 이 <see cref="IIOEntry"/> 인스턴스가 참조하는 파일 시스템의 구조가 <b>외부 요인에 의해 임의로 변경되지 않는 독립적인 상태</b>인지 여부를 나타내는 값을 가져옵니다.<br/>
        /// 즉, 이 핸들러가 가리키는 경로 내부의 구조가 해당 핸들러 또는 개발자에 의해 제어되며, OS나 다른 외부 프로그램에 의해 마음대로 바뀔 수 없는 경우 <see langword="true"/>를 반환합니다.
        /// </summary>
        /// <remarks>
        /// <para>이 속성은 <see cref="IIOEntry"/>의 구체적인 구현에 따라 다르게 동작합니다:</para>
        /// <list type="bullet">
        /// <item><description>
        ///   <see langword="true"/>를 반환하는 경우: <see cref="IIOEntry"/>가 에셋 번들, 압축 파일(.zip, .jar 등),
        ///   또는 <see cref="VirtualDirectory"/>와 같이 자체적인 내부 구조를 가지며 외부에서 구조 변경이 어려운 대상을 참조할 때.<br/>
        ///   개발자가 직접 코드를 통해 구조를 정의하거나 변경하는 가상 파일 시스템 또한 여기에 해당합니다.
        /// </description></item>
        /// <item><description>
        ///   <see langword="false"/>를 반환하는 경우: <see cref="IIOEntry"/>가 파일 시스템의 일반적인 경로를 참조할 때.<br/>
        ///   이러한 경로는 OS나 다른 프로그램에 의해 디렉토리 구조나 파일이 임의로 생성, 삭제, 이동될 수 있으므로 독립적이지 않습니다.
        /// </description></item>
        /// </list>
        /// </remarks>
        bool isIndependent { get; }

        /// <summary>
        /// 이 핸들러가 참조하고 있는 디렉토리/파일 이름입니다.
        /// </summary>
        string name { get; }

        /// <summary>
        /// 전체 경로를 가져옵니다.
        /// </summary>
        FilePath fullPath { get; }



        /// <summary>
        /// 현재 위치를 최상위 경로로 취급하는 새 <see cref="IIOEntry"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>현재 위치를 기반으로 하는 새 <see cref="IIOEntry"/> 인스턴스입니다.</returns>
        IIOEntry Recreate();



        /// <summary>
        /// 지정된 경로를 사용하여 이 핸들러의 자식 <see cref="IIOEntry"/>를 생성합니다.
        /// </summary>
        /// <param name="path">자식 핸들러의 경로입니다.</param>
        /// <returns>생성된 <see cref="IIOEntry"/> 인스턴스입니다.</returns>
        IIOEntry CreateChild(FilePath path);

        /// <summary>
        /// 이 핸들러의 경로에 지정된 확장자를 추가하여 새 <see cref="IIOEntry"/>를 생성합니다.
        /// </summary>
        /// <param name="extension">추가할 확장자입니다.</param>
        /// <returns>확장자가 추가된 새 <see cref="IIOEntry"/> 인스턴스입니다.</returns>
        IIOEntry AddExtension(FileExtension extension);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리가 존재하는지 확인합니다.
        /// </summary>
        /// <returns>디렉터리가 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        UniTask<bool> DirectoryExists();

        /// <summary>
        /// 이 핸들러가 나타내는 파일이 존재하는지 확인합니다.
        /// </summary>
        /// <returns>파일이 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        UniTask<bool> FileExists();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 내의 모든 디렉터리 이름을 가져옵니다.
        /// </summary>
        /// <returns>디렉터리 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<string> GetDirectories();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내의 모든 디렉터리 이름을 가져옵니다.
        /// </summary>
        /// <returns>모든 디렉터리 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<FilePath> GetAllDirectories();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 내의 모든 파일 이름을 가져옵니다.
        /// </summary>
        /// <returns>파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<string> GetFiles();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 내의 모든 파일의 메타데이터를 가져옵니다.
        /// </summary>
        /// <returns>파일 메타데이터를 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 내에서 지정된 와일드카드 패턴과 일치하는 모든 파일 이름을 가져옵니다.
        /// </summary>
        /// <param name="wildcardPatterns">일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>일치하는 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<string> GetFiles(WildcardPatterns wildcardPatterns);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 내에서 지정된 와일드카드 패턴과 일치하는 모든 파일 메타데이터를 가져옵니다.
        /// </summary>
        /// <param name="wildcardPatterns">일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>일치하는 파일 메타데이터를 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData(WildcardPatterns wildcardPatterns);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내의 모든 파일 이름을 가져옵니다.
        /// </summary>
        /// <returns>모든 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<FilePath> GetAllFiles();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내의 모든 파일 메타데이터를 가져옵니다.
        /// </summary>
        /// <returns>모든 파일 메타데이터를 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData();

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내에서 지정된 와일드카드 패턴과 일치하는 모든 파일 이름을 가져옵니다.
        /// </summary>
        /// <param name="wildcardPatterns">일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>일치하는 모든 파일 이름 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<FilePath> GetAllFiles(WildcardPatterns wildcardPatterns);

        /// <summary>
        /// 이 핸들러가 나타내는 디렉터리 및 모든 하위 디렉터리 내에서 지정된 와일드카드 패턴과 일치하는 모든 파일 메타데이터를 가져옵니다.
        /// </summary>
        /// <param name="wildcardPatterns">일치시킬 와일드카드 패턴입니다.</param>
        /// <returns>모든 파일 메타데이터를 목록을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData(WildcardPatterns wildcardPatterns);

        /// <summary>
        /// 이 핸들러가 나타내는 파일의 모든 바이트를 읽습니다.
        /// </summary>
        /// <returns>파일의 모든 바이트를 포함하는 <see cref="byte"/>[]입니다.</returns>
        UniTask<byte[]> ReadAllBytes();

        /// <summary>
        /// 이 핸들러가 나타내는 파일의 모든 텍스트를 읽습니다.
        /// </summary>
        /// <returns>파일의 모든 텍스트를 포함하는 <see cref="string"/>입니다.</returns>
        UniTask<string> ReadAllText();

        /// <summary>
        /// 이 핸들러가 나타내는 파일의 모든 줄을 읽습니다.
        /// </summary>
        /// <returns>파일의 모든 줄을 포함하는 <see cref="IEnumerable{T}"/>입니다.</returns>
        IUniTaskAsyncEnumerable<string> ReadLines();

        /// <summary>
        /// 이 핸들러가 나타내는 파일에서 읽기 위한 스트림을 엽니다.
        /// </summary>
        /// <returns>파일에서 열린 <see cref="Stream"/>입니다.</returns>
        UniTask<Stream> OpenRead();



        UniTask<FileMetaData> GetFileMetaData();



        /// <summary>
        /// 이 핸들러가 다른 지정된 핸들러와 동일한 최종 대상(파일 또는 디렉터리)을 참조하는지 확인합니다.<br/>
        /// 이 비교는 핸들러의 내부 구현 방식이나 객체 인스턴스의 동일성과는 무관하게,
        /// 두 핸들러가 가리키는 논리적 경로의 동등성만을 확인합니다.
        /// </summary>
        /// <param name="other">비교할 다른 <see cref="IIOEntry"/> 인스턴스입니다.</param>
        /// <returns>두 핸들러가 동일한 대상을 참조하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        bool IsSameTarget(IIOEntry? other);
    }
}