#nullable enable
using UnityEngine.Profiling;

namespace RuniOS.Editor.Windows
{
    public sealed class GenericPanel : ScriptableObject, IControlPanel
    {
        public string label => "control_panel.generic";

        public int sort => 0;

        public bool allowUpdate => true;
        public bool allowUpdateInEditor => true;
        
        public void OnGUI()
        {
            DrawText("control_panel.generic.delta_time", Kernel.deltaTime);
            DrawText("control_panel.generic.smooth_delta_time", Kernel.smoothDeltaTime);
            //DrawText("control_panel.generic.fps_delta_time", Kernel.fpsDeltaTime);
            //DrawText("control_panel.generic.fps_smooth_delta_time", Kernel.fpsSmoothDeltaTime);
            DrawText("control_panel.generic.unscaled_delta_time", Kernel.unscaledDeltaTime);
            DrawText("control_panel.generic.unscaled_smooth_delta_time", Kernel.unscaledSmoothDeltaTime);
            //DrawText("control_panel.generic.fps_unscaled_delta_time", Kernel.fpsUnscaledDeltaTime);
            //DrawText("control_panel.generic.fps_unscaled_smooth_delta_time", Kernel.fpsUnscaledSmoothDeltaTime);

            DrawHLine();

            DrawText("control_panel.generic.fps", Kernel.fps);

            DrawHLine();

            DrawText("control_panel.generic.memory", (Profiler.GetTotalAllocatedMemoryLong() / 1048576f).Round(4));
            DrawText("control_panel.generic.memory.reserved", (Profiler.GetTotalReservedMemoryLong() / 1048576f).Round(4));
            DrawText("control_panel.generic.memory.unused_reserved", (Profiler.GetTotalUnusedReservedMemoryLong() / 1048576f).Round(4));
            DrawText("control_panel.generic.memory.mono", (Profiler.GetMonoUsedSizeLong() / 1048576f).Round(4));

            DrawHLine();

            //DrawText("control_panel.generic.main_thread_id", ThreadTask.mainThreadId);

            DrawHLine();

            DrawText("control_panel.generic.data_path", Application.dataPath);
            DrawText("control_panel.generic.streaming_assets_path", Application.streamingAssetsPath);
            DrawText("control_panel.generic.persistent_data_path", Application.persistentDataPath);
            DrawText("control_panel.generic.temporary_cache_path", Application.temporaryCachePath);
            //DrawText("control_panel.generic.resource_pack_path", Kernel.resourcePackPath);
            //DrawText("control_panel.generic.project_data_path", Kernel.projectSettingPath);

            DrawHLine();

            DrawText("control_panel.generic.company_name", Application.companyName);
            DrawText("control_panel.generic.product_name", Application.productName);

            DrawHLine();

            /*{
                string account_status;
                if (UserAccountManager.currentAccount != null)
                    account_status = "control_panel.generic.account_login";
                else
                    account_status = "control_panel.generic.account_logout";

                DrawText("control_panel.generic.account_status", TryGetText(account_status));

                if (UserAccountManager.currentAccount != null)
                {
                    DrawText("control_panel.generic.account_name", UserAccountManager.currentAccount.name);
                    DrawText("control_panel.generic.account_path", UserAccountManager.currentAccount.path);
                    DrawText("control_panel.generic.account_hashed_password", UserAccountManager.currentAccount.hashedPassword);
                }
            }*/

            DrawHLine();

            DrawText("control_panel.generic.version", Application.version);
            DrawText("control_panel.generic.unity_version", Application.unityVersion);

            DrawHLine();

            DrawText("control_panel.generic.platform", Application.platform);

            DrawHLine();

            DrawText("control_panel.generic.operating_system", SystemInfo.operatingSystem);

            DrawHLine();

            DrawText("control_panel.generic.device_model", SystemInfo.deviceModel);
            DrawText("control_panel.generic.device_name", SystemInfo.deviceName);

            DrawHLine();

            DrawText("control_panel.generic.internet_reachability", Application.internetReachability);
            DrawText("control_panel.generic.battery_status", SystemInfo.batteryStatus);

            DrawHLine();

            DrawText("control_panel.generic.processor_type", SystemInfo.processorType);
            DrawText("control_panel.generic.processor_frequency", SystemInfo.processorFrequency);
            DrawText("control_panel.generic.processor_count", SystemInfo.processorCount);

            DrawHLine();

            DrawText("control_panel.generic.graphics_device_name", SystemInfo.graphicsDeviceName);
            DrawText("control_panel.generic.graphics_memory_size", SystemInfo.graphicsMemorySize);

            DrawHLine();

            DrawText("control_panel.generic.system_memory_size", SystemInfo.systemMemorySize);

            DrawHLine();

            Time.timeScale = EditorGUILayout.FloatField(GetTextOrKey("control_panel.generic.game_speed"), Time.timeScale).Clamp(0, 100);
        }
        
        static void DrawText(string key, object value) => GUILayout.Label(GetTextOrKey(key) + " - " + RichNumberMSpace(value), RuniStyles.richLabel);
    }
}