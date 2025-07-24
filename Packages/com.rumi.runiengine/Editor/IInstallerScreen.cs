#nullable enable
using UnityEngine;

namespace RuniEngine.Installer
{
    interface IInstallerScreen
    {
        InstallerWindow? mainWindow { get; set; }
        Vector2? windowSize { get; }

        string label { get; }
        bool headDisable { get; }

        int sort { get; }


        void DrawGUI();
    }
}
