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
                if (targetDrawer != null)
                {
                    if (targetDrawer.targetTitle != null)
                        return targetDrawer.targetTitle;

                    string typeName = targetDrawer.targetTypeName;
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
        
        PackDrawer? targetDrawer;

        void OnEnable()
        {
            targetDrawer = PackInspectorSystem.CreateDrawer(targets);

            if (targetDrawer != null)
            {
                targetDrawer.OnEnable();

                targetDrawer.repaintAction += Repaint;
                targetDrawer.onDirtyStateChanged += SyncDirtyState;

                SyncDirtyState();
            }
        }

        void OnDisable()
        {
            if (targetDrawer != null)
            {
                targetDrawer.OnDisable();

                targetDrawer.repaintAction -= Repaint;
                targetDrawer.onDirtyStateChanged -= SyncDirtyState;

                targetDrawer = null;
            }
        }

        void SyncDirtyState()
        {
            if (targetDrawer != null)
            {
                hasUnsavedChanges = targetDrawer.isDirty;
                saveChangesMessage = "파일의 임포트 설정 적용되지 않음";
            }
        }

        public override void SaveChanges()
        {
            targetDrawer?.SaveChanges();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            targetDrawer?.DiscardChanges();
            base.DiscardChanges();
        }

        public override void OnInspectorGUI()
        {
            if (targetDrawer != null)
            {
                bool isDebug = EditorBridge.__GetInstanceFrom(this).inspectorMode != InspectorMode.Normal;
            
                bool lastEnabled = GUI.enabled;
                GUI.enabled = true;

                if (PackInspectorSystem.isFolderViewMode)
                    Space(-4);
                
                EditorGUILayout.BeginVertical(paddingStyle);
                {
                    targetDrawer.OnGUI(isDebug);
                    if (targetDrawer.needsApplyRevert)
                        DrawFooter();
                }
                EditorGUILayout.EndVertical();
        
                GUI.enabled = lastEnabled;
            }
            else
                base.OnInspectorGUI();
        }

        public override Texture2D? RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (targetDrawer != null && PackInspectorSystem.TryGetRelativePathFrom(target, out PhysicalPath rootPath, out RuniPath path))
                return targetDrawer.RenderStaticPreview(rootPath, path, width, height);
            else
                return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        public override bool HasPreviewGUI() => targetDrawer?.HasPreviewGUI() ?? base.HasPreviewGUI();

        public override GUIContent GetPreviewTitle() => targetDrawer != null ? targetDrawer.GetPreviewTitle() : base.GetPreviewTitle();

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (targetDrawer != null && PackInspectorSystem.TryGetRelativePathFrom(target, out PhysicalPath rootPath, out RuniPath path))
                targetDrawer.OnPreviewGUI(r, rootPath, path, background);
            else
                base.OnPreviewGUI(r, background);
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (targetDrawer != null && PackInspectorSystem.TryGetRelativePathFrom(target, out PhysicalPath rootPath, out RuniPath path))
                targetDrawer.OnInteractivePreviewGUI(r, rootPath, path, background);
            else
                base.OnInteractivePreviewGUI(r, background);
        }

        public override void OnPreviewSettings()
        {
            if (targetDrawer != null)
                targetDrawer.OnPreviewSettings();
            else
                base.OnPreviewSettings();
        }

        void DrawFooter()
        {
            EditorGUI.BeginDisabledGroup(!targetDrawer?.isDirty ?? true);
            
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(GetTextOrKey("gui.apply")))
                targetDrawer?.SaveChanges();
            
            if (GUILayout.Button(GetTextOrKey("gui.revert")))
                targetDrawer?.DiscardChanges();

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }
    }
}