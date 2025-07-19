#nullable enable
using RuniEngine.IO;
using RuniEngine.Spans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RuniEngine
{
    /// <summary>
    /// 가상 파일 시스템 내의 디렉토리를 나타내는 클래스입니다.
    /// 이 클래스는 계층적 디렉토리 구조를 관리하며, 하위 디렉토리와 파일을 포함할 수 있습니다.
    /// </summary>
    public sealed class VirtualDirectory : IVirtualNode
    {
        /// <summary>
        /// 새로운 <see cref="VirtualDirectory"/> 인스턴스를 초기화하고, 자신을 루트 디렉토리로 설정합니다.
        /// 이 생성자는 가상 파일 시스템의 최상위 루트 디렉토리를 생성할 때 사용됩니다.
        /// </summary>
        public VirtualDirectory() => root = this;

        /// <summary>
        /// 지정된 루트 디렉토리와 부모 디렉토리를 가진 새로운 <see cref="VirtualDirectory"/> 인스턴스를 초기화합니다.
        /// 이 생성자는 하위 디렉토리를 생성할 때 내부적으로 사용됩니다.
        /// </summary>
        /// <param name="root">이 디렉토리가 속한 가상 파일 시스템의 최상위 루트 디렉토리입니다.</param>
        /// <param name="parent">이 디렉토리의 부모 디렉토리입니다. 루트 디렉토리인 경우 <see langword="null"/>일 수 있습니다.</param>
        VirtualDirectory(VirtualDirectory? parent)
        {
            this.root = parent?.root ?? this;
            this.parent = parent;
        }

        /// <summary>
        /// 이 디렉토리가 속한 가상 파일 시스템의 최상위 루트 디렉토리를 가져옵니다.<br/>
        /// 이 속성은 읽기 전용입니다.
        /// </summary>
        public VirtualDirectory root { get; }

        /// <summary>
        /// 이 디렉토리의 부모 디렉토리를 가져옵니다.<br/>
        /// 이 속성은 읽기 전용이며, 루트 디렉토리인 경우 <see langword="null"/>입니다.
        /// </summary>
        public VirtualDirectory? parent { get; } = null;

        /// <summary>
        /// 이 가상 파일 시스템 엔트리(디렉토리 또는 파일)가 독립적인 최상위 항목인지 여부를 나타내는 값을 가져옵니다.
        /// 즉, 이 항목이 다른 가상 파일 시스템 엔트리의 하위가 아닌, 스스로 루트 역할을 하는지 여부를 나타냅니다.
        /// </summary>
        public bool isIndependent => root == this && parent == null;

        /// <summary>
        /// 이 디렉토리의 직접적인 하위 항목(디렉토리 및 파일)을 저장하는 컬렉션입니다.
        /// </summary>
        readonly ConcurrentDictionary<string, IVirtualNode> children = new();

        /// <summary>
        /// 지정된 경로에 새로운 디렉토리를 생성합니다.<br/>
        /// 중간 경로가 없으면 자동으로 생성됩니다.
        /// </summary>
        /// <param name="path">생성할 디렉토리의 경로입니다. 예: "Assets/Textures", "Data/Levels"</param>
        /// <returns>
        /// 디렉토리가 성공적으로 생성되었거나 이미 존재하여 접근할 수 있는 경우 <see langword="true"/>를 반환하고,<br/>
        /// 경로가 비어있거나, 경로의 중간에 파일이 있거나, 디렉토리가 아닌 다른 유형의 항목이 경로에 존재하는 경우 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool CreateDirectory(FilePath path)
        {
            if (path.IsEmpty())
                return false;
            
            bool isCreated = false;
            VirtualDirectory childDirectory = this;
            foreach (var directoryNameSpan in path.value.AsSpan().Split(FilePath.directorySeparatorChar))
            {
                string directoryName = directoryNameSpan.AsSpan().ToString();
                if (childDirectory.children.ContainsKey(directoryName))
                {
                    var entry = children[directoryName];
                    if (entry is VirtualDirectory value)
                        childDirectory = value;
                    else
                        return false;
                }
                else
                {
                    VirtualDirectory directory = new VirtualDirectory(childDirectory);

                    childDirectory.children[directoryName] = directory;
                    childDirectory = directory;

                    isCreated = true;
                }
            }

            return isCreated;
        }

        /// <summary>
        /// 지정된 경로에 해당하는 <see cref="VirtualDirectory"/> 인스턴스를 가져옵니다.
        /// </summary>
        /// <param name="path">가져올 디렉토리의 경로입니다. 예: "Data", "Config/Settings"</param>
        /// <returns>지정된 경로의 <see cref="VirtualDirectory"/> 인스턴스입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public VirtualDirectory GetDirectory(FilePath path)
        {
            if (path.IsEmpty())
                return this;

            VirtualDirectory childDirectory = this;
            foreach (var directoryName in path.value.AsSpan().Split(FilePath.directorySeparatorChar))
            {
                if (childDirectory.children.TryGetValue(new string(directoryName), out IVirtualNode value) && value is VirtualDirectory valueDirectory)
                {
                    childDirectory = valueDirectory;
                    continue;
                }

                throw new DirectoryNotFoundException();
            }

            return childDirectory;
        }

        /// <summary>
        /// 지정된 경로에 가상 파일을 씁니다.<br/>
        /// 파일이 위치할 디렉토리는 자동으로 생성되지 않습니다. 미리 <see cref="CreateDirectory(FilePath)"/>를 통해 생성해야 합니다.
        /// </summary>
        /// <param name="path">파일을 쓸 경로입니다. 예: "Assets/Data/myFile.txt"</param>
        /// <param name="virtualFile">쓸 <see cref="VirtualFile"/> 인스턴스입니다.</param>
        /// <exception cref="DirectoryNotFoundException">파일을 쓸 상위 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public void FileWrite(FilePath path, VirtualFile virtualFile) => GetDirectory(path--).children[path.GetFileName()] = virtualFile;

        /// <summary>
        /// 지정된 경로에 해당하는 <see cref="VirtualFile"/> 인스턴스를 가져옵니다.
        /// </summary>
        /// <param name="path">가져올 파일의 경로입니다. 예: "Assets/Sprites/player.png", "Resources/config.json"</param>
        /// <returns>지정된 경로의 <see cref="VirtualFile"/> 인스턴스입니다.</returns>
        /// <exception cref="FileNotFoundException">지정된 경로의 파일을 찾을 수 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">파일이 위치한 상위 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public VirtualFile GetFile(FilePath path)
        {
            FilePath parentPath = path.GetParentPath();
            string fileName = path.GetFileName();

            return GetDirectory(parentPath).children.GetValueOrDefault(fileName) as VirtualFile ?? throw new FileNotFoundException();
        }



        /// <summary>
        /// 지정된 경로에 있는 모든 직접적인 하위 디렉토리의 이름을 가져옵니다.
        /// </summary>
        /// <param name="path">하위 디렉토리를 검색할 디렉토리의 경로입니다.</param>
        /// <returns>해당 디렉토리의 모든 직접적인 하위 디렉토리 이름 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IEnumerable<string> GetDirectories(FilePath path) => GetDirectory(path).children.Where(x => x.Value is VirtualDirectory).Select(x => x.Key);

        /// <summary>
        /// 지정된 경로를 시작으로 모든 하위 디렉토리의 전체 경로를 깊이 우선 탐색(DFS) 방식으로 가져옵니다.<br/>
        /// 시작 경로 자체는 결과에 포함되지 않습니다.
        /// </summary>
        /// <param name="path">탐색을 시작할 디렉토리의 경로입니다.</param>
        /// <returns>시작 경로의 모든 하위 디렉토리의 전체 경로 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IEnumerable<FilePath> GetAllDirectories(FilePath path) => InternalGetAllDirectories(path, false).Select(x => x.Key);

        /// <summary>
        /// 재귀적으로 모든 하위 디렉토리를 탐색하여 경로와 <see cref="VirtualDirectory"/> 쌍을 반환합니다.<br/>
        /// 깊이 우선 탐색(DFS) 방식을 사용합니다.
        /// </summary>
        /// <param name="path">탐색을 시작할 디렉토리의 경로입니다.</param>
        /// <param name="includeSelf">탐색 시작 디렉토리 자체를 결과에 포함할지 여부입니다.</param>
        /// <returns>탐색된 모든 디렉토리의 <see cref="FilePath"/>와 <see cref="VirtualDirectory"/> 쌍 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        IEnumerable<KeyValuePair<FilePath, VirtualDirectory>> InternalGetAllDirectories(FilePath path, bool includeSelf)
        {
            VirtualDirectory initialDirectory = GetDirectory(path);
            if (includeSelf)
                yield return new(path, initialDirectory);

            // DFS(깊이 우선 탐색)를 위해 Stack 사용
            Stack<(FilePath currentPath, VirtualDirectory dir)> stack = new Stack<(FilePath currentPath, VirtualDirectory dir)>();
            stack.Push((path, initialDirectory)); // 시작 디렉토리를 스택에 추가

            while (stack.Count > 0)
            {
                (FilePath currentPath, VirtualDirectory currentDir) = stack.Pop(); // 스택에서 디렉토리와 현재 경로를 꺼냄

                foreach (var item in currentDir.children.Where(x => x.Value is VirtualDirectory).Reverse()) // children이 Dictionary인 경우 Values 사용
                {
                    VirtualDirectory childDirectory = (VirtualDirectory)item.Value;
                    FilePath newPath = currentPath + item.Key;

                    yield return new(newPath, childDirectory); // 자식 디렉토리와 조합된 경로 반환

                    stack.Push((newPath, childDirectory)); // 자식 디렉토리와 새로운 경로를 스택에 추가하여 나중에 탐색
                }
            }
        }

        /// <summary>
        /// 지정된 경로에 있는 모든 직접적인 하위 파일의 이름을 가져옵니다.
        /// </summary>
        /// <param name="path">파일을 검색할 디렉토리의 경로입니다.</param>
        /// <returns>해당 디렉토리의 모든 직접적인 하위 파일 이름 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IEnumerable<string> GetFiles(FilePath path) => GetDirectory(path).children.Where(x => x.Value is VirtualFile).Select(x => x.Key);

        /// <summary>
        /// 지정된 경로를 시작으로 모든 하위 디렉토리의 파일을 포함하여 모든 파일의 전체 경로를 가져옵니다.
        /// </summary>
        /// <param name="path">탐색을 시작할 디렉토리의 경로입니다.</param>
        /// <returns>시작 경로의 모든 하위 디렉토리에 있는 모든 파일의 전체 경로 컬렉션입니다.</returns>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IEnumerable<FilePath> GetAllFiles(FilePath path)
        {
            var directories = InternalGetAllDirectories(path, true);
            return directories.SelectMany(directoryItem => directoryItem.Value.children.Where(x => x.Value is VirtualFile).Select(fileItem => directoryItem.Key + fileItem.Key));
        }
    }
}
