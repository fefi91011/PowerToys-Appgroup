// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.PowerToys.Settings.UI.Library
{
    /// <summary>
    /// A single application shortcut inside an <see cref="AppGroup"/>.
    /// </summary>
    public class AppGroupApp
    {
        public AppGroupApp()
        {
            Name = string.Empty;
            Path = string.Empty;
        }

        public AppGroupApp(string name, string path)
        {
            Name = name;
            Path = path;
        }

        /// <summary>
        /// Gets or sets the display name used for the shortcut file.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the full path of the executable the shortcut points to.
        /// </summary>
        [JsonPropertyName("path")]
        public string Path { get; set; }
    }
}
