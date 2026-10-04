#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Installer.Screens
{
    sealed class PreviewScreen : SetupScreen
    {
        sealed class Row
        {
            public readonly FlattenedPackage package;
            public readonly IInstallation installation;
            public readonly Label label = new();
            public readonly Label state = new();
            public readonly VisualElement diagnostics = new();
            public InstallationPreview? preview;
            public InstallationResult? result;

            public Row(FlattenedPackage package, IInstallation installation)
            {
                this.package = package;
                this.installation = installation;
                label.AddToClassList("runios-setup__section-title");
            }
        }

        readonly HelpBox info = new(string.Empty, HelpBoxMessageType.Info);
        readonly HelpBox status = new(string.Empty, HelpBoxMessageType.Info);
        readonly VisualElement changes = new();
        readonly Button refresh;
        readonly Button install;
        readonly Button cancel;
        readonly List<Row> rows = [];
        PackageFlattenResult? closure;
        CancellationTokenSource? cancellation;
        string statusKey = "installer.setup.preview.empty";
        string error = string.Empty;

        public PreviewScreen() : base("installer.setup.preview.title", 300)
        {
            contentContainer.AddToClassList("runios-setup__settings");
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.rumi.runios.installer/Editor/Screens/SetupScreens.uss"));
            refresh = new Button(() => _ = RunAsync(true));
            install = new Button(() => _ = RunAsync(false));
            cancel = new Button(() => cancellation?.Cancel());
            Add(info);
            Add(changes);
            Add(status);
            Add(refresh);
            Add(install);
            Add(cancel);
            RegisterCallback<DetachFromPanelEvent>(_ => cancellation?.Cancel());
            UpdateButtons();
        }

        protected internal override void OnActivated() => _ = RunAsync(true);
        protected internal override void OnDeactivated() => cancellation?.Cancel();

        async Task RunAsync(bool preview)
        {
            cancellation?.Cancel();
            CancellationTokenSource source = new();
            cancellation = source;
            CancellationToken token = source.Token;
            bool reloadLocked = false;
            rows.Clear();
            changes.Clear();
            error = string.Empty;
            statusKey = preview ? "installer.setup.preview.observing" : "installer.setup.preview.ensuring";
            UpdateButtons();
            OnLanguageChanged();
            try
            {
                // Both actions ask the backend for a fresh closure. Preview is never an execution plan.
                closure = PackageGraph.Flatten(ConfigScriptableObject.config.selectedRoots);
                if (!closure.succeeded)
                {
                    foreach (PackageGraphDiagnostic diagnostic in closure.diagnostics)
                        changes.Add(new HelpBox(diagnostic.code + ": " + diagnostic.message, HelpBoxMessageType.Error));
                    statusKey = "installer.setup.preview.graph_error";
                    return;
                }
                if (closure.packages.Count == 0)
                {
                    statusKey = "installer.setup.preview.empty";
                    return;
                }

                List<IInstallation> installations = new(closure.packages.Count);
                foreach (FlattenedPackage package in closure.packages)
                {
                    IInstallation installation = package.package.CreateInstallation(package.isRoot);
                    installations.Add(installation);
                    Row row = new(package, installation);
                    rows.Add(row);
                    VisualElement box = new();
                    box.AddToClassList("runios-setup__resource-box");
                    box.Add(row.label);
                    box.Add(row.state);
                    box.Add(row.diagnostics);
                    changes.Add(box);
                }
                OnLanguageChanged();

                InstallationRunner runner = new(new IInstallationExecutor[] { new UpmExecutor(), new EmbeddedExecutor() });
                EditorApplication.LockReloadAssemblies();
                reloadLocked = true;
                if (preview)
                {
                    await foreach (InstallationPreview observation in runner.PreviewAsync(installations, token))
                    {
                        await Awaitable.MainThreadAsync();
                        token.ThrowIfCancellationRequested();
                        foreach (Row row in rows)
                            if (ReferenceEquals(row.installation, observation.installation))
                            {
                                row.preview = observation;
                                UpdateRow(row);
                                break;
                            }
                    }
                    token.ThrowIfCancellationRequested();
                    statusKey = "installer.setup.preview.ready";
                }
                else
                {
                    bool succeeded = true;
                    await foreach (InstallationResult result in runner.EnsureAsync(installations, token))
                    {
                        await Awaitable.MainThreadAsync();
                        token.ThrowIfCancellationRequested();
                        succeeded &= result.succeeded;
                        foreach (Row row in rows)
                            if (ReferenceEquals(row.installation, result.installation))
                            {
                                row.result = result;
                                UpdateRow(row);
                                break;
                            }
                    }
                    token.ThrowIfCancellationRequested();
                    statusKey = succeeded ? "installer.setup.preview.complete" : "installer.setup.preview.failed";
                }
            }
            catch (OperationCanceledException)
            {
                if (cancellation == source)
                    statusKey = "installer.setup.preview.cancelled";
            }
            catch (Exception exception)
            {
                await Awaitable.MainThreadAsync();
                if (cancellation == source)
                {
                    statusKey = "installer.setup.preview.failed";
                    error = exception.Message;
                }
            }
            finally
            {
                await Awaitable.MainThreadAsync();
                source.Dispose();
                if (cancellation == source)
                {
                    cancellation = null;
                    OnLanguageChanged();
                    UpdateButtons();
                }
                if (reloadLocked)
                    EditorApplication.UnlockReloadAssemblies();
            }
        }

        void UpdateButtons()
        {
            refresh.SetEnabled(cancellation == null);
            install.SetEnabled(cancellation == null && closure is { succeeded: true } && rows.Count != 0);
            cancel.style.display = cancellation == null ? DisplayStyle.None : DisplayStyle.Flex;
        }

        protected internal override void OnLanguageChanged()
        {
            info.text = InstallerLocalization.GetText("installer.setup.preview.info");
            status.text = InstallerLocalization.GetText(statusKey) + (string.IsNullOrEmpty(error) ? string.Empty : "\n" + error);
            status.messageType = statusKey is "installer.setup.preview.failed" or "installer.setup.preview.graph_error" ? HelpBoxMessageType.Error : HelpBoxMessageType.Info;
            refresh.text = InstallerLocalization.GetText("installer.setup.preview.refresh");
            install.text = InstallerLocalization.GetText("installer.install_setting.install");
            cancel.text = InstallerLocalization.GetText("installer.setup.preview.cancel");
            foreach (Row row in rows)
                UpdateRow(row);
        }

        static void UpdateRow(Row row)
        {
            string provenance = InstallerLocalization.GetText(row.package.isRoot ? "installer.setup.preview.root" : "installer.setup.preview.dependency");
            row.label.text = PackageElement.GetLabel(row.package.package) + " (" + row.package.package.id + ") — " + provenance;
            bool delegated = row.installation is UpmInstallation { ensurePackage: false };
            string key = row.result is { } result
                ? !result.succeeded ? "installer.setup.preview.failed" : delegated ? "installer.setup.preview.delegated" : "installer.setup.preview.satisfied"
                : row.preview?.status switch
                {
                    InstallationPreviewStatus.Satisfied => "installer.setup.preview.satisfied",
                    InstallationPreviewStatus.RequiresEnsure => delegated ? "installer.setup.preview.registry_required" : "installer.setup.preview.required",
                    InstallationPreviewStatus.Delegated => "installer.setup.preview.delegated",
                    InstallationPreviewStatus.NotSupported => "installer.setup.preview.unsupported",
                    InstallationPreviewStatus.Failed => "installer.setup.preview.failed",
                    _ => "installer.setup.preview.observing"
                };
            row.state.text = InstallerLocalization.GetText(key);
            row.diagnostics.Clear();
            IReadOnlyList<InstallationDiagnostic>? diagnostics = row.result?.diagnostics ?? row.preview?.diagnostics;
            if (diagnostics != null)
                foreach (InstallationDiagnostic diagnostic in diagnostics)
                    row.diagnostics.Add(new HelpBox(diagnostic.code + ": " + diagnostic.message, HelpBoxMessageType.Error));
        }
    }
}
