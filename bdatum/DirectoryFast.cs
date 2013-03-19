using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel;
using System.Security;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace bdatum
{
    
    class DirectoryFast
    {
        
    }

    internal sealed class Win32Native
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeFindHandle FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool FindNextFile(SafeFindHandle hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FindClose(IntPtr hFindFile);
    }

    [SecurityCritical]
    internal class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [SecurityCritical]
        public SafeFindHandle()
            : base(true)
        { }

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return Win32Native.FindClose(base.handle);
        }
    }

    public static class DirectoryExtensions
    {
        public static IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            // TODO: validate input parameters

            string lpFileName = Path.Combine(path, searchPattern);
            Win32Native.WIN32_FIND_DATA lpFindFileData;
            var handle = Win32Native.FindFirstFile(lpFileName, out lpFindFileData);
            if (handle.IsInvalid)
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr != 2 && hr != 0x12)
                {
                    throw new Win32Exception(hr);
                }
                yield break;
            }

            if (IsFile(lpFindFileData))
            {
                var fileName = Path.Combine(path, lpFindFileData.cFileName);
                yield return fileName;
            }

            while (Win32Native.FindNextFile(handle, out lpFindFileData))
            {
                if (IsFile(lpFindFileData))
                {
                    var fileName = Path.Combine(path, lpFindFileData.cFileName);
                    yield return fileName;
                }
            }

            handle.Dispose();
        }

        private static bool IsFile(Win32Native.WIN32_FIND_DATA data)
        {
            return 0 == (data.dwFileAttributes & 0x10);
        }
    }

}
