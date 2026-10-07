#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Unity;
using System.Linq;
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
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/io.github.rumi727.runios/Editor/Screens/SetupScreens.uss"));
            refresh = new Button(() => _ = RunAsync(true)) { focusable = false };
            install = new Button(OnInstallClicked) { focusable = false };
            cancel = new Button(() => cancellation?.Cancel()) { focusable = false };
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

        void OnInstallClicked()
        {
            List<string> embeddedConflicts = [];
            foreach (Row row in rows)
            {
                if (row.preview != null && row.preview.diagnostics.Any(diagnostic => diagnostic.code == "upm:embedded-conflict"))
                    embeddedConflicts.Add(row.label.text);
            }

            if (embeddedConflicts.Count != 0)
            {
                string message = InstallerLocalization.GetText("installer.setup.preview.embedded_conflict_message")
                    + "\n\n" + string.Join("\n", embeddedConflicts);
                bool proceed = EditorUtility.DisplayDialog
                (
                    InstallerLocalization.GetText("installer.setup.preview.embedded_conflict_title"), message,
                    InstallerLocalization.GetText("installer.setup.preview.embedded_conflict_confirm"),
                    InstallerLocalization.GetText("installer.setup.preview.embedded_conflict_cancel")
                );
                if (!proceed)
                    return;
            }

            List<string> versionChanges = [];
            foreach (Row row in rows)
            {
                if (row.preview?.status == InstallationPreviewStatus.RequiresForce && row.preview.diagnostics.Count != 0)
                    versionChanges.Add(row.label.text + "\n" + row.preview.diagnostics[0].message);
            }

            bool force = false;
            if (versionChanges.Count != 0)
            {
                string message = InstallerLocalization.GetText("installer.setup.preview.version_change_message") + "\n\n" + string.Join("\n\n", versionChanges);
                force = EditorUtility.DisplayDialog
                (
                    InstallerLocalization.GetText("installer.setup.preview.version_change_title"), message,
                    InstallerLocalization.GetText("installer.setup.preview.version_change_accept"),
                    InstallerLocalization.GetText("installer.setup.preview.version_change_decline")
                );
            }
            _ = RunAsync(false, force);
        }

        async Task RunAsync(bool preview, bool force = false)
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

                InstallationRunner runner = new([new UpmExecutor(), new EmbeddedExecutor()]);
                EditorApplication.LockReloadAssemblies();
                reloadLocked = true;
                if (preview)
                {
                    bool succeeded = true;
                    await foreach (InstallationPreview observation in runner.PreviewAsync(installations, token))
                    {
                        await Awaitable.MainThreadAsync();
                        token.ThrowIfCancellationRequested();
                        succeeded &= observation.status != InstallationPreviewStatus.Failed;
                        foreach (Row row in rows)
                        {
                            if (row.installation != observation.installation)
                                continue;

                            row.preview = observation;
                            UpdateRow(row);
                            break;
                        }
                    }
                    token.ThrowIfCancellationRequested();
                    statusKey = succeeded ? "installer.setup.preview.ready" : "installer.setup.preview.failed";
                }
                else
                {
                    bool succeeded = true;
                    await foreach (InstallationResult result in runner.EnsureAsync(installations, force, token))
                    {
                        await Awaitable.MainThreadAsync();
                        token.ThrowIfCancellationRequested();
                        succeeded &= result.succeeded;
                        foreach (Row row in rows)
                        {
                            if (row.installation != result.installation)
                                continue;

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

            bool canInstall = cancellation == null && closure is { succeeded: true } && rows.Count != 0;
            if (rows.Any(row => row.preview?.status == InstallationPreviewStatus.Failed))
                canInstall = false;

            install.SetEnabled(canInstall);
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
            string key;
            if (row.result is { } result)
                key = !result.succeeded ? "installer.setup.preview.failed" : delegated ? "installer.setup.preview.delegated" : "installer.setup.preview.satisfied";
            else
            {
                key = row.preview?.status switch
                {
                    InstallationPreviewStatus.Satisfied => "installer.setup.preview.satisfied",
                    InstallationPreviewStatus.RequiresEnsure => delegated ? "installer.setup.preview.registry_required" : "installer.setup.preview.required",
                    InstallationPreviewStatus.RequiresForce => "installer.setup.preview.version_change_required",
                    InstallationPreviewStatus.Delegated => "installer.setup.preview.delegated",
                    InstallationPreviewStatus.NotSupported => "installer.setup.preview.unsupported",
                    InstallationPreviewStatus.Failed => "installer.setup.preview.failed",
                    _ => "installer.setup.preview.observing"
                };
            }
            row.state.text = InstallerLocalization.GetText(key);
            row.diagnostics.Clear();
            IReadOnlyList<InstallationDiagnostic>? diagnostics = row.result?.diagnostics ?? row.preview?.diagnostics;
            if (diagnostics == null)
                return;

            foreach (InstallationDiagnostic diagnostic in diagnostics)
            {
                string message = diagnostic.code + ": " + diagnostic.message;
                message = diagnostic.code switch
                {
                    "unity:required-assembly-missing" => InstallerLocalization.GetText("installer.setup.preview.assembly_required") + "\n" + message,
                    "upm:embedded-conflict" => InstallerLocalization.GetText("installer.setup.preview.embedded_conflict_warning"),
                    _ => message
                };
                row.diagnostics.Add(new HelpBox(message, diagnostic.code is "upm:version-mismatch" or "upm:embedded-conflict" ? HelpBoxMessageType.Warning : HelpBoxMessageType.Error));
            }
        }
    }
}
