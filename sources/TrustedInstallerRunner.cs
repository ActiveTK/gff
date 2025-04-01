using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Goodbye_F__king_File
{
    public class TrustedInstallerRunner
    {
        // �萔��`
        const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
        const uint TOKEN_QUERY = 0x8;
        const uint SE_PRIVILEGE_ENABLED = 0x2;
        const uint PROCESS_QUERY_INFORMATION = 0x0400;
        const uint PROCESS_DUP_HANDLE = 0x0040;
        const uint MAXIMUM_ALLOWED = 0x02000000;
        const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        const uint LOGON_WITH_PROFILE = 0x00000001;
        const uint TH32CS_SNAPPROCESS = 0x00000002;
        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_EXECUTE = 0x20000000;
        const uint GENERIC_EXECUTE_SC_MANAGER = 0x00020000;
        const int SC_STATUS_PROCESS_INFO = 0;
        const uint CREATE_NO_WINDOW = 0x08000000;
        const uint STARTF_USESTDHANDLES = 0x00000100;
        const uint HANDLE_FLAG_INHERIT = 0x00000001;
        const uint INFINITE = 0xFFFFFFFF;

        #region STRUCT��`

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public uint nLength;
            public IntPtr lpSecurityDescriptor;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS_PROCESS
        {
            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
            public uint dwProcessId;
            public uint dwServiceFlags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        #endregion

        #region ���X��dll���C���|�[�g

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess,
            ref SECURITY_ATTRIBUTES lpTokenAttributes, int ImpersonationLevel, int TokenType, out IntPtr phNewToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool QueryServiceStatusEx(IntPtr hService, int InfoLevel, IntPtr lpBuffer, uint cbBufSize, out uint pcbBytesNeeded);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcessWithTokenW(IntPtr hToken, uint dwLogonFlags, string lpApplicationName,
            string lpCommandLine, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        #endregion

        #region ��������

        // �K�v�ȓ�����L��������
        static bool EnablePrivilege(string privilegeName)
        {
            if (!OpenProcessToken(System.Diagnostics.Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr hToken))
            {
                Logger.Log(Logger.LogType.ERROR, $"[EnablePrivilege] OpenProcessToken�̎��s�Ɏ��s���܂��� (����: {privilegeName})�B�G���[: {Marshal.GetLastWin32Error()}");
                return false;
            }
            if (!LookupPrivilegeValue(null, privilegeName, out LUID luid))
            {
                Logger.Log(Logger.LogType.ERROR, $"[EnablePrivilege] LookupPrivilegeValue�̎��s�Ɏ��s���܂��� (����: {privilegeName})�B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hToken);
                return false;
            }
            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES[1]
            };
            tp.Privileges[0].Luid = luid;
            tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

            if (!AdjustTokenPrivileges(hToken, false, ref tp, (uint)Marshal.SizeOf(typeof(TOKEN_PRIVILEGES)), IntPtr.Zero, IntPtr.Zero))
            {
                Logger.Log(Logger.LogType.ERROR, $"[EnablePrivilege] AdjustTokenPrivileges�̎��s�Ɏ��s���܂��� (����: {privilegeName})�B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hToken);
                return false;
            }
            CloseHandle(hToken);
            Logger.Log(Logger.LogType.DEBUG, $"[EnablePrivilege] ���� {privilegeName} �𐳏�ɗL�������܂����B");
            return true;
        }

        // �w��v���Z�X������v���Z�XID���擾�i������Ȃ����0��Ԃ��j
        static uint GetProcessIdByName(string processName)
        {
            IntPtr hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (hSnapshot == (IntPtr)(-1))
            {
                Logger.Log(Logger.LogType.ERROR, $"[GetProcessIdByName] CreateToolhelp32Snapshot�̎��s�Ɏ��s���܂����B�G���[: {Marshal.GetLastWin32Error()}");
                return 0;
            }

            PROCESSENTRY32 pe = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32)) };
            uint pid = 0;
            if (Process32First(hSnapshot, ref pe))
            {
                do
                {
                    if (string.Compare(pe.szExeFile, processName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        pid = pe.th32ProcessID;
                        break;
                    }
                } while (Process32Next(hSnapshot, ref pe));
            }
            else
            {
                Logger.Log(Logger.LogType.ERROR, $"[GetProcessIdByName] Process32First�̎��s�Ɏ��s���܂����B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hSnapshot);
                return 0;
            }
            CloseHandle(hSnapshot);
            if (pid == 0)
                Logger.Log(Logger.LogType.ERROR, $"[GetProcessIdByName] �v���Z�X��������܂���ł���: {processName}");
            return pid;
        }

        // winlogon.exe �̃g�[�N����p���ăV�X�e���̃C���p�[�\�l�[�V�������s��
        static void ImpersonateSystem()
        {
            uint systemPid = GetProcessIdByName("winlogon.exe");
            if (systemPid == 0)
            {
                Logger.Log(Logger.LogType.ERROR, "[ImpersonateSystem] winlogon.exe��������܂���ł����B");
                return;
            }
            IntPtr hSystemProcess = OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION, false, systemPid);
            if (hSystemProcess == IntPtr.Zero)
            {
                Logger.Log(Logger.LogType.ERROR, $"[ImpersonateSystem] OpenProcess�̎��s�Ɏ��s���܂��� (winlogon.exe)�B�G���[: {Marshal.GetLastWin32Error()}");
                return;
            }
            if (!OpenProcessToken(hSystemProcess, MAXIMUM_ALLOWED, out IntPtr hSystemToken))
            {
                Logger.Log(Logger.LogType.ERROR, $"[ImpersonateSystem] OpenProcessToken�̎��s�Ɏ��s���܂��� (winlogon.exe)�B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hSystemProcess);
                return;
            }
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES
            {
                nLength = (uint)Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES)),
                bInheritHandle = false,
                lpSecurityDescriptor = IntPtr.Zero
            };
            if (!DuplicateTokenEx(hSystemToken, MAXIMUM_ALLOWED, ref sa, 2 /* SecurityImpersonation */, 2 /* TokenImpersonation */, out IntPtr hDupToken))
            {
                Logger.Log(Logger.LogType.ERROR, $"[ImpersonateSystem] DuplicateTokenEx�̎��s�Ɏ��s���܂��� (winlogon.exe)�B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hSystemToken);
                CloseHandle(hSystemProcess);
                return;
            }
            if (!ImpersonateLoggedOnUser(hDupToken))
            {
                Logger.Log(Logger.LogType.ERROR, $"[ImpersonateSystem] ImpersonateLoggedOnUser�̎��s�Ɏ��s���܂����B�G���[: {Marshal.GetLastWin32Error()}");
            }
            else
            {
                Logger.Log(Logger.LogType.INFO, "[ImpersonateSystem] �V�X�e���̃C���p�[�\�l�[�V�����ɐ������܂����B(SeDebugPrivilege/SeImpersonatePrivilege)");
            }
            CloseHandle(hDupToken);
            CloseHandle(hSystemToken);
            CloseHandle(hSystemProcess);
        }

        // TrustedInstaller�T�[�r�X���J�n���A�v���Z�XID��Ԃ�
        static uint StartTrustedInstallerService()
        {
            Logger.Log(Logger.LogType.DEBUG, "[StartTrustedInstallerService] �T�[�r�X�R���g���[���}�l�[�W���[���I�[�v����...");
            IntPtr hSCManager = OpenSCManager(null, "ServicesActive", GENERIC_EXECUTE_SC_MANAGER);
            if (hSCManager == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenSCManager failed");

            Logger.Log(Logger.LogType.DEBUG, "[StartTrustedInstallerService] TrustedInstaller�T�[�r�X���I�[�v����...");
            IntPtr hService = OpenService(hSCManager, "TrustedInstaller", GENERIC_READ | GENERIC_EXECUTE);
            if (hService == IntPtr.Zero)
            {
                CloseHandle(hSCManager);
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenService failed");
            }

            SERVICE_STATUS_PROCESS ssp = new SERVICE_STATUS_PROCESS();
            uint bytesNeeded = 0;
            int sspSize = Marshal.SizeOf(typeof(SERVICE_STATUS_PROCESS));
            IntPtr pStatus = Marshal.AllocHGlobal(sspSize);

            try
            {
                while (QueryServiceStatusEx(hService, SC_STATUS_PROCESS_INFO, pStatus, (uint)sspSize, out bytesNeeded))
                {
                    ssp = Marshal.PtrToStructure<SERVICE_STATUS_PROCESS>(pStatus);
                    Logger.Log(Logger.LogType.DEBUG, $"[StartTrustedInstallerService] �T�[�r�X�̌��݂̏��: {ssp.dwCurrentState}");
                    // �T�[�r�X����~���Ă���ꍇ�͋N������
                    if (ssp.dwCurrentState == 1) // SERVICE_STOPPED
                    {
                        Logger.Log(Logger.LogType.WARN, "[StartTrustedInstallerService] �T�[�r�X�͒�~���ł��B�N�������݂܂�...");
                        if (!StartService(hService, 0, null))
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "StartService failed");
                        }
                    }
                    // �T�[�r�X�J�n���̏ꍇ�͑ҋ@����
                    if (ssp.dwCurrentState == 2 /* SERVICE_START_PENDING */ ||
                        ssp.dwCurrentState == 3 /* SERVICE_STOP_PENDING */)
                    {
                        Logger.Log(Logger.LogType.DEBUG, $"[StartTrustedInstallerService] �T�[�r�X�̏�Ԃ͕ۗ����ł��B{ssp.dwWaitHint} ms�ҋ@��...");
                        Thread.Sleep((int)ssp.dwWaitHint);
                        continue;
                    }
                    if (ssp.dwCurrentState == 4) // SERVICE_RUNNING
                    {
                        Logger.Log(Logger.LogType.INFO, $"[StartTrustedInstallerService] �T�[�r�X�͎��s���ł��BPID: {ssp.dwProcessId}");
                        return ssp.dwProcessId;
                    }
                }
            }
            finally
            {
                try
                {
                    Marshal.FreeHGlobal(pStatus);
                    CloseHandle(hService);
                    CloseHandle(hSCManager);
                }
                catch { }
            }
            throw new Win32Exception(Marshal.GetLastWin32Error(), "QueryServiceStatusEx failed");
        }

        // TrustedInstaller�v���Z�X�̃g�[�N����p���ăR�}���h�����s���A
        // �W���o�́^�G���[�̃��_�C���N�g�p�ɓ����p�C�v���쐬���AhReadPipe��Ԃ��B
        // ���s����PROCESS_INFORMATION.hProcess==IntPtr.Zero
        static PROCESS_INFORMATION CreateProcessAsTrustedInstaller(uint trustedInstallerPid, string commandLine, out IntPtr hReadPipe)
        {
            hReadPipe = IntPtr.Zero;
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            // �K�v�ȓ�����L����
            if (!EnablePrivilege("SeDebugPrivilege") || !EnablePrivilege("SeImpersonatePrivilege"))
            {
                Logger.Log(Logger.LogType.ERROR, "[CreateProcessAsTrustedInstaller] �K�v�ȓ����̗L�����Ɏ��s���܂����B");
                return pi;
            }
            // �V�X�e���ɃC���p�[�\�l�[�g
            ImpersonateSystem();

            IntPtr hTIProcess = OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION, false, trustedInstallerPid);
            if (hTIProcess == IntPtr.Zero)
            {
                Logger.Log(Logger.LogType.ERROR, $"[CreateProcessAsTrustedInstaller] OpenProcess�̎��s�Ɏ��s���܂��� (TrustedInstaller.exe)�B�G���[: {Marshal.GetLastWin32Error()}");
                return pi;
            }
            if (!OpenProcessToken(hTIProcess, MAXIMUM_ALLOWED, out IntPtr hTIToken))
            {
                Logger.Log(Logger.LogType.ERROR, $"[CreateProcessAsTrustedInstaller] OpenProcessToken�̎��s�Ɏ��s���܂��� (TrustedInstaller.exe)�B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hTIProcess);
                return pi;
            }
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES
            {
                nLength = (uint)Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES)),
                bInheritHandle = false,
                lpSecurityDescriptor = IntPtr.Zero
            };
            if (!DuplicateTokenEx(hTIToken, MAXIMUM_ALLOWED, ref sa, 2, 2, out IntPtr hDupToken))
            {
                Logger.Log(Logger.LogType.ERROR, $"[CreateProcessAsTrustedInstaller] DuplicateTokenEx�̎��s�Ɏ��s���܂��� (TrustedInstaller.exe)�B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hTIToken);
                CloseHandle(hTIProcess);
                return pi;
            }
            // �����p�C�v�쐬�F�q�v���Z�X�ւ͏������݃n���h�����p��
            SECURITY_ATTRIBUTES saPipe = new SECURITY_ATTRIBUTES
            {
                nLength = (uint)Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES)),
                bInheritHandle = true,
                lpSecurityDescriptor = IntPtr.Zero
            };
            if (!CreatePipe(out hReadPipe, out IntPtr hWritePipe, ref saPipe, 0))
            {
                Logger.Log(Logger.LogType.ERROR, $"[CreateProcessAsTrustedInstaller] CreatePipe�̎��s�Ɏ��s���܂����B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hDupToken);
                CloseHandle(hTIToken);
                CloseHandle(hTIProcess);
                return pi;
            }
            // �e���̓ǂݎ��n���h���͌p�������Ȃ�
            if (!SetHandleInformation(hReadPipe, HANDLE_FLAG_INHERIT, 0))
            {
                Logger.Log(Logger.LogType.ERROR, $"[CreateProcessAsTrustedInstaller] SetHandleInformation�̎��s�Ɏ��s���܂����B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hWritePipe);
                CloseHandle(hDupToken);
                CloseHandle(hTIToken);
                CloseHandle(hTIProcess);
                return pi;
            }
            STARTUPINFO si = new STARTUPINFO();
            si.cb = (uint)Marshal.SizeOf(typeof(STARTUPINFO));
            si.lpDesktop = "Winsta0\\Default";
            // �W���o�́^�G���[�����_�C���N�g
            si.dwFlags = STARTF_USESTDHANDLES;
            si.hStdOutput = hWritePipe;
            si.hStdError = hWritePipe;
            // CREATE_NO_WINDOW���w�肵�Ĕ�\���Ŏ��s
            uint creationFlags = CREATE_UNICODE_ENVIRONMENT | CREATE_NO_WINDOW;
            if (!CreateProcessWithTokenW(hDupToken, LOGON_WITH_PROFILE, null, commandLine, creationFlags, IntPtr.Zero, null, ref si, out pi))
            {
                Logger.Log(Logger.LogType.ERROR, $"[CreateProcessAsTrustedInstaller] CreateProcessWithTokenW�̎��s�Ɏ��s���܂����B�G���[: {Marshal.GetLastWin32Error()}");
                CloseHandle(hWritePipe);
                CloseHandle(hDupToken);
                CloseHandle(hTIToken);
                CloseHandle(hTIProcess);
                return pi;
            }
            // �q�v���Z�X�ɂ�hWritePipe���p�������̂ŁA�e���͕���
            CloseHandle(hWritePipe);
            CloseHandle(hDupToken);
            CloseHandle(hTIToken);
            CloseHandle(hTIProcess);
            Logger.Log(Logger.LogType.INFO, "[CreateProcessAsTrustedInstaller] �v���Z�X�̍쐬�ɐ������܂����B");
            return pi;
        }

        #endregion

        // TrustedInstaller�Ƃ��ăv���Z�X���J�n���A�o�͂����_�C���N�g���ăR���\�[���֏o�́A�v���Z�X�I����Ɏ��g���I������
        // commandLine�̂����o�C�i���܂ł̃p�X�͓�d���p���ň͂����ƁI
        public static int Run(string commandLine)
        {
            try
            {
                Logger.Log(Logger.LogType.INFO, "[Main] TrustedInstaller�T�[�r�X���J�n���Ă��܂�...");
                uint tiPid = StartTrustedInstallerService();
                if (tiPid == 0)
                {
                    Logger.Log(Logger.LogType.ERROR, "[Main] TrustedInstaller�T�[�r�X�̊J�n�Ɏ��s���܂����B");
                    return 1;
                }
                Logger.Log(Logger.LogType.INFO, "[Main] TrustedInstaller�Ƃ��ăv���Z�X���쐬���Ă��܂�...");

                // �v���Z�X�쐬�ƕW���o�̓��_�C���N�g�p�p�C�v�̎擾
                PROCESS_INFORMATION pi = CreateProcessAsTrustedInstaller(tiPid, commandLine, out IntPtr hReadPipe);
                if (pi.hProcess == IntPtr.Zero)
                {
                    Logger.Log(Logger.LogType.ERROR, "[Main] TrustedInstaller�Ƃ��ăv���Z�X�̍쐬�Ɏ��s���܂����B");
                    return 1;
                }

                // �ʃX���b�h�Ń��_�C���N�g���ꂽ�o�͂�ǂ݁ALogger�o�R�ŏo��
                Thread outputThread = new Thread(() =>
                {
                    try
                    {
                        using (FileStream fs = new FileStream(new SafeFileHandle(hReadPipe, true), FileAccess.Read))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                string output = Encoding.Default.GetString(buffer, 0, bytesRead);
                                Console.Write(output);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(Logger.LogType.ERROR, $"[OutputThread] ���_�C���N�g���ꂽ�o�͂̓ǂݎ�蒆�ɃG���[���������܂���: {ex.Message}");
                    }
                });
                outputThread.IsBackground = true;
                outputThread.Start();

                // �q�v���Z�X�̏I���܂őҋ@
                WaitForSingleObject(pi.hProcess, INFINITE);
                CloseHandle(pi.hProcess);
                CloseHandle(pi.hThread);
                outputThread.Join();
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.ERROR, "[Main] ��O: " + ex.Message);
            }
            return 0;
        }
    }
}
