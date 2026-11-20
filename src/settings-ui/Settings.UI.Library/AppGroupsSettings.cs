// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

using Microsoft.PowerToys.Settings.UI.Library.Interfaces;

namespace Microsoft.PowerToys.Settings.UI.Library
{
    public class AppGroupsSettings : BasePTModuleSettings, ISettingsConfig
    {
        public const string ModuleName = "AppGroups";

        public AppGroupsSettings()
        {
            Name = ModuleName;
            Version = "1.0";
            Properties = new AppGroupsProperties();
        }

        [JsonPropertyName("properties")]
        public AppGroupsProperties Properties { get; set; }

        public string GetModuleName() => Name;

        public bool UpgradeSettingsConfiguration() => false;
    }
}
