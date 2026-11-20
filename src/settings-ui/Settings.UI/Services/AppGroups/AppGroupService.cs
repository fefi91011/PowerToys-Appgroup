// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.PowerToys.Settings.UI.Library;

namespace Microsoft.PowerToys.Settings.UI.Services.AppGroups
{
    /// <summary>
    /// Places app groups on the desktop and on the taskbar.
    /// </summary>
    /// <remarks>
    /// Desktop groups are plain folders with shortcuts, so they survive explorer restarts.
    /// On Windows 10, taskbar pins are written as shortcuts into the per-user
    /// "User Pinned\TaskBar" folder and become visible after Explorer restarts.
    /// Windows 11 keeps the authoritative taskbar layout in the Taskband registry key and
    /// ignores shortcuts that are only copied into that folder, so automatic pinning is
    /// skipped there (see <see cref="IsAutomaticTaskbarPinningSupported"/>).
    /// </remarks>
    internal sealed class AppGroupService
    {
        private const string DesktopMarkerFileName = ".powertoys-appgroups";

        private const string TaskbarShortcutPrefix = "PowerToys AppGroups - ";

        private const string FallbackGroupName = "App Group";

        private const string FallbackShortcutName = "App";

        /// <summary>
        /// Gets a value indicating whether dropping shortcuts into the per-user pinned folder
        /// actually pins them. Windows 11 (build 22000 and later) ignores those files because
        /// the taskbar layout lives in the Taskband registry key, and Microsoft provides no
        /// supported API for pinning items programmatically.
        /// </summary>
        internal static bool IsAutomaticTaskbarPinningSupported { get; } = !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

        internal AppGroupApplyResult ApplyAll(IEnumerable<AppGroup> groups, bool restartExplorer)
        {
            var result = new AppGroupApplyResult();

            foreach (AppGroup group in groups ?? Enumerable.Empty<AppGroup>())
            {
                ApplyGroupToResult(group, result);
            }

            return Finish(result, restartExplorer);
        }

        internal AppGroupApplyResult ApplyGroup(AppGroup group, bool restartExplorer)
        {
            return ApplyAll(new[] { group }, restartExplorer);
        }

        /// <summary>
        /// Removes everything that was placed for a deleted group.
        /// </summary>
        internal AppGroupApplyResult Remove(AppGroup group, bool restartExplorer)
        {
            var result = new AppGroupApplyResult();
            if (group == null)
            {
                return result;
            }

            try
            {
                // Remove the current name as well as the name from the last apply, in case
                // the group was renamed without being applied again.
                string folderName = GetGroupFolderName(group);
                RemoveArtifacts(folderName, result);
                RemovePreviousArtifacts(group, folderName, result);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{group.Name}: {ex.Message}");
            }

            return Finish(result, restartExplorer);
        }

