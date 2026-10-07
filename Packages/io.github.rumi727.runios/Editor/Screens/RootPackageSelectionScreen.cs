#nullable enable
using System.Collections.Generic;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Installer.Screens
{
    sealed class RootPackageSelectionScreen : SetupScreen
    {
        readonly List<(PackageElement element, float y, float height, float opacity, float progress)> packages = [];

        public RootPackageSelectionScreen() : base("installer.setup.roots.title", 200)
        {
            const string catalogPath = "Packages/io.github.rumi727.runios/Editor/Screens/RootPackages.asset";
            PackageCatalog catalog = AssetDatabase.LoadAssetAtPath<PackageCatalog>(catalogPath);
            foreach (IPackage? definition in catalog.packages)
            {
                if (definition is not PackageAsset asset)
                    continue;

                PackageElement element = new(asset)
                {
                    style =
                    {
                        position = Position.Absolute,
                        left = 0,
                        right = 0
                    }
                };
                element.expandedChanged += OnExpandedChanged;
                packages.Add((element, 0, 0, 1, 0));
                Add(element);
            }

            RegisterCallback<AttachToPanelEvent>(_ => EditorApplication.projectChanged += OnPackagesChanged);
            RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.projectChanged -= OnPackagesChanged);
        }

        void OnExpandedChanged(PackageElement changed)
        {
            if (changed.expanded)
            {
                foreach (var package in packages)
                {
                    if (package.element != changed)
                        package.element.expanded = false;
                }

                // The legacy selected card is drawn last, over fading siblings.
                changed.BringToFront();
            }

            bool anyExpanded = false;
            foreach (var package in packages)
                anyExpanded |= package.element.expanded;
            foreach (var package in packages)
                package.element.SetEnabled(!anyExpanded || package.element.expanded);
        }

        protected internal override void OnActivated()
        {
            foreach (var package in packages)
                package.element.RefreshSelection();
        }

        void OnPackagesChanged()
        {
            foreach (var package in packages)
            {
                package.element.RefreshDependencies();
                package.element.OnLanguageChanged();
            }
        }

        protected internal override void OnLanguageChanged()
        {
            foreach (var package in packages)
                package.element.OnLanguageChanged();
        }

        protected internal override void OnUpdate(double time, float deltaTime)
        {
            if (scrollView == null)
                return;

            float availableHeight = scrollView.contentViewport.layout.height;
            if (float.IsNaN(availableHeight) || availableHeight <= 0)
                return;

            bool anyExpanded = false;
            float collapsedListHeight = 0;
            foreach (var package in packages)
            {
                anyExpanded |= package.element.expanded;
                collapsedListHeight += package.element.collapsedHeight + package.element.resolvedStyle.marginTop + package.element.resolvedStyle.marginBottom;
            }
            contentContainer.style.height = Mathf.Max(availableHeight, collapsedListHeight);

            float nextY = 0;
            for (int i = 0; i < packages.Count; i++)
            {
                var state = packages[i];
                PackageElement element = state.element;

                if (state.height == 0)
                {
                    state.y = nextY;
                    state.height = element.collapsedHeight;
                }

                float targetY = element.expanded ? 0 : nextY;
                state.y = SetupAnimationUtility.Follow(state.y, targetY, SetupWindow.followRate, deltaTime);

                float margins = element.resolvedStyle.marginTop + element.resolvedStyle.marginBottom;
                float targetHeight = element.expanded ? Mathf.Max(element.collapsedHeight, availableHeight - margins) : element.collapsedHeight;
                state.height = SetupAnimationUtility.Follow(state.height, targetHeight, SetupWindow.followRate, deltaTime);

                float targetOpacity = !anyExpanded || element.expanded ? 1 : 0;
                state.opacity = SetupAnimationUtility.Follow(state.opacity, targetOpacity, SetupWindow.followRate, deltaTime);

                float targetProgress = element.expanded ? 1 : 0;
                state.progress = SetupAnimationUtility.Follow(state.progress, targetProgress, SetupWindow.followRate, deltaTime);

                packages[i] = state;

                element.style.translate = new Translate(0, state.y - (scrollView.contentContainer.resolvedStyle.translate.y * state.progress), 0);
                element.style.height = state.height;
                element.style.opacity = state.opacity;

                element.OnUpdate(deltaTime);

                nextY += element.collapsedHeight + margins;
            }
        }
    }
}
