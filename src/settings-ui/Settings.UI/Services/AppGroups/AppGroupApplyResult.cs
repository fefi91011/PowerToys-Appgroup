// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.PowerToys.Settings.UI.Services.AppGroups
{
    /// <summary>
    /// Result of applying one or more app groups.
    /// </summary>
    internal sealed class AppGroupApplyResult
    {
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether taskbar shortcut files were added or removed.
        /// </summary>
        public bool TaskbarChanged { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether taskbar shortcut files were created. On Windows 10
        /// the shell turns these files into pins when Explorer restarts; on Windows 11 they would be
        /// ignored, so this stays <see langword="false"/> there.
        /// </summary>
        public bool TaskbarShortcutsPlaced { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether taskbar pinning was requested but is not supported
        /// by this Windows version.
        /// </summary>
        public bool TaskbarPinningSkipped { get; set; }

        public bool HasErrors => Errors.Count > 0;
    }
}
