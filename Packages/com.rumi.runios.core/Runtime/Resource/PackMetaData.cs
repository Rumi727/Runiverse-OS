#nullable enable
namespace RuniOS.Resource
{
    public struct PackMetaData
    {
        public PackMetaData(string name)
        {
            this.name = name;
            version = Version.all;

            targetVersion = Version.all;
            targetRuniOSVersion = Version.all;
        }

        public PackMetaData(string name, Version version)
        {
            this.name = name;
            this.version = version;

            targetVersion = Version.all;
            targetRuniOSVersion = Version.all;
        }

        public PackMetaData(string name, Version version, VersionRange targetVersion, VersionRange targetRuniOSVersion)
        {
            this.name = name;
            this.version = version;

            this.targetVersion = targetVersion;
            this.targetRuniOSVersion = targetRuniOSVersion;
        }

        [FieldName("gui.name")] public string name { get; set; }
        [FieldName("gui.version")] public Version version { get; set; }

        public VersionRange targetVersion { get; set; }
        public VersionRange targetRuniOSVersion { get; set; }
    }
}