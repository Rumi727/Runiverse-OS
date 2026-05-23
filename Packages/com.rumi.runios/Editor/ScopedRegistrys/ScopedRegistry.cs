namespace RuniOS.Installer.ScopedRegistrys
{
    //[CreateAssetMenu(fileName = "ScopedRegistry", menuName = "Scriptable Objects/ScopedRegistry")]
    class ScopedRegistry : ScriptableObject
    {
        public ScopedRegistry[] scopedRegistries = [];
        
        public new string? name;
        public string? url;
        
        public string[]? scopes;
    }
}
