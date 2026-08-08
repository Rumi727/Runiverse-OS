#nullable enable
using System.IO;
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine.Profiling;

namespace RuniOS.Editor.Windows
{
    public sealed class DevelopmentPanel : ScriptableObject, IControlPanel
    {
        public string label => "control_panel.development";

        public int sort => 1000000;

        public bool allowUpdate => true;
        public bool allowUpdateInEditor => true;

        const int DeepProfileMaxUsedMemory = 2_000_000_000;

        readonly Dictionary<DrivenPropertyManager.DrivenPropertyData, DrivenPropertyDataExtension> drivenPropertyDatas = new();
        [SerializeField] Vector2 drivenPropertyScrollPosition;
        bool reloadingWithDeepProfile;
        
        public void OnGUI()
        {
            EditorGUILayout.HelpBox(GetTextOrKey("control_panel.development.warning"), MessageType.Warning);
            Space();

            {
                GUILayout.Label(GetTextOrKey("control_panel.development.driven_property"), ControlPanel.bigLabelStyle);
                drivenPropertyDatas.SyncKeysWithEnumerable(DrivenPropertyManager.drivenProperties, x => new DrivenPropertyDataExtension(x));
                if (DrivenPropertyManager.drivenProperties.Count <= 0)
                    GUILayout.Label(GetTextOrKey("control_panel.development.driven_property.zero_count"));

                drivenPropertyScrollPosition = EditorGUILayout.BeginScrollView(drivenPropertyScrollPosition, GUILayout.ExpandHeight(false), GUILayout.Height(400));
                
                for (int i = 0; i < DrivenPropertyManager.drivenProperties.Count; i++)
                {
                    DrivenPropertyManager.DrivenPropertyData data = DrivenPropertyManager.drivenProperties[i];
                    DrivenPropertyDataExtension extData = drivenPropertyDatas[data];

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    extData.animBool.target = EditorGUILayout.Foldout(extData.animBool.target, $"{data.target} : {extData.property?.propertyPath ?? string.Empty}", true);

                    FadeGroup(extData.animBool, () =>
                    {
                        BeginIndentLevel();

                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(extData.property);

                            string buttonText = GetTextOrKey("control_panel.development.driven_property.unregister");
                            if (GUILayout.Button(buttonText, GUILayout.Width(GetXSize(buttonText, GUI.skin.button)), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                                DrivenPropertyManager.UnregisterProperty(data.driver, data.target, data.propertyPath);

                            EditorGUILayout.EndHorizontal();
                        }

                        Space();

                        {
                            EditorGUILayout.BeginHorizontal();
                            Space(EditorGUI.indentLevel * 15);

                            DrawObject(GetTextOrKey("control_panel.development.driven_property.driver"), data.driver, extData.driverEditor);
                            GUILayout.Label("→", GUILayout.Width(GetLabelXSize("→")));
                            DrawObject(GetTextOrKey("control_panel.development.driven_property.target"), data.driver, extData.driverEditor);

                            static void DrawObject(string label, Object obj, UnityEditor.Editor? editor)
                            {
                                BeginIndentLevel(0);
                                BeginLabelWidth(label);
                                
                                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                RuniLayoutFields.ObjectPingField(label, obj);

                                if (editor != null)
                                {
                                    // ReSharper disable once Unity.NoNullPatternMatching
                                    editor.ReloadPreviewInstances();
                                    editor.DrawPreview(EditorGUILayout.GetControlRect(false, 256));
                                }
                                EditorGUILayout.EndVertical();
                                
                                EndLabelWidth();
                                EndIndentLevel();
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        EndIndentLevel();
                    });

                    drivenPropertyDatas[data] = extData;
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUI.DisabledScope(reloadingWithDeepProfile))
            {
                if (GUILayout.Button(reloadingWithDeepProfile ? "리로드 중..." : "리로드"))
                    ReloadWithDeepProfile().Forget();
            }
        }

        async UniTask ReloadWithDeepProfile()
        {
            if (reloadingWithDeepProfile)
                return;

            reloadingWithDeepProfile = true;

            bool previousProfilerEnabled = Profiler.enabled;
            bool previousDriverProfiling = ProfilerDriver.enabled;
            bool previousProfileEditor = ProfilerDriver.profileEditor;
            bool previousBinaryLog = Profiler.enableBinaryLog;
            int previousMaxUsedMemory = Profiler.maxUsedMemory;
            string previousLogFile = Profiler.logFile;
            string capturePath = string.Empty;
            bool captureStarted = false;
            bool reloadCompleted = false;
            bool profilerStopped = false;

            try
            {
                capturePath = CreateProfilerCapturePath();

                ProfilerDriver.enabled = false;
                Profiler.enabled = false;
                Profiler.maxUsedMemory = DeepProfileMaxUsedMemory;

                Profiler.logFile = capturePath;
                ProfilerDriver.profileEditor = true;
                Profiler.enableBinaryLog = true;

                Profiler.enabled = true;
                ProfilerDriver.enabled = true;
                captureStarted = true;

                Debug.Log($"Started Deep Profile raw capture: {capturePath}");
                await RuniOS.Resource.ResourceManager.Reload();
                profilerStopped = captureStarted && (!Profiler.enabled || !ProfilerDriver.enabled);
                reloadCompleted = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                profilerStopped |= captureStarted && (!Profiler.enabled || !ProfilerDriver.enabled);

                ProfilerDriver.enabled = false;
                Profiler.enabled = false;

                Profiler.maxUsedMemory = previousMaxUsedMemory;
                Profiler.logFile = previousLogFile;
                Profiler.enableBinaryLog = previousBinaryLog;
                ProfilerDriver.profileEditor = previousProfileEditor;

                Profiler.enabled = previousProfilerEnabled;
                if (previousDriverProfiling)
                    ProfilerDriver.enabled = true;

                reloadingWithDeepProfile = false;

                if (capturePath.Length > 0)
                {
                    if (profilerStopped && !reloadCompleted)
                        Debug.LogWarning($"Deep Profile raw capture stopped before ResourceManager.Reload completed: {capturePath}");
                    else if (profilerStopped)
                        Debug.LogWarning($"Deep Profile raw capture stopped, but ResourceManager.Reload completed: {capturePath}");
                    else if (reloadCompleted)
                        Debug.Log($"Finished Deep Profile raw capture: {capturePath}");
                    else
                        Debug.LogWarning($"Deep Profile raw capture did not complete: {capturePath}");
                }
            }
        }

        static string CreateProfilerCapturePath()
        {
            string projectDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string projectName = new DirectoryInfo(projectDirectory).Name;
            string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string captureDirectory = Path.Combine(userDirectory, "ProfilerCaptures");
            Directory.CreateDirectory(captureDirectory);

            return Path.Combine
            (
                captureDirectory,
                $"{projectName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}_DeepProfile.raw"
            );
        }

        void OnDestroy()
        {
            foreach (var item in drivenPropertyDatas)
                item.Value.Dispose();
            
            drivenPropertyDatas.Clear();
        }

        readonly struct DrivenPropertyDataExtension : IDisposable
        {
            public readonly AnimBool animBool;
            
            public readonly UnityEditor.Editor? driverEditor;
            public readonly UnityEditor.Editor? targetEditor;
            
            public readonly SerializedProperty? property;

            public DrivenPropertyDataExtension(DrivenPropertyManager.DrivenPropertyData data)
            {
                animBool = new AnimBool(false);
                
                driverEditor = null;
                targetEditor = null;
                
                // ReSharper disable Unity.NoNullPatternMatching
                if (data.driver != null)
                {
                    if (data.driver is Component component)
                        UnityEditor.Editor.CreateCachedEditor(component.gameObject, null, ref driverEditor);
                    else
                        UnityEditor.Editor.CreateCachedEditor(data.driver, null, ref driverEditor);
                }
                if (data.target != null)
                {
                    if (data.target is Component component)
                        UnityEditor.Editor.CreateCachedEditor(component.gameObject, null, ref targetEditor);
                    else
                        UnityEditor.Editor.CreateCachedEditor(data.target, null, ref targetEditor);
                }
                // ReSharper restore Unity.NoNullPatternMatching

                property = new SerializedObject(data.target).FindProperty(data.propertyPath);
            }
            
            public void Dispose()
            {
                DestroyImmediate(driverEditor);
                DestroyImmediate(targetEditor);

                if (property != null)
                {
                    property.serializedObject.Dispose();
                    property.Dispose();
                }
            }
        }
    }
}
