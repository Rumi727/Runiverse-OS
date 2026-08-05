#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.APIMarshal.UnityEditor;
using RuniOS.IO;

namespace RuniOS.Editor.Resource
{
    [CustomEditor(typeof(DefaultAsset))]
    [CanEditMultipleObjects]
    class DefaultAssetHook : EditorMarshal
    {
        public override string targetTitle
        {
            get
            {
                if (PackInspectorSystem.activeDrawer != null)
                {
                    if (PackInspectorSystem.activeDrawer.targetTitle != null)
                        return PackInspectorSystem.activeDrawer.targetTitle;

                    string typeName = PackInspectorSystem.activeDrawer.targetTypeName;
                    string settingsText = GetTextOrKey("runios-editor:gui.settings");
                    if (targets.Length > 1)
                        return $"{targets.Length} {typeName} {settingsText}";
                    else
                        return $"{target.name} ({typeName}) {settingsText}";
                }
                else
                    return base.targetTitle;
            }
        }

        static GUIStyle? _paddingStyle;
        public static GUIStyle paddingStyle => _paddingStyle ??= new GUIStyle { padding = new RectOffset(15, 0, 3, 0) };
        
        PackDrawer? subscribedDrawer;

        void OnEnable()
        {
            PackInspectorSystem.onActiveDrawerChanged += BindActiveDrawer;
            BindActiveDrawer();
        }

        void OnDisable()
        {
            PackInspectorSystem.onActiveDrawerChanged -= BindActiveDrawer;
            UnsubscribeDrawer();
        }

        void BindActiveDrawer()
        {
            UnsubscribeDrawer();

            subscribedDrawer = PackInspectorSystem.activeDrawer;
            if (subscribedDrawer != null)
            {
                subscribedDrawer.repaintAction += Repaint;
                subscribedDrawer.onDirtyStateChanged += SyncDirtyState;

                SyncDirtyState();
            }
        }

        void UnsubscribeDrawer()
        {
            if (subscribedDrawer != null)
            {
                subscribedDrawer.repaintAction -= Repaint;
                subscribedDrawer.onDirtyStateChanged -= SyncDirtyState;

                subscribedDrawer = null;
            }
        }

        void SyncDirtyState()
        {
            if (subscribedDrawer != null)
            {
                hasUnsavedChanges = subscribedDrawer.isDirty;
                saveChangesMessage = "파일의 임포트 설정 적용되지 않음";
            }
        }

        public override void SaveChanges()
        {
            subscribedDrawer?.SaveChanges();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            subscribedDrawer?.DiscardChanges();
            base.DiscardChanges();
        }

        public override void OnInspectorGUI()
        {
            var drawer = PackInspectorSystem.activeDrawer;
            if (drawer != null)
            {
                bool isDebug = EditorBridge.__GetInstanceFrom(this).inspectorMode != InspectorMode.Normal;
            
                bool lastEnabled = GUI.enabled;
                GUI.enabled = true;

                if (PackInspectorSystem.isFolderViewMode)
                    Space(-4);
                
                EditorGUILayout.BeginVertical(paddingStyle);
                {
                    drawer.OnGUI((PhysicalPath)Application.streamingAssetsPath, PackInspectorSystem.activePaths, isDebug);
                    if (drawer.needsApplyRevert)
                        DrawFooter();
                }
                EditorGUILayout.EndVertical();
        
                GUI.enabled = lastEnabled;
            }
            else
                base.OnInspectorGUI();
        }

        public override bool HasPreviewGUI() => PackInspectorSystem.activeDrawer != null ? PackInspectorSystem.activeDrawer.HasPreviewGUI() : base.HasPreviewGUI();

        public override GUIContent GetPreviewTitle() => PackInspectorSystem.activeDrawer != null ? PackInspectorSystem.activeDrawer.GetPreviewTitle() : base.GetPreviewTitle();

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (PackInspectorSystem.activeDrawer != null && PackInspectorSystem.TryGetRelativePathFrom(target, out RuniPath path))
                PackInspectorSystem.activeDrawer.OnPreviewGUI(r, (PhysicalPath)Application.streamingAssetsPath, path, background);
            else
                base.OnPreviewGUI(r, background);
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (PackInspectorSystem.activeDrawer != null && PackInspectorSystem.TryGetRelativePathFrom(target, out RuniPath path))
                PackInspectorSystem.activeDrawer.OnInteractivePreviewGUI(r, (PhysicalPath)Application.streamingAssetsPath, path, background);
            else
                base.OnInteractivePreviewGUI(r, background);
        }

        public override void OnPreviewSettings()
        {
            if (PackInspectorSystem.activeDrawer != null)
                PackInspectorSystem.activeDrawer.OnPreviewSettings();
            else
                base.OnPreviewSettings();
        }

        static void DrawFooter()
        {
            EditorGUI.BeginDisabledGroup(!PackInspectorSystem.activeDrawer?.isDirty ?? true);
            
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(GetTextOrKey("gui.apply")))
                PackInspectorSystem.activeDrawer?.SaveChanges();
            
            if (GUILayout.Button(GetTextOrKey("gui.revert")))
                PackInspectorSystem.activeDrawer?.DiscardChanges();

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }
    }
}