#nullable enable
using RuniOS.Spans;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

// ReSharper disable StringLiteralTypo
namespace RuniOS.IO.Locations.Linux
{
    static class LinuxMountParser
    {
        public static LinuxMount Parse(ReadOnlySpan<char> line)
        {
            int mountId = 0;
            int parentMountId = 0;

            uint majorDeviceNumber = 0;
            uint minorDeviceNumber = 0;

            string? root = null;
            string? mountPoint = null;
            string? mountOptions = null;

            ImmutableArray<string>.Builder optionalFields = ImmutableArray.CreateBuilder<string>();

            string? fileSystemType = null;
            string? source = null;
            string? superOptions = null;

            int prefixFieldIndex = 0;
            int suffixFieldIndex = 0;

            bool separatorFound = false;
            foreach (ReadOnlySpan<char> item in line.Split(' '))
            {
                if (!separatorFound)
                {
                    if (prefixFieldIndex < 6)
                    {
                        switch (prefixFieldIndex)
                        {
                            case 0:
                            {
                                mountId = ParseInt32(item);
                                break;
                            }
                            case 1:
                            {
                                parentMountId = ParseInt32(item);
                                break;
                            }
                            case 2:
                            {
                                ParseDeviceNumber(item, out majorDeviceNumber, out minorDeviceNumber);
                                break;
                            }
                            case 3:
                            {
                                root = DecodeField(item);
                                break;
                            }
                            case 4:
                            {
                                mountPoint = DecodeField(item);
                                break;
                            }
                            case 5:
                            {
                                mountOptions = item.ToString();
                                break;
                            }
                        }

                        prefixFieldIndex++;
                        continue;
                    }

                    if (item is "-")
                    {
                        separatorFound = true;
                        continue;
                    }

                    optionalFields.Add(item.ToString());
                    continue;
                }

                switch (suffixFieldIndex)
                {
                    case 0:
                    {
                        fileSystemType = DecodeField(item);
                        break;
                    }
                    case 1:
                    {
                        source = DecodeField(item);
                        break;
                    }
                    case 2:
                    {
                        superOptions = item.ToString();
                        break;
                    }
                    default:
                    {
                        throw new FormatException(
                            "Unexpected field after super options in /proc/self/mountinfo.");
                    }
                }

                suffixFieldIndex++;
            }

            if (prefixFieldIndex != 6)
                throw new FormatException("Incomplete prefix in /proc/self/mountinfo.");

            if (!separatorFound)
                throw new FormatException("Missing '-' separator in /proc/self/mountinfo.");

            if (suffixFieldIndex != 3)
                throw new FormatException("Incomplete suffix in /proc/self/mountinfo.");

            if (root == null || mountPoint == null || mountOptions == null || fileSystemType == null || source == null || superOptions == null)
                throw new FormatException("Missing required field in /proc/self/mountinfo.");

            return new LinuxMount
            (
                mountId,
                parentMountId,
                majorDeviceNumber,
                minorDeviceNumber,
                root,
                (PhysicalPath)mountPoint,
                mountOptions,
                optionalFields.ToImmutable(),
                fileSystemType,
                source,
                superOptions
            );
        }

        static void ParseDeviceNumber(ReadOnlySpan<char> value, out uint major, out uint minor)
        {
            int separatorIndex = value.IndexOf(':');
            if (separatorIndex <= 0 || separatorIndex >= value.Length - 1)
                throw new FormatException($"Invalid device number '{value.ToString()}'.");

            major = ParseUInt32(value[..separatorIndex]);
            minor = ParseUInt32(value[(separatorIndex + 1)..]);
        }

        static int ParseInt32(ReadOnlySpan<char> value)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result))
                throw new FormatException($"Invalid integer '{value.ToString()}'.");

            return result;
        }

        static uint ParseUInt32(ReadOnlySpan<char> value)
        {
            if (!uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out uint result))
                throw new FormatException($"Invalid unsigned integer '{value.ToString()}'.");

            return result;
        }

        static string DecodeField(ReadOnlySpan<char> value)
        {
            int escapeIndex = value.IndexOf('\\');
            if (escapeIndex < 0)
                return value.ToString();

            StringBuilder builder = StringBuilderCache.Acquire(value.Length);
            builder.Append(value[..escapeIndex]);

            for (int i = escapeIndex; i < value.Length; i++)
            {
                if (value[i] == '\\' && i + 3 < value.Length && IsOctal(value[i + 1]) && IsOctal(value[i + 2]) && IsOctal(value[i + 3]))
                {
                    int decoded = ((value[i + 1] - '0') << 6) | ((value[i + 2] - '0') << 3) | (value[i + 3] - '0');
                    builder.Append((char)decoded);

                    i += 3;
                    continue;
                }

                builder.Append(value[i]);
            }

            return StringBuilderCache.GetStringAndRelease(builder);
        }

        static bool IsOctal(char value) => value is >= '0' and <= '7';
    }
}