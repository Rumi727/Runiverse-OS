#nullable enable
using System;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Installer.Screens
{
    sealed class PackageElement : VisualElement
    {
        static readonly CustomStyleProperty<Length> packageHeightProperty = new("--package-height");
        static readonly CustomStyleProperty<Length> headerOneLineProperty = new("--header-one-line");
        static readonly CustomStyleProperty<Length> headerTwoLineProperty = new("--header-two-line");

        public PackageAsset package { get; }
        public float collapsedHeight { get; private set; }
        public event Action<PackageElement>? expandedChanged;

        public bool expanded
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                expandedChanged?.Invoke(this);
            }
        }

        readonly VisualElement header;
        readonly Label label;
        readonly Label dash;
        readonly Label oneLineDescription;
        readonly VisualElement expandedDescriptionTarget;
        readonly Toggle toggle;
        readonly VisualElement content;
        readonly Label description;
        readonly Label dependencyLabel;
        readonly VisualElement dependencyTree;
        float headerOneLine;
        float headerTwoLine;
        float headerHeight;
        Vector2 expandedDescriptionOffset;
        Vector2 descriptionOffset;

        public PackageElement(PackageAsset package)
        {
            this.package = package;
            const string templatePath = "Packages/com.rumi.runios.installer/Editor/Screens/Package.uxml";
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath).CloneTree(this);

            // Host the template's Package root on this element, preserving its child hierarchy and USS.
            VisualElement root = this.Q<VisualElement>("Package");
            name = root.name;
            foreach (string className in root.GetClasses())
                AddToClassList(className);
            while (root.childCount != 0)
                hierarchy.Add(root[0]);
            root.RemoveFromHierarchy();

            header = this.Q<VisualElement>("Header");
            label = this.Q<Label>("Label");
            dash = this.Q<Label>("Dash");
            oneLineDescription = this.Q<Label>("One-Line-Description");
            expandedDescriptionTarget = this.Q<VisualElement>("Expanded-One-Line-Description-Target");
            toggle = this.Q<Toggle>("Toggle");
            content = this.Q<VisualElement>("Content");
            description = this.Q<Label>("Description");
            dependencyTree = this.Q<VisualElement>("Dependency-Tree");
            dependencyLabel = (Label)dependencyTree.parent[dependencyTree.parent.IndexOf(dependencyTree) - 1];

            oneLineDescription.usageHints = UsageHints.DynamicTransform;

            oneLineDescription.RegisterCallback<GeometryChangedEvent>(OnDescriptionGeometryChanged);
            expandedDescriptionTarget.RegisterCallback<GeometryChangedEvent>(OnDescriptionGeometryChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            header.AddManipulator(new Clickable(OnHeaderClicked));
            toggle.RegisterValueChangedCallback(OnSelectionChanged);
            RefreshSelection();
            RefreshDependencies();
        }

        void OnHeaderClicked(EventBase evt)
        {
            // Toggle owns its click; do not turn package selection into expansion.
            if (evt.target is VisualElement target && (target == toggle || toggle.Contains(target)))
                return;

            expanded = !expanded;
        }

        void OnSelectionChanged(ChangeEvent<bool> evt)
        {
            ConfigScriptableObject config = ConfigScriptableObject.config;
            if (evt.newValue)
                config.selectedRoots.Add(package);
            else
                config.selectedRoots.Remove(package);
            config.SetDirty();
        }

        public void RefreshSelection() => toggle.SetValueWithoutNotify(ConfigScriptableObject.config.selectedRoots.Contains(package));

        public void OnLanguageChanged()
        {
            label.text = GetLabel(package);
            oneLineDescription.text = GetText(package.oneLineDescriptionKey);
            description.text = GetText(package.descriptionKey);
            dependencyLabel.text = InstallerLocalization.GetText("installer.install_setting.dependency");
        }

        static string GetText(string key) => string.IsNullOrEmpty(key) ? string.Empty : InstallerLocalization.GetText(key);

        public static string GetLabel(IPackage package) => package is PackageAsset asset
            ? string.IsNullOrEmpty(asset.labelKey) ? asset.displayName : InstallerLocalization.GetText(asset.labelKey)
            : package.id.ToString();

        public void RefreshDependencies()
        {
            dependencyTree.Clear();
            PackageFlattenResult graph = PackageGraph.Flatten([package]);
            if (!graph.succeeded)
            {
                foreach (PackageGraphDiagnostic diagnostic in graph.diagnostics)
                    dependencyTree.Add(new HelpBox(diagnostic.code + ": " + diagnostic.message, HelpBoxMessageType.Error));
                return;
            }

            AddDependencies(dependencyTree, package);
        }

        static void AddDependencies(VisualElement parent, IPackage package)
        {
            foreach (IPackage? dependency in package.dependencies)
            {
                // Flatten has validated these direct references and rejected cycles before UI traversal.
                if (dependency is null)
                    continue;
                if (dependency.dependencies.Count == 0)
                    parent.Add(new Label("- " + dependency.id));
                else
                {
                    Foldout foldout = new() { text = dependency.id.ToString(), value = false };
                    parent.Add(foldout);
                    AddDependencies(foldout, dependency);
                }
            }
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(packageHeightProperty, out Length packageHeight))
                collapsedHeight = packageHeight.value;
            if (evt.customStyle.TryGetValue(headerOneLineProperty, out Length oneLine))
                headerOneLine = oneLine.value;
            if (evt.customStyle.TryGetValue(headerTwoLineProperty, out Length twoLine))
                headerTwoLine = twoLine.value;
            if (headerHeight == 0)
                headerHeight = headerOneLine;
        }

        void OnDescriptionGeometryChanged(GeometryChangedEvent evt)
        {
            if (oneLineDescription.layout.width <= 0 || expandedDescriptionTarget.layout.height <= 0)
                return;

            // Layout excludes this label's animated translate. Convert the marker into the same parent space.
            expandedDescriptionOffset = expandedDescriptionTarget.ChangeCoordinatesTo(oneLineDescription.parent, Vector2.zero) - oneLineDescription.layout.position;
        }

        public void OnUpdate(float deltaTime)
        {
            const float followRate = SetupWindow.followRate;

            header.style.height = headerHeight = SetupAnimationUtility.Follow(headerHeight, expanded ? headerTwoLine : headerOneLine, followRate, deltaTime);
            dash.style.opacity = SetupAnimationUtility.Follow(dash.resolvedStyle.opacity, expanded ? 0 : 1, followRate, deltaTime);
            content.style.opacity = SetupAnimationUtility.Follow(content.resolvedStyle.opacity, expanded ? 1 : 0, followRate, deltaTime);

            Vector2 target = expanded ? expandedDescriptionOffset : Vector2.zero;
            descriptionOffset.x = SetupAnimationUtility.Follow(descriptionOffset.x, target.x, followRate, deltaTime);
            descriptionOffset.y = SetupAnimationUtility.Follow(descriptionOffset.y, target.y, followRate, deltaTime);
            oneLineDescription.style.translate = new Translate(descriptionOffset.x, descriptionOffset.y);
        }
    }
}
