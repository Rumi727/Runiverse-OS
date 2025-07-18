#nullable enable
namespace RuniEngine.Resource
{
    public readonly struct ResourcePackInfo
    {
        public ResourcePackInfo(string name)
        {
            this.name = name;
            version = Version.all;

            targetVersion = Version.all;
            targetRuniOSVersion = Version.all;
        }

        public ResourcePackInfo(string name, Version version)
        {
            this.name = name;
            this.version = version;

            targetVersion = Version.all;
            targetRuniOSVersion = Version.all;
        }

        public ResourcePackInfo(string name, Version version, VersionRange targetVersion, VersionRange targetRuniOSVersion)
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
