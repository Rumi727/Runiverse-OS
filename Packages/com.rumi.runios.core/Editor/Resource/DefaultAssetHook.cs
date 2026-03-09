#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.APIMarshal.UnityEditor;

namespace RuniOS.Editor.Resource
{
    [CustomEditor(typeof(DefaultAsset))]
    [CanEditMultipleObjects]
    class DefaultAssetHook : EditorMarshal
    {
        public static GUIStyle largeLabel => _largeLabel ??= new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        static GUIStyle? _largeLabel;
        
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
                subscribedDrawer.onDirtyStateChanged += SyncDirtyState;
                SyncDirtyState();
            }
        }

        void UnsubscribeDrawer()
        {
            if (subscribedDrawer != null)
            {
                subscribedDrawer.onDirtyStateChanged -= SyncDirtyState;
                subscribedDrawer = null;
            }
        }

        void SyncDirtyState()
        {
            if (subscribedDrawer != null)
            {
                hasUnsavedChanges = subscribedDrawer.isDirty;
                saveChangesMessage = "asdf";
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
                    if (drawer.title != null)
                        GUILayout.Label(GetTextOrKey(drawer.title), largeLabel);
                    
                    drawer.OnGUI(PackInspectorSystem.activePaths, isDebug);
                    if (drawer.needsApplyRevert)
                        DrawFooter();
                }
                EditorGUILayout.EndVertical();
        
                GUI.enabled = lastEnabled;
            }
            else
                base.OnInspectorGUI();
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