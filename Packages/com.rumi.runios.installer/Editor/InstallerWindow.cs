#nullable enable
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Unity;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Installer
{
    /// <summary>
    /// Authors root selection, displays dependency closures, and runs configured installation executors.<br/>
    /// root 선택을 작성하고 dependency closure를 표시하며 구성한 installation executor를 실행합니다.
    /// </summary>
    public sealed class InstallerWindow : EditorWindow
    {
        [SerializeField] PackageCatalog? _catalog;
        [SerializeField] PackageAsset?[] _packages = [];
        [SerializeField] PackageAsset?[] _roots = [];
        PackageFlattenResult? _closure;
        readonly List<(string label, string identity, bool isRoot, string requiredBy)> _requiredRows = new();
        readonly List<(string label, string identity)> _unusedRows = new();
        readonly List<InstallationPreview> _previews = new();
        readonly List<InstallationResult> _results = new();
        Vector2 _scroll;
        bool _executing;
        string _status = string.Empty;
        string _inventoryError = string.Empty;
        CancellationTokenSource? _cancellation;

        /// <summary>
        /// Opens the independent installer window.<br/>
        /// 독립 Installer 창을 엽니다.
        /// </summary>
        [MenuItem("Window/Runiverse OS/Installer")]
        public static void Open() => GetWindow<InstallerWindow>("Runiverse OS Installer");

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Package definitions", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_executing))
            {
                var serialized = new SerializedObject(this);
                serialized.Update();
                EditorGUILayout.PropertyField(serialized.FindProperty("_catalog"), new GUIContent("Catalog (optional)"));
                if (_catalog == null) EditorGUILayout.PropertyField(serialized.FindProperty("_packages"), new GUIContent("Definitions"), true);
                EditorGUILayout.PropertyField(serialized.FindProperty("_roots"), new GUIContent("Selected roots"), true);
                if (serialized.ApplyModifiedProperties()) { _closure = null; _requiredRows.Clear(); _unusedRows.Clear(); _previews.Clear(); _results.Clear(); _status = string.Empty; _inventoryError = string.Empty; }
                if (GUILayout.Button("Flatten dependencies")) { _previews.Clear(); _results.Clear(); TryFlatten(); }
            }
            if (_closure is not null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Selected roots", EditorStyles.boldLabel);
                DrawPackageRows(true);
                EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel);
                DrawPackageRows(false);
                foreach (PackageGraphDiagnostic diagnostic in _closure.diagnostics)
                {
                    EditorGUILayout.HelpBox(diagnostic.code + ": " + diagnostic.message, MessageType.Error);
                    if (diagnostic.owner is not null) EditorGUILayout.LabelField("Owner / slot", PackageLabel(diagnostic.owner) + " / " + diagnostic.referenceIndex);
                    if (diagnostic.path.Count != 0) EditorGUILayout.LabelField("Reference path", PathLabel(diagnostic.path));
                    if (diagnostic.relatedPath.Count != 0) EditorGUILayout.LabelField("Conflicting path", PathLabel(diagnostic.relatedPath));
                }
                if (_closure.succeeded)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Definitions outside this closure", EditorStyles.boldLabel);
                    foreach (var row in _unusedRows)
                        EditorGUILayout.LabelField(row.label, row.identity);
                    if (!string.IsNullOrEmpty(_inventoryError)) EditorGUILayout.HelpBox(_inventoryError, MessageType.Warning);
                    EditorGUILayout.HelpBox("This is a definition query. No installed package will be removed.", MessageType.Info);
                }
            }
            using (new EditorGUI.DisabledScope(_executing || _closure is null || !_closure.succeeded || _closure.packages.Count == 0))
            {
                if (GUILayout.Button("Preview required installations")) _ = RunAsync(true);
                if (GUILayout.Button("Ensure required installations")) _ = EnsureAsync();
            }
            if (_executing && GUILayout.Button("Cancel")) _cancellation?.Cancel();
            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, MessageType.Info);
            if (_previews.Count != 0) EditorGUILayout.LabelField("Preview (advisory)", EditorStyles.boldLabel);
            foreach (InstallationPreview preview in _previews)
            {
                string executor = preview.executor?.GetType().Name ?? "Unsupported";
                string status = preview.status == InstallationPreviewStatus.Delegated ? "Registry ready; package acquisition delegated"
                    : preview.status == InstallationPreviewStatus.RequiresEnsure && preview.installation is UpmInstallation { ensurePackage: false } ? "Registry preparation required; package acquisition delegated"
                    : preview.status.ToString();
                EditorGUILayout.LabelField(executor + " / " + InstallationLabel(preview.installation), status);
                foreach (InstallationDiagnostic diagnostic in preview.diagnostics)
                    EditorGUILayout.HelpBox(diagnostic.code + ": " + diagnostic.message, MessageType.Error);
            }
            foreach (InstallationResult result in _results)
            {
                string executor = result.executor?.GetType().Name ?? "Unsupported";
                string status = !result.succeeded ? "Failed" : result.installation is UpmInstallation { ensurePackage: false }
                    ? "Registry ready; package acquisition delegated" : "Satisfied";
                EditorGUILayout.LabelField(executor + " / " + InstallationLabel(result.installation), status);
                foreach (InstallationDiagnostic diagnostic in result.diagnostics)
                    EditorGUILayout.HelpBox(diagnostic.code + ": " + diagnostic.message, MessageType.Error);
            }
            EditorGUILayout.EndScrollView();
        }
        bool TryFlatten()
        {
            try
            {
                IReadOnlyList<IPackage?> definitions = _catalog != null ? _catalog.packages : _packages;
                _closure = PackageGraph.Flatten(_roots);
                _requiredRows.Clear();
                _unusedRows.Clear();
                _inventoryError = string.Empty;
                foreach (FlattenedPackage flattened in _closure.packages)
                    _requiredRows.Add((PackageLabel(flattened.package), flattened.package.exactIdentity, flattened.isRoot, RequiredByLabel(flattened.requiredBy)));
                if (_closure.succeeded)
                {
                    try
                    {
                        foreach (IPackage package in _closure.GetUnused(NonNullDefinitions(definitions)))
                            _unusedRows.Add((PackageLabel(package), package.exactIdentity));
                    }
                    catch (Exception exception) { _unusedRows.Clear(); _inventoryError = "Optional inventory query failed: " + exception.Message; }
                }
                _status = _closure.succeeded ? "Dependency closure is valid." : "Resolve graph errors before installation.";
                return _closure.succeeded;
            }
            catch (Exception exception)
            {
                _closure = null;
                _requiredRows.Clear();
                _unusedRows.Clear();
                _status = exception.Message;
                return false;
            }
        }
        Task EnsureAsync() => RunAsync(false);
        async Task RunAsync(bool preview)
        {
            if (_executing || !TryFlatten() || _closure is null) return;
            _executing = true;
            _previews.Clear();
            _results.Clear();
            var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            bool reloadLocked = false;
            try
            {
                // Root provenance is translated by the Package, not by the executor.
                var installations = new List<IInstallation>(_closure.packages.Count);
                foreach (FlattenedPackage flattened in _closure.packages)
                    installations.Add(flattened.package.CreateInstallation(flattened.isRoot));
                var runner = new InstallationRunner(new IInstallationExecutor[] { new UpmExecutor(), new EmbeddedExecutor() });
                EditorApplication.LockReloadAssemblies();
                reloadLocked = true;
                if (preview)
                {
                    _status = "Observing required installations…";
                    await foreach (InstallationPreview observation in runner.PreviewAsync(installations, cancellation.Token))
                    {
                        await Awaitable.MainThreadAsync();
                        _previews.Add(observation);
                        if (this != null) Repaint();
                    }
                    _status = "Preview is advisory. Ensure observes the environment again.";
                }
                else
                {
                    _status = "Checking and ensuring required installations…";
                    await foreach (InstallationResult result in runner.EnsureAsync(installations, cancellation.Token))
                    {
                        await Awaitable.MainThreadAsync();
                        _results.Add(result);
                        if (this != null) Repaint();
                    }
                    bool succeeded = true;
                    foreach (InstallationResult result in _results) succeeded &= result.succeeded;
                    _status = succeeded ? "All descriptor requirements were ensured." : "Some installations failed. See the execution diagnostics.";
                }
            }
            catch (OperationCanceledException) { _status = "Cancelled. An already-started UPM request was allowed to finish."; }
            catch (Exception exception) { _status = exception.Message; }
            finally
            {
                await Awaitable.MainThreadAsync();
                cancellation.Dispose();
                _cancellation = null;
                _executing = false;
                if (reloadLocked) EditorApplication.UnlockReloadAssemblies();
                if (this != null) Repaint();
            }
        }
        void DrawPackageRows(bool isRoot)
        {
            foreach (var row in _requiredRows)
            {
                if (row.isRoot != isRoot) continue;
                EditorGUILayout.LabelField(row.label, row.identity);
                if (!string.IsNullOrEmpty(row.requiredBy)) EditorGUILayout.LabelField("Required by", row.requiredBy);
            }
        }
        static string InstallationLabel(IInstallation installation) => installation is UpmInstallation upm ? upm.packageName
            : installation is EmbeddedInstallation embedded ? embedded.packageName : installation.GetType().Name;
        static string RequiredByLabel(IEnumerable<IPackage> packages)
        {
            var labels = new List<string>();
            foreach (IPackage package in packages) labels.Add(PackageLabel(package));
            return string.Join(", ", labels);
        }
        void OnDisable() => _cancellation?.Cancel();
        static string PackageLabel(IPackage package)
        {
            if (package is UnityEngine.Object value && value == null) return "Missing asset";
            return package is PackageAsset asset ? asset.displayName : package is UnityEngine.Object other ? other.name : package.GetType().Name;
        }
        static string PathLabel(IReadOnlyList<IPackage> path)
        {
            var names = new string[path.Count];
            for (int i = 0; i < names.Length; i++) names[i] = PackageLabel(path[i]);
            return string.Join(" / ", names);
        }
        static IEnumerable<IPackage> NonNullDefinitions(IEnumerable<IPackage?> definitions)
        {
            foreach (IPackage? package in definitions)
                if (package is not null) yield return package;
        }
    }
}
