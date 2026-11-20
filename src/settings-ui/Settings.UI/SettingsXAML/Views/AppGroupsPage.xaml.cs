// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

using Microsoft.PowerToys.Settings.UI.Helpers;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.ViewModels;
using Microsoft.UI.Xaml;

namespace Microsoft.PowerToys.Settings.UI.Views
{
    public sealed partial class AppGroupsPage : NavigablePage, IRefreshablePage
    {
        private AppGroupsViewModel ViewModel { get; }

        public AppGroupsPage()
        {
            var settingsUtils = SettingsUtils.Default;
            ViewModel = new AppGroupsViewModel(
                settingsUtils,
                SettingsRepository<GeneralSettings>.GetInstance(settingsUtils),
                SettingsRepository<AppGroupsSettings>.GetInstance(settingsUtils),
                ShellPage.SendDefaultIPCMessage);
            InitializeComponent();
        }

        public void RefreshEnabledState() => ViewModel.RefreshEnabledState();

        private void AddApps_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not AppGroupViewModel group)
            {
                return;
            }

            string selectedApp = PickExecutable();
            if (!string.IsNullOrEmpty(selectedApp))
            {
                group.AddApp(selectedApp);
            }
        }

        private static string PickExecutable()
        {
            // The Win32 dialog is used instead of FileOpenPicker because the Settings process
            // may run elevated, where FileOpenPicker is not available.
            var openFileName = new OpenFileName
            {
                Filter = "Applications\0*.exe\0All files\0*.*\0",
                File = new string(new char[260 * 2]),
                Title = ResourceLoaderInstance.ResourceLoader.GetString("AppGroups_SelectAppDialogTitle"),
                Flags = (int)OpenFileNameFlags.OFN_NOCHANGEDIR,
            };

            openFileName.StructSize = Marshal.SizeOf(openFileName);
            openFileName.MaxFile = openFileName.File.Length;
            openFileName.Hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.GetSettingsWindow());

            return NativeMethods.GetOpenFileName(openFileName) ? openFileName.File : null;
        }
    }
}
