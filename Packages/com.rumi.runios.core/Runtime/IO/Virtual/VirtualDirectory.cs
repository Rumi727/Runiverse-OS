#nullable enable
using RuniOS.Spans;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace RuniOS.IO.Virtual
{
    public class VirtualDirectory : VirtualDirectoryBase
    {
        readonly Dictionary<string, VirtualNode> children = new Dictionary<string, VirtualNode>();

        /// <summary>
        /// 지정된 경로에 새로운 디렉토리를 생성합니다.<br/>
        /// 중간 경로가 없으면 자동으로 생성됩니다.
        /// </summary>
        /// <param name="path">생성할 디렉토리의 경로입니다. 예: "assets/runios/textures", "assets/runios/sounds"</param>
        /// <returns>
        /// 디렉토리가 성공적으로 생성되었거나 이미 존재하여 접근할 수 있는 경우 <see langword="true"/>를 반환하고,<br/>
        /// 경로가 비어있는 경우 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="DirectoryNotFoundException">
        /// 경로의 주어진 세그먼트가 디렉토리가 아닌 다른 유형의 항목일 때 발생합니다.<br/>
        /// 예를 들어, 디렉토리를 생성하거나 찾으려는데 경로 중간 또는 마지막에 파일이 존재하는 경우,
        /// 시스템은 기대하는 디렉토리를 찾을 수 없으므로 이 예외를 발생시킵니다.
        /// </exception>
        public bool CreateDirectory(FilePath path)
        {
            ThrowIfDeletedException();

            if (path.IsEmpty())
                return false; // 빈 경로는 false 반환 (유효하지 않은 요청)

            bool isCreated = false;
            VirtualDirectoryBase childDirectory = this;
            foreach (var directoryNameSpan in path.value.AsSpan().Split(FilePath.directorySeparatorChar))
            {
                string directoryName = new string(directoryNameSpan);
                VirtualNode? childNode = childDirectory.GetChildNode(directoryName);
                if (childNode != null)
                {
                    if (childNode is VirtualDirectoryBase dirNode)
                        childDirectory = dirNode;
                    else
                    {
                        // 경로 중간에 파일이나 디렉토리가 아닌 다른 노드가 있는 경우
                        // 이는 비정상적인 상황이므로 예외를 던집니다.
                        ThrowPathIsFileException(path, directoryName);
                    }
                }
                else
                {
                    VirtualDirectory directory = new VirtualDirectory();

                    childDirectory.AttachChild(directoryName, directory);
                    childDirectory = directory;

                    isCreated = true;
                }
            }

            return isCreated;
        }

        public override VirtualNode? GetChildNode(string name)
        {
            ThrowIfInvalidNodeName(name);
            return children.GetValueOrDefault(name);
        }

        public override IEnumerable<VirtualNode> EnumerateNode() => children.Values;

        public override void AttachChild(string name, VirtualNode child)
        {
            ThrowIfInvalidNodeName(name);

            child.ThrowIfDeletedException();
            child.ThrowIfAttachedException();

            GetChildNode(name)?.ThrowIfAttachedException();

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화
            children[name] = child;

            BindChild(name, child);
        }

        public override void SetChild(string name, VirtualNode child)
        {
            ThrowIfInvalidNodeName(name);

            child.ThrowIfDeletedException();
            child.ThrowIfAttachedException();

            GetChildNode(name)?.Detach();

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화
            children[name] = child;

            BindChild(name, child);
        }

        protected internal override void OnDetachChild(VirtualNode child)
        {
            child.ThrowIfNotAttachedException();
            if (!children.ContainsKey(child.name))
                ThrowNodeNotFound(child.name);

            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화
            children.Remove(child.name);
        }

        public override void OnDelete()
        {
            InvalidateCache(); // 디렉토리 구조 변경 전에 캐시 무효화

            foreach (var item in children.ToList())
                item.Value.Delete();
        }

        /// <summary>
        /// 항상 예외를 던집니다.<br/>
        /// 이는 경로 중간에 파일이 있어 티렉토리를 탐색할 수 없는 상황에 사용됩니다.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">
        /// 경로 중간에 파일이 있어 디렉토리를 탐색할 수 없는 경우 발생합니다.
        /// </exception>
        [DoesNotReturn]
        static void ThrowInvalidDirectoryException(FilePath path) => throw new DirectoryNotFoundException($"The directory at path '{path}' was invalid.");


        /// <summary>
        /// 항상 예외를 던집니다.<br/>
        /// 이는 디렉토리를 기대했지만 실제로는 디렉토리가 아닌 다른 유형의 항목인 상황에 사용됩니다.
        /// </summary>
        /// <param name="path">문제가 발생한 전체 경로입니다.</param>
        /// <param name="segmentName">디렉토리가 아닌 항목의 이름(문제의 원인이 된 경로 세그먼트)입니다.</param>
        /// <exception cref="DirectoryNotFoundException">
        /// 경로의 주어진 세그먼트가 디렉토리가 아닌 다른 유형의 항목일 때 발생합니다.<br/>
        /// 예를 들어, 디렉토리를 생성하거나 찾으려는데 경로 중간 또는 마지막에 파일이 존재하는 경우,
        /// 시스템은 기대하는 디렉토리를 찾을 수 없으므로 이 예외를 발생시킵니다.
        /// </exception>
        [DoesNotReturn]
        static void ThrowPathIsFileException(FilePath path, string segmentName)
        {
            throw new DirectoryNotFoundException(
                $"Path operation failed for '{path}'. " +
                $"The segment '{segmentName}' is a file or another non-directory item, " +
                $"but a directory was expected."
            );
        }
    }
}