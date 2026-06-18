#nullable enable
namespace RuniOS.Installer.Screens
{
    sealed class TMPSettingScreen : IInstallerScreen
    {
        [InitializeOnLoadMethod]
        static void Initialize() => InstallerWindow.RegisterScreen(new TMPSettingScreen());



        public InstallerWindow? mainWindow { get; set; }
        public Vector2? windowSize => new Vector2(584, 348);

        public string label => InstallerWindow.TryGetText("installer.tmp_setting.label");
        public bool headDisable => false;

        public int sort => 2;



#if ENABLE_TEXT_MESH_PRO
        readonly TMPro.TMP_PackageResourceImporter importer = new TMPro.TMP_PackageResourceImporter();
        public void DrawGUI(Rect position)
        {
            GUILayout.Label(InstallerWindow.TryGetText("installer.tmp_setting.info"));
            importer.OnGUI();
        }
#else
        public void DrawGUI(Rect position) => EditorGUILayout.HelpBox(InstallerWindow.TryGetText("installer.tmp_setting.warning"), MessageType.Error);
#endif
    }
}