        /// <summary>
        /// Restarts Explorer so the shell rebuilds the taskbar and shows newly pinned items.
        /// Open File Explorer windows are closed by this operation.
        /// </summary>
        private static void RestartExplorer()
        {
            foreach (Process process in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                    // Explorer may already be gone or may refuse to terminate; starting a new
                    // instance below still refreshes the shell in that case.
                }
                finally
                {
                    process.Dispose();
                }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true,
            });
        }

        private static AppGroupApplyResult Finish(AppGroupApplyResult result, bool restartExplorer)
        {
            // Restarting Explorer only helps on Windows 10, where the shell builds its pins from
            // the shortcut files. On Windows 11 the restart cannot make the pins appear and would
            // only close the user's File Explorer windows.
            if (result.TaskbarChanged && restartExplorer && IsAutomaticTaskbarPinningSupported)
            {
                try
                {
                    RestartExplorer();
                }
                catch (Exception ex)
                {
                    result.Errors.Add(ex.Message);
                }
            }

            return result;
        }

        private static void ApplyGroupToResult(AppGroup group, AppGroupApplyResult result)
        {
            if (group == null)
            {
                return;
            }

            string folderName = GetGroupFolderName(group);
            RemovePreviousArtifacts(group, folderName, result);

            ApplyDesktop(group, folderName, result);
            ApplyTaskbar(group, folderName, result);
            group.LastAppliedName = folderName;
        }

        private static void RemoveArtifacts(string groupFolderName, AppGroupApplyResult result)
        {
            // The name can come from the settings file (LastAppliedName), so never use it as a
            // raw path: sanitize it and bail out when nothing usable is left.
            string safeName = SanitizeFileName(groupFolderName);
            if (string.IsNullOrWhiteSpace(safeName))
            {
                return;
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrEmpty(desktopPath))
            {
                RemoveDesktopFolder(Path.Combine(desktopPath, safeName));
            }

            RemoveTaskbarShortcuts(safeName, result);
        }

        /// <summary>
        /// Removes the artifacts of the name used by the previous apply, so a renamed group does
        /// not leave its old folder and taskbar shortcuts behind.
        /// </summary>
        private static void RemovePreviousArtifacts(AppGroup group, string folderName, AppGroupApplyResult result)
        {
            try
            {
                if (!string.IsNullOrEmpty(group.LastAppliedName) &&
                    !string.Equals(group.LastAppliedName, folderName, StringComparison.OrdinalIgnoreCase))
                {
                    RemoveArtifacts(group.LastAppliedName, result);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{group.Name}: {ex.Message}");
            }
        }

        private static void ApplyDesktop(AppGroup group, string folderName, AppGroupApplyResult result)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(desktopPath))
                {
                    return;
                }

                string folderPath = Path.Combine(desktopPath, folderName);

                if (!group.ShowOnDesktop)
                {
                    RemoveDesktopFolder(folderPath);
                    return;
                }

                // Never take over an existing folder that this module did not create: writing the
                // marker and deleting the shortcuts below would destroy the user's own files.
                if (Directory.Exists(folderPath) && !File.Exists(Path.Combine(folderPath, DesktopMarkerFileName)))
                {
                    throw new IOException($"The desktop folder \"{folderPath}\" already exists and was not created by App Groups.");
                }

                Directory.CreateDirectory(folderPath);
                File.WriteAllText(Path.Combine(folderPath, DesktopMarkerFileName), group.Id ?? string.Empty);

                // Rebuild the shortcuts so removed apps disappear from the desktop.
                foreach (string existingShortcut in Directory.GetFiles(folderPath, "*.lnk"))
                {
                    File.Delete(existingShortcut);
                }

                CreateShortcuts(folderPath, string.Empty, group.Apps);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{group.Name}: {ex.Message}");
            }
        }

        private static void RemoveDesktopFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                return;
            }

            // Only remove folders this module created, never a user folder that happens to
            // share the group name.
            if (!File.Exists(Path.Combine(folderPath, DesktopMarkerFileName)))
            {
                return;
            }

            Directory.Delete(folderPath, true);
        }

        private static void ApplyTaskbar(AppGroup group, string folderName, AppGroupApplyResult result)
        {
            try
            {
                string pinnedFolder = GetTaskbarPinnedFolder();
                if (string.IsNullOrEmpty(pinnedFolder))
                {
                    return;
                }

                string prefix = TaskbarShortcutPrefix + folderName + " - ";
                RemoveTaskbarShortcuts(folderName, result);

                if (!group.PinToTaskbar)
                {
                    return;
                }

                if (group.Apps?.Any(app => app != null && !string.IsNullOrWhiteSpace(app.Path)) != true)
                {
                    return;
                }

                // Windows 11 ignores shortcut files that are only copied into this folder, so do
                // not leave misleading files behind and do not claim that the apps were pinned.
                if (!IsAutomaticTaskbarPinningSupported)
                {
                    result.TaskbarPinningSkipped = true;
                    return;
                }

                Directory.CreateDirectory(pinnedFolder);

                bool placedAny = CreateShortcuts(pinnedFolder, prefix, group.Apps);
                result.TaskbarChanged = placedAny;
                result.TaskbarShortcutsPlaced = placedAny;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{group.Name}: {ex.Message}");
            }
        }

        private static void RemoveTaskbarShortcuts(string groupFolderName, AppGroupApplyResult result)
        {
            string pinnedFolder = GetTaskbarPinnedFolder();
            if (string.IsNullOrEmpty(pinnedFolder) || !Directory.Exists(pinnedFolder))
            {
                return;
            }

            string prefix = TaskbarShortcutPrefix + groupFolderName + " - ";
            foreach (string existingShortcut in Directory.GetFiles(pinnedFolder, "*.lnk"))
            {
                if (Path.GetFileName(existingShortcut).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(existingShortcut);
                    result.TaskbarChanged = true;
                }
            }
        }

        private static string GetTaskbarPinnedFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(appData))
            {
                return null;
            }

            return Path.Combine(appData, "Microsoft", "Internet Explorer", "Quick Launch", "User Pinned", "TaskBar");
        }

        private static string GetGroupFolderName(AppGroup group)
        {
            string name = SanitizeFileName(group.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.IsNullOrEmpty(group.Id)
                    ? FallbackGroupName
                    : $"{FallbackGroupName} {group.Id.Substring(0, Math.Min(8, group.Id.Length))}";
            }

            return name;
        }

        private static bool CreateShortcuts(string folderPath, string namePrefix, IEnumerable<AppGroupApp> apps)
        {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool created = false;
            foreach (AppGroupApp app in apps ?? Enumerable.Empty<AppGroupApp>())
            {
                if (app == null || string.IsNullOrWhiteSpace(app.Path))
                {
                    continue;
                }

                string shortcutName = GetUniqueShortcutName(usedNames, app);
                ShellLinkHelper.CreateShortcut(Path.Combine(folderPath, namePrefix + shortcutName + ".lnk"), app.Path);
                created = true;
            }

            return created;
        }

        private static string GetUniqueShortcutName(HashSet<string> usedNames, AppGroupApp app)
        {
            string baseName = SanitizeFileName(app.Name);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(app.Path));
            }

            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = FallbackShortcutName;
            }

            string uniqueName = baseName;
            int suffix = 2;
            while (!usedNames.Add(uniqueName))
            {
                uniqueName = $"{baseName} ({suffix})";
                suffix++;
            }

            return uniqueName;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            var builder = new System.Text.StringBuilder(name.Length);
            foreach (char character in name.Trim())
            {
                builder.Append(Array.IndexOf(invalidChars, character) >= 0 ? '_' : character);
            }

            string sanitized = builder.ToString().Trim().TrimEnd('.');
            return sanitized;
        }
    }
}
