#nullable enable
namespace RuniOS.Editor.Windows
{
    public interface IControlPanel
    {
        string label { get; }
        
        int sort { get; }

        bool allowUpdate { get; }
        bool allowUpdateInEditor { get; }

        void OnGUI();
    }
}