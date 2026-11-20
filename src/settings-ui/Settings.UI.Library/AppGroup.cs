// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.PowerToys.Settings.UI.Library
{
    /// <summary>
    /// A named group of applications that can be placed on the desktop and/or pinned to the taskbar.
    /// </summary>
    public class AppGroup
    {
        public AppGroup()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = string.Empty;
            Apps = new List<AppGroupApp>();
        }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("showOnDesktop")]
        public bool ShowOnDesktop { get; set; }

        [JsonPropertyName("pinToTaskbar")]
        public bool PinToTaskbar { get; set; }

        [JsonPropertyName("apps")]
        public List<AppGroupApp> Apps { get; set; }

        /// <summary>
        /// Gets or sets the folder name that was used the last time this group was applied.
        /// Used to clean up a renamed group's old desktop folder and taskbar shortcuts.
        /// </summary>
        [JsonPropertyName("lastAppliedName")]
        public string LastAppliedName { get; set; }
    }
}
