// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Microsoft.PowerToys.Settings.UI.Services.AppGroups
{
    /// <summary>
    /// Creates Windows shell shortcuts (.lnk) without relying on WScript.Shell, so the settings
    /// process stays Native AOT compatible.
    /// </summary>
    internal static class ShellLinkHelper
    {
        public static void CreateShortcut(string shortcutPath, string targetPath)
        {
            IShellLinkW link = (IShellLinkW)new ShellLink();
            try
            {
                link.SetPath(targetPath);
                link.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? string.Empty);
                ((IPersistFile)link).Save(shortcutPath, true);
            }
            finally
            {
                Marshal.FinalReleaseComObject(link);
            }
        }

        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);

            void GetIDList(out IntPtr ppidl);

            void SetIDList(IntPtr pidl);

            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);

            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);

            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);

            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

            void GetHotkey(out short pwHotkey);

            void SetHotkey(short wHotkey);

            void GetShowCmd(out int piShowCmd);

            void SetShowCmd(int iShowCmd);

            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);

            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

            void Resolve(IntPtr hwnd, uint fFlags);

            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }
    }
}
