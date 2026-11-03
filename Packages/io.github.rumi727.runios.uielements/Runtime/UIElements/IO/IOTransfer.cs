#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using System.IO;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// IO 항목을 다른 폴더로 옮기거나 복사하며, 같은 이름이 이미 있으면 대화 상자로 어떻게 할지 묻습니다.
    /// </summary>
    /// <remarks>
    /// 파일은 덮어쓰기·이름 바꾸기·건너뛰기를, 폴더는 합치기·이름 바꾸기·건너뛰기를 고를 수 있으며, 취소하면 남은 항목을 처리하지 않습니다.
    /// 폴더를 합치면 그 안의 항목도 같은 방식으로 처리합니다.
    /// <br/>
    /// 대상 프로바이더가 <see cref="IIOProvider.TryGetPath"/>로 원본 항목을 가리킬 수 있으면, 프로바이더가 달라도 대상 프로바이더 안의 이동·복사로 처리합니다.
    /// 그렇지 않으면 원본을 읽어 대상에 새로 쓰며, 이동은 할 수 없습니다.
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

            sameProvider = sourceProvider.IsSameTarget(destinationProvider);
        }

        /// <summary>
        /// <paramref name="entries"/>를 <paramref name="directory"/> 안으로 복사할 수 있는지 확인합니다. 폴더를 자기 자신이나 그 안으로는 복사할 수 없습니다.
        /// </summary>
        /// <param name="sourceProvider"><paramref name="entries"/>가 들어 있는 프로바이더입니다.</param>
        public static bool CanCopy(IIOProvider sourceProvider, IEnumerable<IOEntry> entries, IONode directory)
        {
            if (directory.provider is not IWritableIOProvider)
                return false;

            // 대상 폴더를 원본 프로바이더 기준으로 바꿀 수 없으면, 원본 폴더 안인지 알 수 없으므로 안이 아닌 것으로 봅니다.
            if (!sourceProvider.TryGetPath(directory.provider, directory.path, out RuniPath directoryPath))
                return true;

            return !entries.Any(x => x.isDirectory && IsSameOrDescendant(sourceProvider, directoryPath, x.path));
        }

        /// <summary>
        /// <paramref name="entries"/>를 <paramref name="directory"/> 안으로 옮길 수 있는지 확인합니다.
        /// 대상 프로바이더가 원본 항목을 모두 가리킬 수 있어야 하며, 이미 그 폴더에 있는 항목만 옮기면 바뀌는 것이 없으므로 옮길 수 없습니다.
        /// </summary>
        /// <param name="sourceProvider"><paramref name="entries"/>가 들어 있는 프로바이더입니다.</param>
        public static bool CanMove(IIOProvider sourceProvider, IEnumerable<IOEntry> entries, IONode directory) =>
            sourceProvider is IWritableIOProvider
            && CanCopy(sourceProvider, entries, directory)
            && TryGetEntriesIn(directory.provider, sourceProvider, entries, out IOEntry[] destinationEntries)
            && destinationEntries.Any(x => !PathEquals(directory.provider, x.path.GetParentPath(), directory.path));

        /// <summary>
        /// <paramref name="entries"/>를 <paramref name="directory"/> 안으로 옮기거나 복사합니다. 항목마다 실패하면 로그를 남기고 다음 항목을 처리합니다.
        /// </summary>
        /// <param name="sourceProvider"><paramref name="entries"/>가 들어 있는 프로바이더입니다.</param>
        /// <param name="dialogTarget">이름이 겹칠 때 띄우는 대화 상자를 표시할 패널을 정하는 요소입니다.</param>
        public static UniTask Run(IIOProvider sourceProvider, IEnumerable<IOEntry> entries, IOWriteNode directory, IODropOperation operation, VisualElement dialogTarget)
        {
            // 대상 프로바이더가 원본 항목을 모두 가리킬 수 있으면, 원본도 대상 프로바이더로 다뤄 같은 프로바이더 안의 이동·복사로 처리합니다.
            if (TryGetEntriesIn(directory.provider, sourceProvider, entries, out IOEntry[] destinationEntries))
                return new IOTransfer(directory.provider, directory.provider, operation, dialogTarget).TransferAll(destinationEntries, directory.path);

            return new IOTransfer(sourceProvider, directory.provider, operation, dialogTarget).TransferAll(entries, directory.path);
        }

        // 원본 항목의 경로를 모두 provider 기준으로 바꿉니다.
        static bool TryGetEntriesIn(IIOProvider provider, IIOProvider sourceProvider, IEnumerable<IOEntry> entries, out IOEntry[] result)
        {
            List<IOEntry> converted = [];
            foreach (IOEntry entry in entries)
            {
                if (!provider.TryGetPath(sourceProvider, entry.path, out RuniPath path))
                {
                    result = [];
                    return false;
                }

                converted.Add(entry with { path = path });
            }

            result = converted.ToArray();
            return true;
        }

        static StringComparison GetComparison(IIOProvider provider, RuniPath path) =>
            provider.IsCaseSensitive(path) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        static bool PathEquals(IIOProvider provider, RuniPath x, RuniPath y) =>
            string.Equals(x.value, y.value, GetComparison(provider, x.GetParentPath()));

        static bool IsSameOrDescendant(IIOProvider provider, RuniPath path, RuniPath ancestorPath) =>
            PathEquals(provider, path, ancestorPath)
            || path.value.StartsWith(ancestorPath.value + RuniPath.directorySeparatorChar, GetComparison(provider, ancestorPath));

        async UniTask TransferAll(IEnumerable<IOEntry> entries, RuniPath directoryPath)
        {
            foreach (IOEntry entry in entries)
            {
                if (cancelled)
                    return;

                // 이미 그 폴더에 있는 항목은 옮겨도 바뀌지 않으므로 건너뜁니다.
                if (operation == IODropOperation.Move && PathEquals(destinationProvider, entry.path.GetParentPath(), directoryPath))
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
            bool isSelf = sourceProvider.TryGetPath(destinationProvider, destinationPath, out RuniPath sourceDestinationPath) && PathEquals(sourceProvider, sourceDestinationPath, entry.path);
            bool canOverwrite = entry.isDirectory == existing.isDirectory && !isSelf;
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
            if (!sameProvider)
                await CopyAcrossProviders(entry, destinationPath, overwrite);
            else if (operation == IODropOperation.Move)
                await (entry.isDirectory ? destinationProvider.MoveDirectory(entry.path, destinationPath) : destinationProvider.MoveFile(entry.path, destinationPath, overwrite));
            else
                await (entry.isDirectory ? destinationProvider.CopyDirectory(entry.path, destinationPath) : destinationProvider.CopyFile(entry.path, destinationPath, overwrite));
        }

        // 이미 있는 폴더 안으로 자식을 하나씩 옮기거나 복사합니다.
        // 이동해도 원본 폴더는 지우지 않습니다. 폴더 삭제는 안쪽까지 지우는데, 열거에서 빠진 항목(접근할 수 없는 항목 등)이 있으면 보지 못한 채 함께 지워지기 때문입니다.
        async UniTask Merge(IOEntry entry, RuniPath destinationPath)
        {
            // 이동하면서 원본 폴더가 바뀌므로, 자식 목록을 먼저 모두 가져옵니다.
            List<IOEntry> children = await sourceProvider.EnumerateEntries(entry.path, false).ToListAsync();
            await TransferAll(children, destinationPath);
        }

        // 다른 프로바이더로는 프로바이더의 이동·복사를 쓸 수 없으므로, 원본을 읽어 대상에 새로 씁니다.
        async UniTask CopyAcrossProviders(IOEntry entry, RuniPath destinationPath, bool overwrite)
        {
            if (entry.isDirectory)
            {
                await destinationProvider.CreateDirectory(destinationPath);
                await foreach (IOEntry child in sourceProvider.EnumerateEntries(entry.path, false))
                    await CopyAcrossProviders(child, destinationPath / child.path.GetFileName(), false);

                return;
            }

            if (!overwrite)
            {
                await CopyFileAcrossProviders(entry.path, destinationPath);
                return;
            }

            // 덮어쓸 때는 원본과 대상이 사실 같은 파일일 수 있습니다. 두 프로바이더가 같은 디스크를 가리키지만 TryGetPath로 알아내지 못한 경우입니다.
            // 대상을 CreateFile로 바로 열면 원본을 읽기도 전에 그 내용이 비워지므로, 같은 폴더의 임시 파일에 끝까지 쓴 뒤 덮어쓰는 이동으로 바꿔 끼웁니다.
            // 같은 파일이었더라도 원본을 다 읽은 뒤에 바뀌므로 내용이 사라지지 않습니다.
            RuniPath temporaryPath = await GetAvailablePath(destinationPath.GetParentPath() / $".{destinationPath.GetFileName()}.runios-temp", false);
            try
            {
                await CopyFileAcrossProviders(entry.path, temporaryPath);
                await destinationProvider.MoveFile(temporaryPath, destinationPath, true);
            }
            catch
            {
                // 다 쓰지 못했거나 바꿔 끼우지 못한 임시 파일을 남기지 않습니다.
                if (await destinationProvider.FileExists(temporaryPath))
                    await destinationProvider.DeleteFile(temporaryPath);

                throw;
            }
        }

        async UniTask CopyFileAcrossProviders(RuniPath sourcePath, RuniPath destinationPath)
        {
            await using Stream source = await sourceProvider.OpenRead(sourcePath);
            await using Stream destination = await destinationProvider.CreateFile(destinationPath);
            await source.CopyToAsync(destination);
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
