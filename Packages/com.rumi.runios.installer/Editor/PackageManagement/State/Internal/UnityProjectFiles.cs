#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using RuniOS.PackageManagement.Unity.Editor.Metadata;
using RuniOS.PackageManagement.Unity.Editor.Registry;
using RuniOS.PackageManagement.Unity.Editor.Internal.Json;

namespace RuniOS.PackageManagement.Unity.Editor.State.Internal
{
    internal static class UnityProjectFiles
    {
        internal const string manifestResource = "unity:manifest";
        internal const string lockResource = "unity:packages-lock";
        internal const string ownershipResource = "unity:installer-state";
        internal static string PathFor(UnityProjectTarget target, string resource) => Path.Combine(target.projectPath, "Packages", resource switch
        {
            manifestResource => "manifest.json", lockResource => "packages-lock.json", ownershipResource => "runios.installer-state.json",
            _ => throw new ArgumentException("An unknown Unity package resource was requested.", nameof(resource))
        });
        internal static string Fingerprint(string path)
        {
            if (!File.Exists(path)) return "missing";
            using var algorithm = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        internal static IReadOnlyDictionary<string, string> Fingerprints(UnityProjectTarget target) =>
            new ReadOnlyDictionary<string, string>(new[] { manifestResource, lockResource, ownershipResource }.ToDictionary(x => x, x => Fingerprint(PathFor(target, x)), StringComparer.Ordinal));
        internal static void Check(UnityProjectTarget target, IReadOnlyDictionary<string, string> expected)
        {
            foreach (string resource in new[] { manifestResource, lockResource, ownershipResource })
                if (!expected.TryGetValue(resource, out string? value) || value != Fingerprint(PathFor(target, resource)))
                    throw new IOException($"Resource '{resource}' changed after observation. Create a new preview.");
        }
        internal static void Write(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new IOException("A parent directory is required."));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, text, new System.Text.UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        internal static UpmSourceConfiguration Sources(Dictionary<string, object?> manifest)
        {
            IEnumerable<ScopedRegistryDefinition> registries = manifest.TryGetValue("scopedRegistries", out object? value) ?
                JsonData.Array(value).Select(x =>
                {
                    Dictionary<string, object?> registry = JsonData.Object(x);
                    return new ScopedRegistryDefinition(JsonData.Required(registry, "name"), JsonData.Required(registry, "url"),
                        JsonData.Array(registry.TryGetValue("scopes", out object? scopes) ? scopes : null).Select(JsonData.String));
                }) : Array.Empty<ScopedRegistryDefinition>();
            return new UpmSourceConfiguration(registries, JsonData.Optional(manifest, "registry") ?? "https://packages.unity.com");
        }
        internal static Dictionary<string, object?> Ownership(string? json)
        {
            if (json is null) return new(StringComparer.Ordinal) { ["schemaVersion"] = new JsonNumber("1"), ["packages"] = new Dictionary<string, object?>(StringComparer.Ordinal) };
            var root = JsonData.Parse(json);
            if (!root.TryGetValue("schemaVersion", out object? version) || version is not JsonNumber number || number.text != "1")
                throw new FormatException("Unsupported Installer ownership state schema.");
            JsonData.Map(root, "packages");
            return root;
        }
    }
}
