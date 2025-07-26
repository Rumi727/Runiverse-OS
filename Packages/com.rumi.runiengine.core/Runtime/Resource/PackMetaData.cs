#nullable enable
namespace RuniEngine.Resource
{
    public readonly struct PackMetaData
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

        public string name { get; }
        public Version version { get; }

        public VersionRange targetVersion { get; }
        public VersionRange targetRuniOSVersion { get; }
    }
}
