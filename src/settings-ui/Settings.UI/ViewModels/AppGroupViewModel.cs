// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.Library.Helpers;
using Microsoft.PowerToys.Settings.UI.Library.ViewModels.Commands;

namespace Microsoft.PowerToys.Settings.UI.ViewModels
{
    /// <summary>
    /// View model for a single app group.
    /// </summary>
    public class AppGroupViewModel : Observable
    {
        private readonly AppGroupsViewModel _owner;

        public AppGroupViewModel(AppGroup model, AppGroupsViewModel owner)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _owner = owner;
            Model.Apps ??= new List<AppGroupApp>();

            foreach (AppGroupApp app in Model.Apps)
            {
                if (app != null)
                {
                    Apps.Add(new AppGroupAppViewModel(app, this));
                }
            }

            ApplyCommand = new RelayCommand<object>(_ => _owner.ApplyGroup(this));
            RemoveCommand = new RelayCommand<object>(_ => _owner.RemoveGroup(this));
        }

        internal AppGroup Model { get; }

        public ObservableCollection<AppGroupAppViewModel> Apps { get; } = new ObservableCollection<AppGroupAppViewModel>();

        public ICommand ApplyCommand { get; }

        public ICommand RemoveCommand { get; }

        public bool HasNoApps => Apps.Count == 0;

        public bool IsTaskbarPinningSupported => _owner.IsTaskbarPinningSupported;

        public string Name
        {
            get => Model.Name;

            set
            {
                value ??= string.Empty;
                if (Model.Name != value)
                {
                    Model.Name = value;
                    OnPropertyChanged();
                    _owner.Save();
                }
            }
        }

        public bool ShowOnDesktop
        {
            get => Model.ShowOnDesktop;

            set
            {
                if (Model.ShowOnDesktop != value)
                {
                    Model.ShowOnDesktop = value;
                    OnPropertyChanged();
                    _owner.Save();
                }
            }
        }

        public bool PinToTaskbar
        {
            get => Model.PinToTaskbar;

            set
            {
                if (Model.PinToTaskbar != value)
                {
                    Model.PinToTaskbar = value;
                    OnPropertyChanged();
                    _owner.Save();
                }
            }
        }

        internal void AddApp(string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                Model.Apps.Any(app => string.Equals(app.Path, path, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var app = new AppGroupApp(System.IO.Path.GetFileNameWithoutExtension(path), path);
            Model.Apps.Add(app);
            Apps.Add(new AppGroupAppViewModel(app, this));
            OnPropertyChanged(nameof(HasNoApps));
            _owner.Save();
        }

        internal void RemoveApp(AppGroupAppViewModel app)
        {
            if (app == null)
            {
                return;
            }

            Model.Apps.Remove(app.Model);
            Apps.Remove(app);
            OnPropertyChanged(nameof(HasNoApps));
            _owner.Save();
        }
    }
}
