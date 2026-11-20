// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using Microsoft.PowerToys.Settings.UI.Helpers;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.Library.Interfaces;
using Microsoft.PowerToys.Settings.UI.Library.ViewModels.Commands;
using Microsoft.PowerToys.Settings.UI.Services.AppGroups;

namespace Microsoft.PowerToys.Settings.UI.ViewModels
{
    /// <summary>
    /// View model for the App Groups page. Groups are stored in the module settings file and
    /// applied to the desktop and taskbar by <see cref="AppGroupService"/>.
    /// </summary>
    public partial class AppGroupsViewModel : PageViewModelBase
    {
        protected override string ModuleName => AppGroupsSettings.ModuleName;

        private readonly SettingsUtils _settingsUtils;

        private readonly GeneralSettings _generalSettingsConfig;

        private readonly AppGroupsSettings _settings;

        private readonly AppGroupService _service = new AppGroupService();

        private readonly Func<string, int> _sendConfigMessage;

        public AppGroupsViewModel(
            SettingsUtils settingsUtils,
            ISettingsRepository<GeneralSettings> settingsRepository,
            ISettingsRepository<AppGroupsSettings> moduleSettingsRepository,
            Func<string, int> ipcMSGCallBackFunc)
        {
            ArgumentNullException.ThrowIfNull(settingsUtils);
            ArgumentNullException.ThrowIfNull(settingsRepository);
            ArgumentNullException.ThrowIfNull(moduleSettingsRepository);

            _settingsUtils = settingsUtils;
            _generalSettingsConfig = settingsRepository.SettingsConfig;
            _settings = moduleSettingsRepository.SettingsConfig ?? new AppGroupsSettings();
            _settings.Properties ??= new AppGroupsProperties();
            _settings.Properties.Groups ??= new List<AppGroup>();

            _sendConfigMessage = ipcMSGCallBackFunc;

            _isEnabled = _generalSettingsConfig.Enabled.AppGroups;

            foreach (AppGroup group in _settings.Properties.Groups)
            {
                if (group != null)
                {
                    Groups.Add(new AppGroupViewModel(group, this));
                }
            }

            AddGroupCommand = new RelayCommand<object>(_ => AddGroup());
            ApplyAllCommand = new RelayCommand<object>(_ => ApplyAllGroups());
        }

        public ObservableCollection<AppGroupViewModel> Groups { get; } = new ObservableCollection<AppGroupViewModel>();

        public ICommand AddGroupCommand { get; }

        public ICommand ApplyAllCommand { get; }

        public bool IsEnabled
        {
            get => _isEnabled;

            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    _generalSettingsConfig.Enabled.AppGroups = value;
                    OutGoingGeneralSettings snd = new OutGoingGeneralSettings(_generalSettingsConfig);
                    _sendConfigMessage?.Invoke(snd.ToString());
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public bool RestartExplorerAfterApplying
        {
            get => _settings.Properties.RestartExplorerAfterApplying?.Value ?? true;

            set
            {
                if ((_settings.Properties.RestartExplorerAfterApplying?.Value ?? true) != value)
                {
                    _settings.Properties.RestartExplorerAfterApplying ??= new BoolProperty(true);
                    _settings.Properties.RestartExplorerAfterApplying.Value = value;
                    Save();
                    OnPropertyChanged(nameof(RestartExplorerAfterApplying));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                    OnPropertyChanged(nameof(IsStatusVisible));
                }
            }
        }

        public bool IsStatusVisible => !string.IsNullOrEmpty(_statusMessage);

        /// <summary>
        /// Gets a value indicating whether this Windows version can turn the taskbar shortcut
        /// files into real pins.
        /// </summary>
        public bool IsTaskbarPinningSupported => AppGroupService.IsAutomaticTaskbarPinningSupported;

        public bool IsTaskbarPinningUnsupported => !IsTaskbarPinningSupported;

        /// <summary>
        /// Creates a new, empty group. Called from the page and from the dashboard button.
        /// </summary>
        public void AddGroup()
        {
            var group = new AppGroup
            {
                Name = GetDefaultGroupName(),
            };

            Groups.Add(new AppGroupViewModel(group, this));
            SyncModel();
            StatusMessage = string.Empty;
        }

        public void ApplyAllGroups()
        {
            ReportResult(_service.ApplyAll(Groups.Select(group => group.Model), RestartExplorerAfterApplying));
            Save();
        }

        internal void ApplyGroup(AppGroupViewModel group)
        {
            if (group == null)
            {
                return;
            }

            ReportResult(_service.ApplyGroup(group.Model, RestartExplorerAfterApplying));
            Save();
        }

        internal void RemoveGroup(AppGroupViewModel group)
        {
            if (group == null)
            {
                return;
            }

            ReportResult(_service.Remove(group.Model, RestartExplorerAfterApplying));
            Groups.Remove(group);
            SyncModel();
        }

        /// <summary>
        /// Persists the current group list to the module settings file.
        /// </summary>
        internal void Save()
        {
            _settingsUtils.SaveSettings(_settings.ToJsonString(), AppGroupsSettings.ModuleName);
        }

        public void RefreshEnabledState()
        {
            _isEnabled = _generalSettingsConfig.Enabled.AppGroups;
            OnPropertyChanged(nameof(IsEnabled));
        }

        private void SyncModel()
        {
            _settings.Properties.Groups = Groups.Select(group => group.Model).ToList();
            Save();
        }

        private void ReportResult(AppGroupApplyResult result)
        {
            if (result.HasErrors)
            {
                StatusMessage = string.Join(Environment.NewLine, result.Errors);
            }
            else if (result.TaskbarPinningSkipped)
            {
                StatusMessage = ResourceLoaderInstance.ResourceLoader.GetString("AppGroups_StatusTaskbarUnsupported");
            }
            else if (result.TaskbarShortcutsPlaced)
            {
                StatusMessage = ResourceLoaderInstance.ResourceLoader.GetString("AppGroups_StatusTaskbarBestEffort");
            }
            else
            {
                StatusMessage = ResourceLoaderInstance.ResourceLoader.GetString("AppGroups_StatusApplied");
            }
        }

        private string GetDefaultGroupName()
        {
            string baseName = ResourceLoaderInstance.ResourceLoader.GetString("AppGroups_DefaultGroupName");
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "Group";
            }

            int number = Groups.Count + 1;
            while (Groups.Any(group => string.Equals(group.Name, $"{baseName} {number}", StringComparison.OrdinalIgnoreCase)))
            {
                number++;
            }

            return $"{baseName} {number}";
        }

        private bool _isEnabled;

        private string _statusMessage = string.Empty;
    }
}
