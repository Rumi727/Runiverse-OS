#nullable enable
namespace RuniOS.IO.Locations
{
    public readonly record struct KnownDirectoryType(string value)
    {
        // Common / XDG
        public static readonly KnownDirectoryType home = "home";

        public static readonly KnownDirectoryType desktop = "desktop";
        public static readonly KnownDirectoryType downloads = "downloads";
        public static readonly KnownDirectoryType documents = "documents";

        public static readonly KnownDirectoryType music = "music";
        public static readonly KnownDirectoryType pictures = "pictures";
        public static readonly KnownDirectoryType videos = "videos";

        public static readonly KnownDirectoryType templates = "templates";
        public static readonly KnownDirectoryType publicShare = "public-share";

        // Windows
        public static readonly KnownDirectoryType contacts = "contacts";
        public static readonly KnownDirectoryType favorites = "favorites";
        public static readonly KnownDirectoryType links = "links";

        public static readonly KnownDirectoryType savedGames = "saved-games";
        public static readonly KnownDirectoryType searches = "searches";

        public override string ToString() => value;

        public static implicit operator string(KnownDirectoryType type) => type.ToString();
        public static implicit operator KnownDirectoryType(string type) => new KnownDirectoryType(type);
    }
}