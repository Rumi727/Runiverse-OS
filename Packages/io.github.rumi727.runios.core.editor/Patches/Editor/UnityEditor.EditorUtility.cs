#nullable enable
#if UNITY_EDITOR_LINUX
using HarmonyLib;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RuniOS.Editor.Patches
{
    public static partial class Patches
    {
        public static partial class UnityEditorPatch
        {
            /// <summary>
            /// Makes the <see cref="EditorUtility"/> file panels open through the XDG desktop portal (<c>org.freedesktop.portal.FileChooser</c>), so the desktop environment's own file dialog is shown.<br/>
            /// The panels use GTK's <c>GtkFileChooserNative</c>, which falls back to the GTK file chooser when no portal is available.
            /// <br/><br/>
            /// <see cref="EditorUtility"/>의 파일 패널이 XDG 데스크톱 포탈(<c>org.freedesktop.portal.FileChooser</c>)을 통해 열리도록 하여 데스크톱 환경의 파일 대화 상자가 표시되게 합니다.<br/>
            /// 패널은 GTK의 <c>GtkFileChooserNative</c>를 사용하며, 포탈을 사용할 수 없으면 GTK 파일 선택기로 대체됩니다.
            /// </summary>
            [HarmonyPatch(typeof(EditorUtility))]
            public static class EditorUtilityPatch
            {
                [HarmonyPrefix]
                [HarmonyPatch(nameof(EditorUtility.OpenFilePanel))]
                public static bool OpenFilePanelPrefix(string? title, string? directory, string? extension, ref string __result)
                {
                    __result = ShowDialog(title, actionOpen, directory, null, FiltersFromExtensions(extension));
                    return false;
                }

                [HarmonyPrefix]
                [HarmonyPatch("Internal_OpenFilePanelWithFilters")]
                public static bool OpenFilePanelWithFiltersPrefix(string? title, string? directory, string[]? filters, ref string __result)
                {
                    __result = ShowDialog(title, actionOpen, directory, null, FiltersFromPairs(filters));
                    return false;
                }

                [HarmonyPrefix]
                [HarmonyPatch(nameof(EditorUtility.OpenFolderPanel))]
                public static bool OpenFolderPanelPrefix(string? title, string? folder, ref string __result)
                {
                    __result = ShowDialog(title, actionSelectFolder, folder, null, null);
                    return false;
                }

                [HarmonyPrefix]
                [HarmonyPatch(nameof(EditorUtility.SaveFolderPanel))]
                public static bool SaveFolderPanelPrefix(string? title, string? folder, ref string __result)
                {
                    __result = ShowDialog(title, actionSelectFolder, folder, null, null);
                    return false;
                }

                [HarmonyPrefix]
                [HarmonyPatch(nameof(EditorUtility.SaveFilePanel))]
                public static bool SaveFilePanelPrefix(string? title, string? directory, string? defaultName, string? extension, ref string __result)
                {
                    __result = SaveFile(title, directory, defaultName, extension);
                    return false;
                }

                [HarmonyPrefix]
                [HarmonyPatch("SaveBuildPanel")]
                public static bool SaveBuildPanelPrefix(string? title, string? directory, string? defaultName, string? extension, out bool updateExistingBuild, ref string __result)
                {
                    updateExistingBuild = false;

                    // Targets without an extension (e.g. WebGL) build into a folder.
                    __result = string.IsNullOrEmpty(extension)
                        ? ShowDialog(title, actionSelectFolder, directory, null, null)
                        : SaveFile(title, directory, defaultName, extension);
                    return false;
                }

                [HarmonyPrefix]
                [HarmonyPatch("Internal_SaveFilePanelInProject")]
                public static bool SaveFilePanelInProjectPrefix(string? title, string? defaultName, string? extension, string? path, ref string __result)
                {
                    string assetsRoot = Path.GetFullPath(Application.dataPath);
                    string? directory = Path.Combine(assetsRoot, "..", string.IsNullOrEmpty(path) ? "Assets" : path);

                    // Same contract as the native panel: an "Assets/..." path, or empty when cancelled.
                    while (true)
                    {
                        string chosen = SaveFile(title, directory, defaultName, extension);
                        if (chosen.Length == 0)
                        {
                            __result = string.Empty;
                            return false;
                        }

                        chosen = Path.GetFullPath(chosen);
                        if (chosen.StartsWith(assetsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                        {
                            __result = "Assets" + chosen.Substring(assetsRoot.Length);
                            return false;
                        }

                        EditorUtility.DisplayDialog(title ?? string.Empty, "The file must be saved inside the project's Assets folder.", "OK");
                        directory = Path.GetDirectoryName(chosen);
                        defaultName = Path.GetFileName(chosen);
                    }
                }

                static string SaveFile(string? title, string? directory, string? defaultName, string? extension)
                {
                    string path = ShowDialog(title, actionSave, directory, defaultName, FiltersFromExtensions(extension));
                    if (path.Length == 0 || extension == null || extension.Length == 0 || Path.HasExtension(path))
                        return path;

                    return path + "." + extension.Split(',')[0].Trim().TrimStart('*', '.');
                }

                static List<(string name, string[] patterns)>? FiltersFromExtensions(string? extensions)
                {
                    if (extensions == null || string.IsNullOrWhiteSpace(extensions))
                        return null;

                    string[] patterns = Patterns(extensions);
                    return new List<(string name, string[] patterns)> { (string.Join(" ", patterns), patterns), ("All Files", new[] { "*" }) };
                }

                static List<(string name, string[] patterns)>? FiltersFromPairs(string[]? filters)
                {
                    if (filters == null || filters.Length < 2)
                        return null;

                    List<(string name, string[] patterns)> result = new List<(string name, string[] patterns)>();
                    for (int i = 0; i + 1 < filters.Length; i += 2)
                        result.Add((filters[i], Patterns(filters[i + 1])));

                    return result;
                }

                static string[] Patterns(string extensions) => extensions
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(static x => x.Trim().TrimStart('*', '.'))
                    .Select(static x => x.Length == 0 ? "*" : "*." + x)
                    .ToArray();

                // Runs a nested GTK main loop until the dialog closes, like the native panels do.
                // Returns an empty string when the user cancels.
                static string ShowDialog(string? title, int action, string? directory, string? currentName, List<(string name, string[] patterns)>? filters)
                {
                    // GtkFileChooserNative only goes through the portal when this is set. An explicit value from the user is kept.
                    setenv("GTK_USE_PORTAL", "1", 0);

                    IntPtr dialog = gtk_file_chooser_native_new(title, ActiveWindow(), action, IntPtr.Zero, IntPtr.Zero);
                    try
                    {
                        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                            gtk_file_chooser_set_current_folder(dialog, Path.GetFullPath(directory));
                        if (!string.IsNullOrEmpty(currentName))
                            gtk_file_chooser_set_current_name(dialog, currentName!);
                        if (action == actionSave)
                            gtk_file_chooser_set_do_overwrite_confirmation(dialog, true);

                        if (filters != null)
                        {
                            foreach ((string name, string[] patterns) in filters)
                            {
                                IntPtr filter = gtk_file_filter_new();
                                gtk_file_filter_set_name(filter, name);
                                foreach (string pattern in patterns)
                                    gtk_file_filter_add_pattern(filter, pattern);

                                gtk_file_chooser_add_filter(dialog, filter);
                            }
                        }

                        if (gtk_native_dialog_run(dialog) != responseAccept)
                            return string.Empty;

                        IntPtr fileName = gtk_file_chooser_get_filename(dialog);
                        try
                        {
                            return Utf8ToString(fileName);
                        }
                        finally
                        {
                            g_free(fileName);
                        }
                    }
                    finally
                    {
                        g_object_unref(dialog);
                    }
                }

                // Parenting the dialog to the focused editor window makes the portal dialog modal to it.
                static IntPtr ActiveWindow()
                {
                    IntPtr list = gtk_window_list_toplevels();
                    try
                    {
                        // GList is { data, next, prev }.
                        for (IntPtr node = list; node != IntPtr.Zero; node = Marshal.ReadIntPtr(node, IntPtr.Size))
                        {
                            IntPtr window = Marshal.ReadIntPtr(node);
                            if (gtk_window_is_active(window))
                                return window;
                        }

                        return IntPtr.Zero;
                    }
                    finally
                    {
                        g_list_free(list);
                    }
                }

                static string Utf8ToString(IntPtr pointer)
                {
                    if (pointer == IntPtr.Zero)
                        return string.Empty;

                    int length = 0;
                    while (Marshal.ReadByte(pointer, length) != 0)
                        length++;

                    byte[] bytes = new byte[length];
                    Marshal.Copy(pointer, bytes, 0, length);
                    return Encoding.UTF8.GetString(bytes);
                }

                const int actionOpen = 0;
                const int actionSave = 1;
                const int actionSelectFolder = 2;
                const int responseAccept = -3;

                const string libc = "libc.so.6";
                const string glib = "libglib-2.0.so.0";
                const string gobject = "libgobject-2.0.so.0";
                const string gtk = "libgtk-3.so.0";

                [DllImport(libc)] static extern int setenv(string name, string value, int overwrite);

                [DllImport(glib)] static extern void g_free(IntPtr pointer);
                [DllImport(glib)] static extern void g_list_free(IntPtr list);
                [DllImport(gobject)] static extern void g_object_unref(IntPtr instance);

                [DllImport(gtk)] static extern IntPtr gtk_window_list_toplevels();
                [DllImport(gtk)] static extern bool gtk_window_is_active(IntPtr window);

                [DllImport(gtk)] static extern IntPtr gtk_file_chooser_native_new([MarshalAs(UnmanagedType.LPUTF8Str)] string? title, IntPtr parent, int action, IntPtr acceptLabel, IntPtr cancelLabel);
                [DllImport(gtk)] static extern int gtk_native_dialog_run(IntPtr dialog);

                [DllImport(gtk)] static extern bool gtk_file_chooser_set_current_folder(IntPtr chooser, [MarshalAs(UnmanagedType.LPUTF8Str)] string fileName);
                [DllImport(gtk)] static extern void gtk_file_chooser_set_current_name(IntPtr chooser, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
                [DllImport(gtk)] static extern void gtk_file_chooser_set_do_overwrite_confirmation(IntPtr chooser, bool doOverwriteConfirmation);
                [DllImport(gtk)] static extern void gtk_file_chooser_add_filter(IntPtr chooser, IntPtr filter);
                [DllImport(gtk)] static extern IntPtr gtk_file_chooser_get_filename(IntPtr chooser);

                [DllImport(gtk)] static extern IntPtr gtk_file_filter_new();
                [DllImport(gtk)] static extern void gtk_file_filter_set_name(IntPtr filter, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
                [DllImport(gtk)] static extern void gtk_file_filter_add_pattern(IntPtr filter, [MarshalAs(UnmanagedType.LPUTF8Str)] string pattern);
            }
        }
    }
}
#endif
