// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.PowerToys.Settings.UI.Library
{
    public class AppGroupsProperties
    {
        public AppGroupsProperties()
        {
            Groups = new List<AppGroup>();
            RestartExplorerAfterApplying = new BoolProperty(true);
        }

        [JsonPropertyName("groups")]
        public List<AppGroup> Groups { get; set; }

        [JsonPropertyName("restartExplorerAfterApplying")]
        public BoolProperty RestartExplorerAfterApplying { get; set; }
    }
}
