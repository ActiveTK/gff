using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Threading;

namespace Goodbye_F__king_File
{
    public class ProcessKiller
    {
        // P/Invoke �錾: �g�[�N������p
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint Zero, IntPtr Null1, IntPtr Null2);

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID Luid;
            public uint Attributes;
        }

        const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        const uint TOKEN_QUERY = 0x0008;
        const uint SE_PRIVILEGE_ENABLED = 0x00000002;

        // P/Invoke �錾: �v���Z�X����p
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        const uint PROCESS_TERMINATE = 0x0001;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        // SeDebugPrivilege ��L����
        public static bool EnableDebugPrivilege()
        {
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr hToken))
            {
                Logger.Log(Logger.LogType.ERROR, "OpenProcessToken �̌Ăяo���Ɏ��s���܂����B");
                return false;
            }

            if (!LookupPrivilegeValue(null, "SeDebugPrivilege", out LUID luid))
            {
                Logger.Log(Logger.LogType.ERROR, "LookupPrivilegeValue �̌Ăяo���Ɏ��s���܂����B");
                return false;
            }

            TOKEN_PRIVILEGES tp;
            tp.PrivilegeCount = 1;
            tp.Luid = luid;
            tp.Attributes = SE_PRIVILEGE_ENABLED;
            if (!AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
            {
                Logger.Log(Logger.LogType.ERROR, "AdjustTokenPrivileges �̌Ăяo���Ɏ��s���܂����B");
                return false;
            }
            return true;
        }

        // �w�肳�ꂽ�v���Z�X�������I��
        public static void ForceKillProcess(Process proc)
        {
            // �܂��̓f�o�b�O������L����
            if (!EnableDebugPrivilege())
            {
                Logger.Log(Logger.LogType.ERROR, "�f�o�b�O�����̗L�����Ɏ��s���܂����B");
                return;
            }

            IntPtr hProcess = OpenProcess(PROCESS_TERMINATE, false, proc.Id);
            if (hProcess == IntPtr.Zero)
            {
                Logger.Log(Logger.LogType.ERROR, $"�v���Z�X {proc.Id} ���I�[�v���ł��܂���ł����B");
                return;
            }

            if (!TerminateProcess(hProcess, 1))
            {
                Logger.Log(Logger.LogType.ERROR, $"�v���Z�X {proc.Id} �̋����I���Ɏ��s���܂����B");
            }
            else
            {
                Logger.Log(Logger.LogType.INFO, $"�v���Z�X {proc.Id} �������I�����܂����B");
                Thread.Sleep(200);
            }
        }
    }
}