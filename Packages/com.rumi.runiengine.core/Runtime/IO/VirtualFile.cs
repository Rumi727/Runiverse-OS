#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RuniEngine.IO
{
    /// <summary>
    /// 가상 파일 시스템 내의 파일을 나타내는 클래스입니다.
    /// 파일의 내용을 메모리에 직접 저장하거나, <see cref="IOHandler"/>를 통해 실제 I/O 작업을 처리할 수 있습니다.
    /// </summary>
    public class VirtualFile : IVirtualNode
    {
        // IOHandler는 실제 파일 시스템과의 상호작용을 담당합니다.
        readonly IOHandler? ioHandler = null;
        readonly byte[] content = Array.Empty<byte>();

        /// <summary>
        /// 실제 파일 시스템에 접근할 수 있는 <see cref="IOHandler"/>를 사용하여 <see cref="VirtualFile"/>의 새 인스턴스를 초기화합니다.
        /// 이 생성자는 주로 실제 디스크의 파일을 가상 파일 시스템에 매핑할 때 사용됩니다.
        /// </summary>
        /// <param name="ioHandler">파일 I/O 작업을 처리할 <see cref="IOHandler"/> 인스턴스입니다.</param>
        public VirtualFile(IOHandler ioHandler) => this.ioHandler = ioHandler;

        /// <summary>
        /// 지정된 바이트 배열을 파일 내용으로 사용하여 <see cref="VirtualFile"/>의 새 인스턴스를 초기화합니다.
        /// 이 생성자는 파일 내용을 메모리에 직접 저장할 때 사용됩니다.
        /// </summary>
        /// <param name="content">파일의 원본 바이트 배열 내용입니다.</param>
        /// <exception cref=""
        public VirtualFile(byte[] content) => this.content = content.ToArray();

        /// <summary>
        /// 지정된 문자열을 UTF-8로 인코딩하여 파일 내용으로 사용하여 <see cref="VirtualFile"/>의 새 인스턴스를 초기화합니다.
        /// 이 생성자는 텍스트 파일을 가상으로 생성하거나 관리할 때 사용됩니다.
        /// </summary>
        /// <param name="content">파일의 원본 문자열 내용입니다.</param>
        public VirtualFile(string content) => this.content = Encoding.UTF8.GetBytes(content);



        /// <summary>
        /// 파일의 모든 내용을 비동기적으로 바이트 배열로 읽어옵니다.
        /// <see cref="IOHandler"/>가 지정된 경우 해당 핸들러를 사용하고, 그렇지 않으면 메모리에 저장된 내용을 반환합니다.
        /// </summary>
        /// <returns>파일의 모든 바이트 내용을 포함하는 <see cref="byte"/>[]입니다.</returns>
        public UniTask<byte[]> ReadAllBytesAsync() => ioHandler?.ReadAllBytes() ?? UniTask.RunOnThreadPool(() => content);

        /// <summary>
        /// 파일의 모든 내용을 비동기적으로 문자열로 읽어옵니다.
        /// <see cref="IOHandler"/>가 지정된 경우 해당 핸들러를 사용하고, 그렇지 않으면 메모리에 저장된 내용을 UTF-8로 디코딩하여 반환합니다.
        /// </summary>
        /// <returns>파일의 모든 텍스트 내용을 포함하는 <see cref="string"/>입니다.</returns>
        public UniTask<string> ReadAllTextAsync() => ioHandler?.ReadAllText() ?? UniTask.RunOnThreadPool(() => Encoding.UTF8.GetString(content));

        /// <summary>
        /// 파일의 모든 내용을 비동기적으로 읽어와 줄 단위의 문자열 컬렉션을 반환합니다.
        /// <see cref="IOHandler"/>가 지정된 경우 해당 핸들러를 사용하고, 그렇지 않으면 메모리에 저장된 내용을 줄 단위로 분리하여 반환합니다.
        /// </summary>
        /// <returns>파일의 각 줄을 나타내는 문자열의 <see cref="IEnumerable{T}"/> 컬렉션입니다.</returns>
        public UniTask<IEnumerable<string>> ReadLines() => ioHandler?.ReadLines() ?? UniTask.RunOnThreadPool(() => Encoding.UTF8.GetString(content).ReadLines());

        /// <summary>
        /// 파일의 내용을 읽기 위한 <see cref="Stream"/>을 비동기적으로 엽니다.
        /// <see cref="IOHandler"/>가 지정된 경우 해당 핸들러를 통해 스트림을 얻고, 그렇지 않으면 메모리 내용을 기반으로 <see cref="MemoryStream"/>을 반환합니다.
        /// </summary>
        /// <returns>파일 내용을 읽을 수 있는 <see cref="Stream"/> 스트림입니다.</returns>
        public UniTask<Stream> OpenRead() => ioHandler?.OpenRead() ?? UniTask.RunOnThreadPool(() => (Stream)new MemoryStream(content, false));
    }
}
