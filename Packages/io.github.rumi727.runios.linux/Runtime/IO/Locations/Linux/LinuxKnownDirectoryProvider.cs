#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Text;

// ReSharper disable StringLiteralTypo
namespace RuniOS.IO.Locations.Linux
{
    public sealed partial class LinuxKnownDirectoryProvider : IKnownDirectoryProvider
    {
        LinuxKnownDirectoryProvider() { }

        public static LinuxKnownDirectoryProvider instance { get; } = new();

        public static IEnumerable<KnownDirectory> EnumerateDirectories()
        {
            string? home = GetHomeDirectory();
            if (home == null)
                yield break;

            yield return new KnownDirectory(KnownDirectoryType.home, (PhysicalPath)home);

            Dictionary<KnownDirectoryType, PhysicalPath> directories = ReadUserDirectories(home);
            foreach ((KnownDirectoryType type, PhysicalPath path) in directories)
                yield return new KnownDirectory(type, path);
        }

        IUniTaskAsyncEnumerable<KnownDirectory> IKnownDirectoryProvider.EnumerateDirectories() => EnumerateDirectories().ToUniTaskAsyncEnumerable();

        static Dictionary<KnownDirectoryType, PhysicalPath> ReadUserDirectories(string home)
        {
            Dictionary<KnownDirectoryType, PhysicalPath> result = new();

            string? configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(configHome) || !Path.IsPathRooted(configHome))
                configHome = Path.Combine(home, ".config");

            string filePath = Path.Combine(configHome, "user-dirs.dirs");
            try
            {
                foreach (string line in File.ReadLines(filePath))
                {
                    if (!TryParseUserDirectory(line, home, out KnownDirectoryType type, out PhysicalPath path, out bool isDisabled))
                        continue;

                    if (isDisabled)
                        result.Remove(type);
                    else
                        result[type] = path;
                }
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }

            return result;
        }

        static bool TryParseUserDirectory(string line, string home, out KnownDirectoryType type, out PhysicalPath path, out bool isDisabled)
        {
            type = default;
            path = default;
            isDisabled = false;

            ReadOnlySpan<char> span = line.AsSpan().Trim();
            if (span.IsEmpty || span[0] == '#')
                return false;

            int equalsIndex = span.IndexOf('=');
            if (equalsIndex < 0)
                return false;

            ReadOnlySpan<char> name = span[..equalsIndex].Trim();
            ReadOnlySpan<char> expression = span[(equalsIndex + 1)..].Trim();

            const string prefix = "XDG_";
            const string suffix = "_DIR";

            if (!name.StartsWith(prefix, StringComparison.Ordinal) || !name.EndsWith(suffix, StringComparison.Ordinal))
                return false;

            ReadOnlySpan<char> xdgType = name[prefix.Length..^suffix.Length];
            if (xdgType.IsEmpty)
                return false;

            if (!TryParseQuotedValue(expression, out string value))
                return false;

            type = GetKnownDirectoryType(xdgType);

            // XDG에서 $HOME 자체는 해당 user directory를 disabled한 것.
            if (value == "$HOME")
            {
                isDisabled = true;
                return true;
            }

            string resolvedPath;

            const string homePrefix = "$HOME/";

            if (value.StartsWith(homePrefix, StringComparison.Ordinal))
                resolvedPath = Path.Combine(home, value[homePrefix.Length..]);
            else if (Path.IsPathRooted(value))
                resolvedPath = value;
            else
                return false;

            try
            {
                PhysicalPath physicalPath = (PhysicalPath)resolvedPath;

                // "$HOME" 대신 절대경로로 home 자체를 적은 경우도 동일하게 처리.
                if (physicalPath == (PhysicalPath)home)
                {
                    isDisabled = true;
                    return true;
                }

                path = physicalPath;
                return true;
            }
            catch (ArgumentException) { return false; }
            catch (IOException) { return false; }
            catch (NotSupportedException) { return false; }
        }

        static bool TryParseQuotedValue(ReadOnlySpan<char> expression, out string value)
        {
            value = string.Empty;
            if (expression.IsEmpty || expression[0] != '"')
                return false;

            StringBuilder builder = new(expression.Length);
            for (int i = 1; i < expression.Length; i++)
            {
                char character = expression[i];
                switch (character)
                {
                    case '"':
                    {
                        ReadOnlySpan<char> remaining = expression[(i + 1)..].Trim();
                        if (!remaining.IsEmpty && remaining[0] != '#')
                            return false;

                        value = builder.ToString();
                        return true;
                    }
                    // user-dirs.dirs의 shell escaping.
                    case '\\' when i + 1 < expression.Length:
                    {
                        builder.Append(expression[++i]);
                        continue;
                    }
                    default:
                    {
                        builder.Append(character);
                        break;
                    }
                }

            }

            return false;
        }

        static KnownDirectoryType GetKnownDirectoryType(ReadOnlySpan<char> value)
        {
            switch (value)
            {
                case "DESKTOP":
                    return KnownDirectoryType.desktop;
                case "DOWNLOAD":
                    return KnownDirectoryType.downloads;
                case "DOCUMENTS":
                    return KnownDirectoryType.documents;
                case "MUSIC":
                    return KnownDirectoryType.music;
                case "PICTURES":
                    return KnownDirectoryType.pictures;
                case "VIDEOS":
                    return KnownDirectoryType.videos;
                case "TEMPLATES":
                    return KnownDirectoryType.templates;
                case "PUBLICSHARE":
                    return KnownDirectoryType.publicShare;
            }

            StringBuilder builder = new(value.Length);
            foreach (char character in value)
                builder.Append(character == '_' ? '-' : char.ToLowerInvariant(character));

            return new KnownDirectoryType(builder.ToString());
        }

        static string? GetHomeDirectory()
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home) && Path.IsPathRooted(home))
                return Path.GetFullPath(home);

            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (!string.IsNullOrEmpty(home) && Path.IsPathRooted(home))
                return Path.GetFullPath(home);

            return null;
        }

#if UNITY_EDITOR_LINUX || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => IKnownDirectoryProvider.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => IKnownDirectoryProvider.Unregister(instance);
#endif
    }
}