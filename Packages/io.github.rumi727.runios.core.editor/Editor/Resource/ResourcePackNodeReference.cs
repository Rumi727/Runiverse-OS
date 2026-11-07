#nullable enable
using RuniOS.IO;

namespace RuniOS.Editor.Resource
{
    // 도메인을 다시 불러오면 프로바이더가 새로 만들어지므로, 노드를 위치(종류와 패키지 이름)와 경로로 저장해 두었다가 다시 찾습니다.
    [Serializable]
    struct ResourcePackNodeReference
    {
        public bool editor;
        public string package;
        public string path;

        public static bool TryCreate(IONode node, out ResourcePackNodeReference reference)
        {
            if (!TryFindSource(node.provider, out bool editor, out EditorStreamingAssetsService.Source source))
            {
                reference = default;
                return false;
            }

            reference = new ResourcePackNodeReference { editor = editor, package = source.package?.name ?? string.Empty, path = node.path.value };
            return true;
        }

        public readonly bool TryResolve(out IONode node)
        {
            foreach (EditorStreamingAssetsService.Source source in editor ? EditorStreamingAssetsService.editorSources : EditorStreamingAssetsService.sources)
            {
                if ((source.package?.name ?? string.Empty) != (package ?? string.Empty))
                    continue;

                node = new IONode(source.provider, RuniPath.From(path ?? string.Empty));
                return true;
            }

            node = default;
            return false;
        }

        public static bool TryFindSource(IIOProvider provider, out bool editor, out EditorStreamingAssetsService.Source source)
        {
            foreach (EditorStreamingAssetsService.Source item in EditorStreamingAssetsService.sources)
            {
                if (item.provider != provider)
                    continue;

                editor = false;
                source = item;
                return true;
            }

            foreach (EditorStreamingAssetsService.Source item in EditorStreamingAssetsService.editorSources)
            {
                if (item.provider != provider)
                    continue;

                editor = true;
                source = item;
                return true;
            }

            editor = false;
            source = default;
            return false;
        }
    }
}
