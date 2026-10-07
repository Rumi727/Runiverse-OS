#nullable enable
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Installer.Screens
{
    sealed class WelcomeScreen : SetupScreen
    {
        public override VisualElement logoTarget => _logoTarget;
        readonly VisualElement _logoTarget;

        readonly VisualElement headings;
        readonly Label description;

        public WelcomeScreen() : base("installer.welcome", 0, false, false)
        {
            const string templatePath = "Packages/io.github.rumi727.runios/Editor/Screens/WelcomeScreen.uxml";
            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath);
            template.CloneTree(contentContainer);
            contentContainer.AddToClassList("runios-setup__welcome");

            _logoTarget = contentContainer.Q<VisualElement>("Welcome-Logo");
            headings = contentContainer.Q<VisualElement>("Welcome-Headings");
            description = contentContainer.Q<Label>("Description");
        }

        protected internal override void OnLanguageChanged()
        {
            description.text = InstallerLocalization.GetText("installer.welcome.text");

            for (int i = 0; i < InstallerLocalization.languages.Count; i++)
                ((Label)headings.hierarchy[i]).text = InstallerLocalization.GetText("installer.welcome", InstallerLocalization.languages[i]);
        }

        protected internal override void OnUpdate(double time, float deltaTime)
        {
            _logoTarget.style.rotate = new Rotate(Angle.Degrees(Mathf.Repeat((float)time * 64, 360)));

            const int interval = 45;
            int count = headings.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                VisualElement element = headings.hierarchy[i];
                float offset = -interval + Mathf.Repeat(((float)time * 20) + (i * interval), interval * count);
                float alpha;
                if (offset < 0)
                    alpha = (1 - (Mathf.Abs(offset) / interval)) * 2;
                else
                    alpha = 1 - (Mathf.Abs(offset) / (interval * 0.5f));

                element.style.translate = new Translate(0, offset);
                element.style.opacity = Mathf.Clamp01(alpha);
            }
        }
    }
}
