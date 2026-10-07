#nullable enable
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Installer.Screens
{
    sealed class TMPSettingScreen : SetupScreen
    {
        readonly Label info = new();
        readonly HelpBox warning = new(string.Empty, HelpBoxMessageType.Error);
        readonly VisualElement resources = new();
        readonly Label essentialsTitle = new();
        readonly Label essentialsInfo = new();
        readonly Button essentials;
        readonly Label examplesTitle = new();
        readonly Label examplesInfo = new();
        readonly Button examples;
        Type? settingsType;
        bool essentialsImported;
        bool examplesImported;
        bool needsUpdate;
        bool examplesNeedUpdate;

        public TMPSettingScreen() : base("installer.tmp_setting.label", 100)
        {
            contentContainer.AddToClassList("runios-setup__settings");
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/io.github.rumi727.runios/Editor/Screens/SetupScreens.uss"));
            Add(info);
            Add(warning);
            Add(resources);

            essentialsTitle.AddToClassList("runios-setup__section-title");
            examplesTitle.AddToClassList("runios-setup__section-title");

            essentials = new Button(() => EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources")) { focusable = false };
            examples = new Button(() => EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Examples and Extras")) { focusable = false };
            VisualElement essentialsBox = new();
            essentialsBox.AddToClassList("runios-setup__resource-box");
            essentialsBox.Add(essentialsTitle);
            essentialsBox.Add(essentialsInfo);
            essentialsBox.Add(essentials);
            resources.Add(essentialsBox);
            VisualElement examplesBox = new();
            examplesBox.AddToClassList("runios-setup__resource-box");
            examplesBox.Add(examplesTitle);
            examplesBox.Add(examplesInfo);
            examplesBox.Add(examples);
            resources.Add(examplesBox);

            // TMP is optional at bootstrap. Observe its settings without an assembly dependency.
            foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
                if (type.FullName == "TMPro.TMP_Settings")
                {
                    settingsType = type;
                    break;
                }

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                EditorApplication.projectChanged += Refresh;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
                AssetDatabase.importPackageCompleted += OnPackageImported;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                EditorApplication.projectChanged -= Refresh;
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                AssetDatabase.importPackageCompleted -= OnPackageImported;
            });
        }

        protected internal override void OnActivated() => Refresh();
        void OnPlayModeChanged(PlayModeStateChange state) => Refresh();
        void OnPackageImported(string packageName)
        {
            if (packageName == "TMP Examples & Extras")
                examplesNeedUpdate = false;
            Refresh();
        }

        void Refresh()
        {
            essentialsImported = File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset");
            examplesImported = Directory.Exists("Assets/TextMesh Pro/Examples & Extras");
            needsUpdate = false;
            UnityEngine.Object? settings = AssetDatabase.LoadMainAssetAtPath("Assets/TextMesh Pro/Resources/TMP Settings.asset");
            if (settings != null && settingsType != null)
            {
                // Read TMP's own version rather than duplicating its resource-version constant.
                string? currentVersion = settingsType.GetField("s_CurrentAssetVersion", BindingFlags.Static | BindingFlags.NonPublic)?.GetRawConstantValue() as string;
                SerializedProperty? version = new SerializedObject(settings).FindProperty("assetVersion");
                needsUpdate = currentVersion != null && version != null && version.stringValue != currentVersion;
            }
            examplesNeedUpdate |= needsUpdate;

            warning.style.display = settingsType == null ? DisplayStyle.Flex : DisplayStyle.None;
            resources.style.display = settingsType == null ? DisplayStyle.None : DisplayStyle.Flex;
            essentials.SetEnabled((!essentialsImported || needsUpdate) && !EditorApplication.isPlaying);
            examples.SetEnabled(((essentialsImported && !examplesImported) || examplesNeedUpdate) && !EditorApplication.isPlaying);
            OnLanguageChanged();
        }

        protected internal override void OnLanguageChanged()
        {
            info.text = InstallerLocalization.GetText("installer.tmp_setting.info");
            warning.text = InstallerLocalization.GetText("installer.tmp_setting.warning");
            essentialsTitle.text = InstallerLocalization.GetText("installer.tmp_setting.essentials.title");
            essentialsInfo.text = InstallerLocalization.GetText(needsUpdate ? "installer.tmp_setting.essentials.update" : "installer.tmp_setting.essentials.info");
            essentials.text = InstallerLocalization.GetText("installer.tmp_setting.essentials.import");
            examplesTitle.text = InstallerLocalization.GetText("installer.tmp_setting.examples.title");
            examplesInfo.text = InstallerLocalization.GetText(examplesNeedUpdate ? "installer.tmp_setting.examples.update" : "installer.tmp_setting.examples.info");
            examples.text = InstallerLocalization.GetText("installer.tmp_setting.examples.import");
        }
    }
}
