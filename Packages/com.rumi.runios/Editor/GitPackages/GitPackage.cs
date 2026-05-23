namespace RuniOS.Installer.GitPackages
{
    //[CreateAssetMenu(fileName = "RuniPackage", menuName = "Scriptable Objects/RuniPackage")]
    class GitPackage : ScriptableObject
    {
        public GitPackage?[] packages = [];
        public string?[] dependencies = [];
        
        public string id = string.Empty;
        public string gitUrl = string.Empty;
        
        public string label = string.Empty;

        public string oneLineDescription = string.Empty;
        public string description = string.Empty;
    }
}