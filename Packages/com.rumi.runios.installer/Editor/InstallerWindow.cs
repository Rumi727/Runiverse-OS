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
        readonly List<(string label, string identity)> _requiredRows = new();
        readonly List<(string label, string identity)> _unusedRows = new();
        readonly List<InstallationResult> _results = new();
        Vector2 _scroll;
        bool _executing;
        string _status = string.Empty;
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
                if (serialized.ApplyModifiedProperties()) { _closure = null; _requiredRows.Clear(); _unusedRows.Clear(); _results.Clear(); _status = string.Empty; }
                if (GUILayout.Button("Flatten dependencies")) TryFlatten();
            }
            if (_closure is not null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Required packages", EditorStyles.boldLabel);
                foreach (var row in _requiredRows)
                    EditorGUILayout.LabelField(row.label, row.identity);
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
                    EditorGUILayout.HelpBox("This is a definition query. No installed package will be removed.", MessageType.Info);
                }
            }
            using (new EditorGUI.DisabledScope(_executing || _closure is null || !_closure.succeeded || _closure.packages.Count == 0))
                if (GUILayout.Button("Ensure required installations")) _ = EnsureAsync();
            if (_executing && GUILayout.Button("Cancel")) _cancellation?.Cancel();
            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, MessageType.Info);
            foreach (InstallationResult result in _results)
            {
                string executor = result.executor?.GetType().Name ?? "Unsupported";
                EditorGUILayout.LabelField(executor + " / " + result.installation.GetType().Name, result.succeeded ? "Satisfied" : "Failed");
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
                _closure = new PackageGraph(definitions).Flatten(_roots);
                _requiredRows.Clear();
                _unusedRows.Clear();
                foreach (IPackage package in _closure.packages)
                    _requiredRows.Add((PackageLabel(package), package.exactIdentity));
                if (_closure.succeeded)
                    foreach (IPackage package in _closure.GetUnused(NonNullDefinitions(definitions)))
                        _unusedRows.Add((PackageLabel(package), package.exactIdentity));
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
        async Task EnsureAsync()
        {
            if (_executing || !TryFlatten() || _closure is null) return;
            _executing = true;
            _results.Clear();
            var cancellation = new CancellationTokenSource();
            _cancellation = cancellation;
            bool reloadLocked = false;
            try
            {
                // Capture each code-derived descriptor once for this execution.
                var installations = new List<IInstallation>(_closure.packages.Count);
                foreach (IPackage package in _closure.packages) installations.Add(package.CreateInstallation());
                var runner = new InstallationRunner(new IInstallationExecutor[] { new UpmExecutor(), new EmbeddedExecutor() });
                EditorApplication.LockReloadAssemblies();
                reloadLocked = true;
                _status = "Checking and ensuring required installations…";
                await foreach (InstallationResult result in runner.EnsureAsync(installations, cancellation.Token))
                {
                    await Awaitable.MainThreadAsync();
                    _results.Add(result);
                    if (this != null) Repaint();
                }
                bool succeeded = true;
                foreach (InstallationResult result in _results) succeeded &= result.succeeded;
                _status = succeeded ? "All required installations are satisfied." : "Some installations failed. See the execution diagnostics.";
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
