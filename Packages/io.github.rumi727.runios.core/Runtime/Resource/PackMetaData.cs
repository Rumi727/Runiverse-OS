#nullable enable
namespace RuniOS.Resource
{
    public struct PackMetaData(string name, Version version, VersionRange targetVersion, VersionRange targetRuniOSVersion)
    {
        public PackMetaData(string name) : this(name, Version.all, Version.all, Version.all) { }

        public PackMetaData(string name, Version version) : this(name, version, Version.all, Version.all) { }

        [FieldName("runios-editor:gui.name")] public string name { get; set; } = name;
        [FieldName("runios-editor:gui.version")] public Version version { get; set; } = version;

        public VersionRange targetVersion { get; set; } = targetVersion;
        public VersionRange targetRuniOSVersion { get; set; } = targetRuniOSVersion;
    }
}