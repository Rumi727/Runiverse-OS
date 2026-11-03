#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using System.IO;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// IO 항목을 다른 폴더로 옮기거나 복사하거나 링크하며, 같은 이름이 이미 있으면 대화 상자로 어떻게 할지 묻습니다.
    /// </summary>
    /// <remarks>
    /// 파일은 덮어쓰기·이름 바꾸기·건너뛰기를, 폴더는 합치기·이름 바꾸기·건너뛰기를 고를 수 있으며, 취소하면 남은 항목을 처리하지 않습니다.
    /// 폴더를 합치면 그 안의 항목도 같은 방식으로 처리합니다.
    /// <br/>
    /// 이동과 링크는 원본과 대상이 같은 프로바이더 인스턴스일 때만 할 수 있습니다. 프로바이더가 다르면 항상 원본을 읽어 대상에 새로 쓰는 복사입니다.
    /// 링크는 덮어쓰거나 합칠 수 없어, 같은 이름이 있으면 이름 바꾸기·건너뛰기·취소만 고를 수 있습니다.
    /// </remarks>
    public sealed class IOTransfer
    {
        enum Resolution
        {
            // 폴더는 합치기입니다.
            Overwrite,
            Rename,
            Skip
        }

        readonly IIOProvider sourceProvider;
        readonly IWritableIOProvider destinationProvider;
        readonly bool sameProvider;
        readonly IODropOperation operation;
        readonly VisualElement dialogTarget;

        // "모두 적용"을 고른 처리 방법이며, 파일과 폴더를 따로 기억합니다.
        Resolution? fileResolution;
        Resolution? directoryResolution;
        bool cancelled;

        IOTransfer(IIOProvider sourceProvider, IWritableIOProvider destinationProvider, IODropOperation operation, VisualElement dialogTarget)
        {
            this.sourceProvider = sourceProvider;
            this.destinationProvider = destinationProvider;
            this.operation = operation;
            this.dialogTarget = dialogTarget;

            sameProvider = ReferenceEquals(sourceProvider, destinationProvider);
        }

        /// <summary>
        /// <paramref name="entries"/>를 <paramref name="directory"/> 안으로 복사할 수 있는지 확인합니다. 폴더를 자기 자신이나 그 안으로는 복사할 수 없습니다.
        /// </summary>
        /// <param name="sourceProvider"><paramref name="entries"/>가 들어 있는 프로바이더입니다.</param>
        public static bool CanCopy(IIOProvider sourceProvider, IEnumerable<IOEntry> entries, IONode directory) =>
            directory.provider is IWritableIOProvider
            && (!ReferenceEquals(sourceProvider, directory.provider) || !entries.Any(x => x.isDirectory && IsSameOrDescendant(directory.path, x.path)));

        /// <summary>
        /// <paramref name="entries"/>를 <paramref name="directory"/> 안으로 옮길 수 있는지 확인합니다.
        /// 원본과 대상이 같은 프로바이더 인스턴스여야 하며, 이미 그 폴더에 있는 항목만 옮기면 바뀌는 것이 없으므로 옮길 수 없습니다.
        /// </summary>
        /// <param name="sourceProvider"><paramref name="entries"/>가 들어 있는 프로바이더입니다.</param>
        public static bool CanMove(IIOProvider sourceProvider, IEnumerable<IOEntry> entries, IONode directory) =>
            ReferenceEquals(sourceProvider, directory.provider)
            && sourceProvider is IWritableIOProvider
            && CanCopy(sourceProvider, entries, directory)
            && entries.Any(x => !PathEquals(x.path.GetParentPath(), directory.path));

        /// <summary>
        /// <paramref name="directory"/> 안에 항목의 심볼릭 링크를 만들 수 있는지 확인합니다.
        /// 원본과 대상이 같은 프로바이더 인스턴스여야 하며, 그 프로바이더가 <see cref="ISymbolicLinkIOProvider"/>여야 합니다.
        /// </summary>
        /// <param name="sourceProvider">링크가 가리킬 항목이 들어 있는 프로바이더입니다.</param>
        public static bool CanLink(IIOProvider sourceProvider, IONode directory) =>
            ReferenceEquals(sourceProvider, directory.provider) && sourceProvider is ISymbolicLinkIOProvider;

        /// <summary>
        /// <paramref name="entries"/>를 <paramref name="directory"/> 안으로 옮기거나 복사하거나 링크합니다. 항목마다 실패하면 로그를 남기고 다음 항목을 처리합니다.
        /// </summary>
        /// <param name="sourceProvider"><paramref name="entries"/>가 들어 있는 프로바이더입니다.</param>
        /// <param name="dialogTarget">이름이 겹칠 때 띄우는 대화 상자를 표시할 패널을 정하는 요소입니다.</param>
        public static UniTask Run(IIOProvider sourceProvider, IEnumerable<IOEntry> entries, IOWriteNode directory, IODropOperation operation, VisualElement dialogTarget) =>
            new IOTransfer(sourceProvider, directory.provider, operation, dialogTarget).TransferAll(entries, directory.path);

        static bool PathEquals(RuniPath x, RuniPath y) => string.Equals(x.value, y.value, StringComparison.Ordinal);

        static bool IsSameOrDescendant(RuniPath path, RuniPath ancestorPath) =>
            PathEquals(path, ancestorPath)
            || path.value.StartsWith(ancestorPath.value + RuniPath.directorySeparatorChar, StringComparison.Ordinal);

        async UniTask TransferAll(IEnumerable<IOEntry> entries, RuniPath directoryPath)
        {
            foreach (IOEntry entry in entries)
            {
                if (cancelled)
                    return;

                // 이미 그 폴더에 있는 항목은 옮겨도 바뀌지 않으므로 건너뜁니다.
                if (operation == IODropOperation.Move && PathEquals(entry.path.GetParentPath(), directoryPath))
                    continue;

                try
                {
                    await Transfer(entry, directoryPath / entry.path.GetFileName());
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        async UniTask Transfer(IOEntry entry, RuniPath destinationPath)
        {
            if (await destinationProvider.GetEntry(destinationPath) is not { } existing)
            {
                await TransferItem(entry, destinationPath, false);
                return;
            }

            // 종류가 다르면 덮어쓰거나 합칠 수 없고, 자기 자신에게 덮어쓰면 원본을 덮어쓰게 됩니다.
            bool isSelf = sameProvider && PathEquals(destinationPath, entry.path);
            // 링크는 이미 있는 항목을 대체하거나 합치지 않으므로 덮어쓸 수 없습니다.
            bool canOverwrite = entry.isDirectory == existing.isDirectory && !isSelf && operation != IODropOperation.Link;
            RuniPath renamedPath = await GetAvailablePath(destinationPath, entry.isDirectory);

            switch (await Resolve(entry, canOverwrite, renamedPath))
            {
                case Resolution.Overwrite when entry.isDirectory:
                    await Merge(entry, destinationPath);
                    break;
                case Resolution.Overwrite:
                    await TransferItem(entry, destinationPath, true);
                    break;
                case Resolution.Rename:
                    await TransferItem(entry, renamedPath, false);
                    break;
                case Resolution.Skip:
                    break;
                default:
                    cancelled = true;
                    break;
            }
        }

        // 대상이 없거나 덮어쓸 파일입니다. 폴더는 대상이 없을 때만 들어옵니다.
        async UniTask TransferItem(IOEntry entry, RuniPath destinationPath, bool overwrite)
        {
            if (operation == IODropOperation.Link)
                await ((ISymbolicLinkIOProvider)destinationProvider).CreateSymbolicLink(destinationPath, entry.path, entry.isDirectory);
            else if (operation == IODropOperation.Move)
                await (entry.isDirectory ? destinationProvider.MoveDirectory(entry.path, destinationPath) : destinationProvider.MoveFile(entry.path, destinationPath, overwrite));
            else if (!entry.isDirectory)
                await CopyFile(entry.path, destinationPath, overwrite);
            else if (sameProvider)
                await destinationProvider.CopyDirectory(entry.path, destinationPath);
            else
                await CopyDirectoryAcrossProviders(entry.path, destinationPath);
        }

        // 이미 있는 폴더 안으로 자식을 하나씩 옮기거나 복사합니다.
        // 이동해도 원본 폴더는 지우지 않습니다. 폴더 삭제는 안쪽까지 지우는데, 열거에서 빠진 항목(접근할 수 없는 항목 등)이 있으면 보지 못한 채 함께 지워지기 때문입니다.
        async UniTask Merge(IOEntry entry, RuniPath destinationPath)
        {
            // 이동하면서 원본 폴더가 바뀌므로, 자식 목록을 먼저 모두 가져옵니다.
            List<IOEntry> children = await sourceProvider.EnumerateEntries(entry.path, false).ToListAsync();
            await TransferAll(children, destinationPath);
        }

        // 다른 프로바이더로는 프로바이더의 폴더 복사를 쓸 수 없으므로, 원본을 읽어 대상에 새로 씁니다.
        async UniTask CopyDirectoryAcrossProviders(RuniPath sourcePath, RuniPath destinationPath)
        {
            await destinationProvider.CreateDirectory(destinationPath);
            await foreach (IOEntry child in sourceProvider.EnumerateEntries(sourcePath, false))
            {
                RuniPath childDestinationPath = destinationPath / child.path.GetFileName();
                if (child.isDirectory)
                    await CopyDirectoryAcrossProviders(child.path, childDestinationPath);
                else
                    await CopyFile(child.path, childDestinationPath, false);
            }
        }

        // 덮어쓰기 여부와 관계없이 항상 대상 폴더의 임시 파일에 끝까지 쓴 뒤 최종 이름으로 바꿔 끼웁니다.
        // 서로 다른 프로바이더가 사실 같은 파일을 가리킬 수 있어, 대상을 바로 열면 원본을 읽기도 전에 그 내용이 비워질 수 있기 때문입니다.
        // 또한 도중에 실패해도 반쯤 쓴 파일이 최종 이름으로 남지 않습니다.
        async UniTask CopyFile(RuniPath sourcePath, RuniPath destinationPath, bool overwrite)
        {
            RuniPath temporaryPath = await GetAvailablePath(destinationPath.GetParentPath() / $".{destinationPath.GetFileName()}.runios-temp", false);
            try
            {
                if (sameProvider)
                    await destinationProvider.CopyFile(sourcePath, temporaryPath, false);
                else
                {
                    await using Stream source = await sourceProvider.OpenRead(sourcePath);
                    await using Stream destination = await destinationProvider.CreateFile(temporaryPath);
                    await source.CopyToAsync(destination);
                }

                await destinationProvider.MoveFile(temporaryPath, destinationPath, overwrite);
            }
            catch
            {
                // 다 쓰지 못했거나 바꿔 끼우지 못한 임시 파일을 남기지 않습니다.
                if (await destinationProvider.FileExists(temporaryPath))
                    await destinationProvider.DeleteFile(temporaryPath);

                throw;
            }
        }

        // 같은 이름이 있으면 name (2), name (3)…처럼 번호를 붙입니다. 파일은 확장자 앞에 붙이며, .bashrc처럼 점으로 시작하는 이름은 확장자가 없는 것으로 봅니다.
        async UniTask<RuniPath> GetAvailablePath(RuniPath path, bool isDirectory)
        {
            string fileName = path.GetFileName();
            int extensionIndex = isDirectory ? -1 : fileName.LastIndexOf(FileExtension.extensionSeparatorChar);

            string name = extensionIndex > 0 ? fileName[..extensionIndex] : fileName;
            string extension = extensionIndex > 0 ? fileName[extensionIndex..] : string.Empty;

            RuniPath directoryPath = path.GetParentPath();
            RuniPath availablePath = path;
            for (int number = 2; await destinationProvider.GetEntry(availablePath) != null; number++)
                availablePath = directoryPath / $"{name} ({number}){extension}";

            return availablePath;
        }

        // 취소하거나 대화 상자가 닫히면 null입니다.
        async UniTask<Resolution?> Resolve(IOEntry entry, bool canOverwrite, RuniPath renamedPath)
        {
            Resolution? resolutionForAll = entry.isDirectory ? directoryResolution : fileResolution;
            if (resolutionForAll is { } resolution && (resolution != Resolution.Overwrite || canOverwrite))
                return resolution;

            string kind = entry.isDirectory ? "directory" : "file";
            RuniDialog dialog = new(
                GetText($"runios-editor:io_transfer.conflict.{kind}_title"),
                string.Format(GetText("runios-editor:io_transfer.conflict.message"), entry.metaData.name, renamedPath.GetFileName()));

            Toggle applyToAll = new() { text = GetText("runios-editor:io_transfer.conflict.apply_to_all") };
            dialog.content.Add(applyToAll);

            Button overwrite = dialog.AddButton(GetText(entry.isDirectory ? "runios-editor:io_transfer.conflict.merge" : "runios-editor:io_transfer.conflict.overwrite"));
            overwrite.SetEnabled(canOverwrite);

            Button rename = dialog.AddButton(GetText("runios-editor:io_transfer.conflict.rename"));
            Button skip = dialog.AddButton(GetText("runios-editor:io_transfer.conflict.skip"));
            dialog.AddButton(GetText("runios-editor:io_transfer.conflict.cancel"));

            // 되돌릴 수 없는 덮어쓰기가 Enter 한 번에 실행되지 않도록 이름 바꾸기에 포커스를 둡니다.
            dialog.defaultButton = rename;

            Button? result = await dialog.ShowAsync(dialogTarget);
            Resolution? chosen = null;
            if (result == overwrite)
                chosen = Resolution.Overwrite;
            else if (result == rename)
                chosen = Resolution.Rename;
            else if (result == skip)
                chosen = Resolution.Skip;

            if (chosen != null && applyToAll.value)
            {
                if (entry.isDirectory)
                    directoryResolution = chosen;
                else
                    fileResolution = chosen;
            }

            return chosen;
        }

        static string GetText(string key) => LocalizationUtility.GetText(key) ?? key;
    }
}
