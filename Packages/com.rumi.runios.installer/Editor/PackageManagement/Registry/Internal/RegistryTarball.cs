#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace RuniOS.PackageManagement.Unity.Editor.Registry.Internal
{
    internal static class RegistryTarball
    {
        internal static void Verify(byte[] data, string integrity)
        {
            string[] tokens = integrity.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] supported = { "sha512", "sha384", "sha256", "sha1" };
            string? selected = supported.SelectMany(algorithm => tokens.Where(x => x.StartsWith(algorithm + "-", StringComparison.Ordinal))).FirstOrDefault();
            if (selected is null) throw new IOException("No supported tarball integrity algorithm is available.");
            int separator = selected.IndexOf('-');
            string algorithmName = selected.Substring(0, separator);
            byte[] expected = Convert.FromBase64String(selected.Substring(separator + 1).Split('?')[0]);
            using HashAlgorithm algorithm = algorithmName == "sha512" ? SHA512.Create() : algorithmName == "sha384" ? SHA384.Create() :
                algorithmName == "sha256" ? SHA256.Create() : SHA1.Create();
            byte[] actual = algorithm.ComputeHash(data);
            if (!actual.SequenceEqual(expected)) throw new IOException("Registry tarball integrity verification failed.");
        }
        internal static (string packageJson, string? sidecarJson) Read(byte[] bytes, CancellationToken cancellationToken)
        {
            using MemoryStream input = new(bytes, false);
            using GZipStream gzip = new(input, CompressionMode.Decompress);
            byte[] header = new byte[512];
            long expanded = 0;
            string? package = null, sidecar = null;
            void Exact(byte[] buffer, int count)
            {
                int offset = 0;
                while (offset < count)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int read = gzip.Read(buffer, offset, count - offset);
                    if (read == 0) throw new IOException("The registry tarball is truncated.");
                    offset += read; expanded += read;
                    if (expanded > 512L * 1024 * 1024) throw new IOException("The expanded registry tarball exceeds the metadata retrieval limit.");
                }
            }
            string Text(int start, int count) => System.Text.Encoding.UTF8.GetString(header, start, count).TrimEnd('\0');
            while (true)
            {
                Exact(header, 512);
                if (header.All(x => x == 0)) break;
                long expectedChecksum = Convert.ToInt64(Text(148, 8).Trim('\0', ' '), 8);
                long checksum = header.Select((value, index) => index >= 148 && index < 156 ? 32 : (int)value).Sum();
                if (checksum != expectedChecksum) throw new IOException("The registry tar header checksum is invalid.");
                string name = Text(0, 100), prefix = Text(345, 155);
                if (prefix.Length > 0) name = prefix + "/" + name;
                long size = Convert.ToInt64(Text(124, 12).Trim('\0', ' '), 8);
                if (size < 0 || size > 512L * 1024 * 1024) throw new IOException("The registry tar entry is too large.");
                bool metadata = name == "package/package.json" || name == "package/runios.package.json";
                if (metadata && (header[156] != 0 && header[156] != (byte)'0')) throw new IOException("Package metadata must be a regular tar entry.");
                if (metadata && size > 4 * 1024 * 1024) throw new IOException("The package metadata exceeds the retrieval limit.");
                byte[] buffer = new byte[8192];
                using MemoryStream content = new();
                long remaining = size;
                while (remaining > 0)
                {
                    int count = (int)Math.Min(remaining, buffer.Length); Exact(buffer, count);
                    if (metadata) content.Write(buffer, 0, count);
                    remaining -= count;
                }
                int padding = (int)((512 - size % 512) % 512);
                if (padding > 0) Exact(buffer, padding);
                if (metadata)
                {
                    string json = new System.Text.UTF8Encoding(false, true).GetString(content.ToArray());
                    if (name == "package/package.json")
                    {
                        if (package is not null) throw new IOException("Duplicate package.json in the tarball.");
                        package = json;
                    }
                    else
                    {
                        if (sidecar is not null) throw new IOException("Duplicate runios.package.json in the tarball.");
                        sidecar = json;
                    }
                }
            }
            return (package ?? throw new IOException("The registry tarball has no package/package.json."), sidecar);
        }
    }
}
