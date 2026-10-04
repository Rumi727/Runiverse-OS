#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.Editor.Installer.Screens
{
    sealed class ThirdScreen : SetupScreen
    {
        readonly Label label;
        readonly HelpBox info;
        readonly Button button;
        string contentKey = "installer.setup.placeholder.content";

        public ThirdScreen() : base("installer.setup.third.title", 200)
        {
            contentContainer.AddToClassList("runios-setup__placeholder");
            label = new Label();
            Add(label);
            info = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            Add(info);
            button = new Button(() =>
            {
                contentKey = "installer.setup.third.button_clicked";
                label.text = InstallerLocalization.GetText(contentKey);
            });
            Add(button);
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
            label.text = InstallerLocalization.GetText(contentKey).Replace("{screen}", title);
            info.text = InstallerLocalization.GetText("installer.setup.placeholder.info");
            button.text = InstallerLocalization.GetText("installer.setup.placeholder.button");

            foreach (VisualElement element in contentContainer.Children())
            {
                if (element is TextField textField)
                    textField.label = InstallerLocalization.GetText("installer.setup.placeholder.text");
            }
        }
    }
}
