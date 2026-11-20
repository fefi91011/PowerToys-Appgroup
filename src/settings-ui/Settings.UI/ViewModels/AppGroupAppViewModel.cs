// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;

using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.Library.ViewModels.Commands;

namespace Microsoft.PowerToys.Settings.UI.ViewModels
{
    /// <summary>
    /// View model for a single application entry of an app group.
    /// </summary>
    public class AppGroupAppViewModel
    {
        public AppGroupAppViewModel(AppGroupApp model, AppGroupViewModel owner)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            RemoveCommand = new RelayCommand<object>(_ => owner.RemoveApp(this));
        }

        internal AppGroupApp Model { get; }

        public string Name => Model.Name;

        public string Path => Model.Path;

        public ICommand RemoveCommand { get; }
    }
}
