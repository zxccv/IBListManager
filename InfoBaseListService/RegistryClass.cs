using System;
using System.Runtime.InteropServices;
// ReSharper disable All

namespace InfoBaseListService
{
    class RegistryClass
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID pLuid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TokPriv1Luid
        {
            public int Count;
            public LUID Luid;
            public UInt32 Attr;
        }

        private const Int32 ANYSIZE_ARRAY = 1;
        private const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
        private const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const UInt32 TOKEN_QUERY = 0x0008;

        private const uint HKEY_USERS = 0x80000003;
        private const string SE_RESTORE_NAME = "SeRestorePrivilege";
        private const string SE_BACKUP_NAME = "SeBackupPrivilege";

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        static extern bool AdjustTokenPrivileges(
            IntPtr htok,
            bool disableAllPrivileges,
            ref TokPriv1Luid newState,
            int len,
            IntPtr prev,
            IntPtr relen);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern Int32 RegLoadKey(UInt32 hKey, String lpSubKey, String lpFile);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern Int32 RegUnLoadKey(UInt32 hKey, string lpSubKey);

        private static IntPtr _myToken;
        private static TokPriv1Luid _tokenPrivileges = new TokPriv1Luid();
        private static TokPriv1Luid _tokenPrivileges2 = new TokPriv1Luid();

        private static LUID _restoreLuid;
        private static LUID _backupLuid;

        public static void GetPrivileges()
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out _myToken))
                throw new Exception("OpenProcess Error");

            if (!LookupPrivilegeValue(null, SE_RESTORE_NAME, out _restoreLuid))
                throw new Exception("LookupPrivilegeValue Error");

            if (!LookupPrivilegeValue(null, SE_BACKUP_NAME, out _backupLuid))
                throw new Exception("LookupPrivilegeValue Error");

            _tokenPrivileges.Attr = SE_PRIVILEGE_ENABLED;
            _tokenPrivileges.Luid = _restoreLuid;
            _tokenPrivileges.Count = 1;

            _tokenPrivileges2.Attr = SE_PRIVILEGE_ENABLED;
            _tokenPrivileges2.Luid = _backupLuid;
            _tokenPrivileges2.Count = 1;

            if (!AdjustTokenPrivileges(_myToken, false, ref _tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
                throw new Exception("AdjustTokenPrivileges Error: " + Marshal.GetLastWin32Error());

            if (!AdjustTokenPrivileges(_myToken, false, ref _tokenPrivileges2, 0, IntPtr.Zero, IntPtr.Zero))
                throw new Exception("AdjustTokenPrivileges Error: " + Marshal.GetLastWin32Error());
        }

        public static void LoadUser(string sid, string filename)
        {
            int retVal = RegLoadKey(HKEY_USERS, sid, filename);
            if (retVal != 0)
                throw new Exception("RegLoadKey Error:" + retVal);
        }

        public static void UnloadUser(string sid)
        {
            RegUnLoadKey(HKEY_USERS, sid);
        }
    }
}
