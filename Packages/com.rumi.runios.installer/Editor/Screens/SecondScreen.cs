#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.Editor.Installer.Screens
{
    sealed class SecondScreen : SetupScreen
    {
        readonly Label label;
        readonly Toggle option;

        public SecondScreen() : base("installer.setup.second.title", 100)
        {
            contentContainer.AddToClassList("runios-setup__placeholder");
            label = new Label();
            option = new Toggle();
            Add(label);
            Add(option);
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
            Add(new TextField());
        }

        protected internal override void OnLanguageChanged()
        {
            label.text = InstallerLocalization.GetText("installer.setup.placeholder.content").Replace("{screen}", title);
            option.label = InstallerLocalization.GetText("installer.setup.placeholder.option");

            foreach (VisualElement element in contentContainer.Children())
            {
                if (element is TextField textField)
                    textField.label = InstallerLocalization.GetText("installer.setup.placeholder.text");
            }
        }
    }
}
