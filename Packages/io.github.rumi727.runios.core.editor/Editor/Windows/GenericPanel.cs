#nullable enable
using RuniOS.Texts;
using RuniOS.Texts.Styles.TMPro;
using UnityEngine.Profiling;

namespace RuniOS.Editor.Windows
{
    public sealed class GenericPanel : ScriptableObject, IControlPanel
    {
        public string label => "runios-editor:control_panel.generic";

        public int sort => 0;

        public bool allowUpdate => true;
        public bool allowUpdateInEditor => true;
        
        public void OnGUI()
        {
            DrawText("runios-editor:control_panel.generic.delta_time", Kernel.deltaTime);
            DrawText("runios-editor:control_panel.generic.smooth_delta_time", Kernel.smoothDeltaTime);
            //DrawText("runios-editor:control_panel.generic.fps_delta_time", Kernel.fpsDeltaTime);
            //DrawText("runios-editor:control_panel.generic.fps_smooth_delta_time", Kernel.fpsSmoothDeltaTime);
            DrawText("runios-editor:control_panel.generic.unscaled_delta_time", Kernel.unscaledDeltaTime);
            DrawText("runios-editor:control_panel.generic.unscaled_smooth_delta_time", Kernel.unscaledSmoothDeltaTime);
            //DrawText("runios-editor:control_panel.generic.fps_unscaled_delta_time", Kernel.fpsUnscaledDeltaTime);
            //DrawText("runios-editor:control_panel.generic.fps_unscaled_smooth_delta_time", Kernel.fpsUnscaledSmoothDeltaTime);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.fps", Kernel.fps);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.memory", (Profiler.GetTotalAllocatedMemoryLong() / 1048576f).Round(4));
            DrawText("runios-editor:control_panel.generic.memory.reserved", (Profiler.GetTotalReservedMemoryLong() / 1048576f).Round(4));
            DrawText("runios-editor:control_panel.generic.memory.unused_reserved", (Profiler.GetTotalUnusedReservedMemoryLong() / 1048576f).Round(4));
            DrawText("runios-editor:control_panel.generic.memory.mono", (Profiler.GetMonoUsedSizeLong() / 1048576f).Round(4));

            DrawHLine();

            //DrawText("runios-editor:control_panel.generic.main_thread_id", ThreadTask.mainThreadId);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.data_path", Application.dataPath);
            DrawText("runios-editor:control_panel.generic.streaming_assets_path", Application.streamingAssetsPath);
            DrawText("runios-editor:control_panel.generic.persistent_data_path", Application.persistentDataPath);
            DrawText("runios-editor:control_panel.generic.temporary_cache_path", Application.temporaryCachePath);
            //DrawText("runios-editor:control_panel.generic.resource_pack_path", Kernel.resourcePackPath);
            //DrawText("runios-editor:control_panel.generic.project_data_path", Kernel.projectSettingPath);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.company_name", Application.companyName);
            DrawText("runios-editor:control_panel.generic.product_name", Application.productName);

            DrawHLine();

            /*{
                string account_status;
                if (UserAccountManager.currentAccount != null)
                    account_status = "runios-editor:control_panel.generic.account_login";
                else
                    account_status = "runios-editor:control_panel.generic.account_logout";

                DrawText("runios-editor:control_panel.generic.account_status", TryGetText(account_status));

                if (UserAccountManager.currentAccount != null)
                {
                    DrawText("runios-editor:control_panel.generic.account_name", UserAccountManager.currentAccount.name);
                    DrawText("runios-editor:control_panel.generic.account_path", UserAccountManager.currentAccount.path);
                    DrawText("runios-editor:control_panel.generic.account_hashed_password", UserAccountManager.currentAccount.hashedPassword);
                }
            }*/

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.version", Application.version);
            DrawText("runios-editor:control_panel.generic.unity_version", Application.unityVersion);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.platform", Application.platform);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.operating_system", SystemInfo.operatingSystem);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.device_model", SystemInfo.deviceModel);
            DrawText("runios-editor:control_panel.generic.device_name", SystemInfo.deviceName);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.internet_reachability", Application.internetReachability);
            DrawText("runios-editor:control_panel.generic.battery_status", SystemInfo.batteryStatus);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.processor_type", SystemInfo.processorType);
            DrawText("runios-editor:control_panel.generic.processor_frequency", SystemInfo.processorFrequency);
            DrawText("runios-editor:control_panel.generic.processor_count", SystemInfo.processorCount);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.graphics_device_name", SystemInfo.graphicsDeviceName);
            DrawText("runios-editor:control_panel.generic.graphics_memory_size", SystemInfo.graphicsMemorySize);
            DrawText("runios-editor:control_panel.generic.graphics_device_type", SystemInfo.graphicsDeviceType);
            DrawText("runios-editor:control_panel.generic.graphics_multi_threaded", SystemInfo.graphicsMultiThreaded);
            DrawText("runios-editor:control_panel.generic.rendering_threading_mode", SystemInfo.renderingThreadingMode);

            DrawHLine();

            DrawText("runios-editor:control_panel.generic.system_memory_size", SystemInfo.systemMemorySize);

            DrawHLine();

            Time.timeScale = EditorGUILayout.FloatField(GetTextOrKey("runios-editor:control_panel.generic.game_speed"), Time.timeScale).Clamp(0, 100);
        }

        static readonly LocalizationText labelText = Text.Local();
        static readonly ValueText<object?> valueText = Text.Value();
        static readonly GroupText groupText = Text.Group($"{labelText} - {valueText}");
        static void DrawText(string key, object value)
        {
            labelText.identifier = key;
            valueText.value = value;
            if (value.GetType().IsNumeric)
                valueText.SetStyle(TMPStyles.monoSpacing, 7.6f);
            else
                valueText.UnsetStyle(TMPStyles.monoSpacing);

            GUILayout.Label(groupText.ToRichText(), RuniStyles.richLabel);
        }
    }
}