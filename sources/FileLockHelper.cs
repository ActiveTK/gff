using Goodbye_F__king_File;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;

namespace Goodbye_F__king_File
{
    public static class FileLockHelper
    {
        // �萔�Ȃǂ̒�`
        private const int SystemHandleInformation = 16;
        private const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
        private const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        private const int ObjectNameInformation = 1;
        private const int PROCESS_DUP_HANDLE = 0x0040;

        // NTAPI: NtQuerySystemInformation
        [DllImport("ntdll.dll")]
        private static extern uint NtQuerySystemInformation(
            int SystemInformationClass,
            IntPtr SystemInformation,
            uint SystemInformationLength,
            ref uint ReturnLength);

        // NTAPI: NtQueryObject
        [DllImport("ntdll.dll")]
        private static extern uint NtQueryObject(
            IntPtr ObjectHandle,
            int ObjectInformationClass,
            IntPtr ObjectInformation,
            uint ObjectInformationLength,
            ref uint ReturnLength);

        // Win32 API: DuplicateHandle
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            ushort hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            uint dwDesiredAccess,
            bool bInheritHandle,
            uint dwOptions);

        // Win32 API: OpenProcess
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        // Win32 API: CloseHandle
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // �V�X�e���n���h�����̍\���́i�e�n���h���̏��j
        [StructLayout(LayoutKind.Sequential)]
        struct SYSTEM_HANDLE_ENTRY
        {
            public int OwnerPid;
            public byte ObjectType;
            public byte HandleFlags;
            public ushort HandleValue;
            public IntPtr ObjectPointer;
            public uint AccessMask;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmRegisterResources(uint pSessionHandle,
            uint nFiles,
            string[] rgsFilenames,
            uint nApplications,
            [In] RM_UNIQUE_PROCESS[] rgApplications,
            uint nServices,
            string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        static extern int RmGetList(uint dwSessionHandle,
            out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);

        // Restart Manager API �֘A�̍\���́E�萔��`
        [StructLayout(LayoutKind.Sequential)]
        struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            [Obsolete]
            public FILETIME ProcessStartTime;
        }

        enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string strServiceShortName;
            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }
        [DllImport("rstrtmgr.dll")]
        static extern int RmEndSession(uint pSessionHandle);

        // �w�肳�ꂽ�t�@�C�������b�N���Ă���v���Z�X�̈ꗗ���擾
        public static List<Process> GetLockingProcesses(string path)
        {
            uint handle;
            string sessionKey = Guid.NewGuid().ToString();

            int res = RmStartSession(out handle, 0, sessionKey);
            if (res != 0)
                throw new Exception("Restart Manager �Z�b�V�����̊J�n�Ɏ��s���܂����B");

            try
            {
                string[] resources = new string[] { path };
                res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);
                if (res != 0)
                    throw new Exception("���\�[�X�̓o�^�Ɏ��s���܂����B");

                uint pnProcInfoNeeded = 0;
                uint pnProcInfo = 0;
                uint lpdwRebootReasons = 0;

                // �K�v�ȃv���Z�X���̃T�C�Y��₢���킹��
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);
                if (res != 0 && res != 234) // ERROR_MORE_DATA
                    throw new Exception("�v���Z�X���̎擾�Ɏ��s���܂����B");

                RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                pnProcInfo = pnProcInfoNeeded;

                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                if (res != 0)
                    throw new Exception("�v���Z�X���̎擾�Ɏ��s���܂����B");

                var lockingProcesses = new List<Process>();
                foreach (var procInfo in processInfo)
                {
                    try
                    {
                        var proc = Process.GetProcessById(procInfo.Process.dwProcessId);
                        lockingProcesses.Add(proc);
                    }
                    catch
                    {
                        // �v���Z�X�����ɏI�����Ă���ꍇ�̓X�L�b�v
                    }
                }
                return lockingProcesses;
            }
            finally
            {
                RmEndSession(handle);
            }
        }


        // �w�肵���v���Z�X�̒��ŁA�Ώۃt�@�C���Ɋ֘A����n���h���������I�ɕ���
        public static bool ForceCloseFileHandle(Process proc, string filePath)
        {
            bool anyClosed = false;
            IntPtr procHandle = OpenProcess(PROCESS_DUP_HANDLE, false, proc.Id);
            if (procHandle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "�v���Z�X�n���h���̎擾�Ɏ��s���܂����B");

            try
            {
                // �V�X�e���n���h�����̎擾
                uint handleInfoSize = 0x10000;
                IntPtr handleInfoPtr = Marshal.AllocHGlobal((int)handleInfoSize);
                try
                {
                    uint retLength = 0;
                    uint ntStatus = NtQuerySystemInformation(SystemHandleInformation, handleInfoPtr, handleInfoSize, ref retLength);
                    while (ntStatus == STATUS_INFO_LENGTH_MISMATCH)
                    {
                        Marshal.FreeHGlobal(handleInfoPtr);
                        handleInfoSize = retLength;
                        handleInfoPtr = Marshal.AllocHGlobal((int)handleInfoSize);
                        ntStatus = NtQuerySystemInformation(SystemHandleInformation, handleInfoPtr, handleInfoSize, ref retLength);
                    }
                    if (ntStatus != 0)
                        throw new Exception("NtQuerySystemInformation �Ɏ��s���܂����BNTSTATUS: 0x" + ntStatus.ToString("X"));

                    // �擪�� Int32 �̓n���h���̐�������
                    int handleCount = Marshal.ReadInt32(handleInfoPtr);
                    IntPtr handleEntryPtr = IntPtr.Add(handleInfoPtr, sizeof(int));

                    int sizeOfEntry = Marshal.SizeOf(typeof(SYSTEM_HANDLE_ENTRY));

                    // �Ώۃv���Z�X�̃n���h���𑖍�
                    for (int i = 0; i < handleCount; i++)
                    {
                        SYSTEM_HANDLE_ENTRY entry = Marshal.PtrToStructure<SYSTEM_HANDLE_ENTRY>(handleEntryPtr);
                        if (entry.OwnerPid != proc.Id)
                        {
                            handleEntryPtr = IntPtr.Add(handleEntryPtr, sizeOfEntry);
                            continue;
                        }

                        // DuplicateHandle ��p���đΏۃn���h�������v���Z�X�֕����i�ǂݎ���p�j
                        if (DuplicateHandle(procHandle, entry.HandleValue, Process.GetCurrentProcess().Handle, out IntPtr dupHandle, 0, false, 0))
                        {
                            try
                            {
                                // �n���h������I�u�W�F�N�g�����擾
                                string objectName = GetObjectName(dupHandle);
                                if (!string.IsNullOrEmpty(objectName) && objectName.IndexOf(filePath, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    // ��v�����ꍇ�ADUPLICATE_CLOSE_SOURCE �w��Ńn���h���𕡐����A���������
                                    IntPtr dummy;
                                    bool dupClose = DuplicateHandle(procHandle, entry.HandleValue, Process.GetCurrentProcess().Handle, out dummy, 0, false, DUPLICATE_CLOSE_SOURCE);
                                    if (dupClose)
                                    {
                                        anyClosed = true;
                                        Logger.Log(Logger.LogType.INFO, $"�v���Z�X {proc.Id} �̃n���h�� 0x{entry.HandleValue:X} ����܂����B�i�Ώ�: {objectName}�j");
                                    }
                                    else
                                    {
                                        Logger.Log(Logger.LogType.ERROR, $"�v���Z�X {proc.Id} �̃n���h�� 0x{entry.HandleValue:X} �̃N���[�Y�Ɏ��s���܂���: {Marshal.GetLastWin32Error()}");
                                    }
                                }
                            }
                            finally
                            {
                                CloseHandle(dupHandle);
                            }
                        }
                        handleEntryPtr = IntPtr.Add(handleEntryPtr, sizeOfEntry);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(handleInfoPtr);
                }
            }
            finally
            {
                CloseHandle(procHandle);
            }
            return anyClosed;
        }

        // NtQueryObject ���g�p���āA�n���h������I�u�W�F�N�g�����擾
        private static string GetObjectName(IntPtr handle)
        {
            uint length = 0;
            // �K�v�ȃT�C�Y��₢���킹��
            uint status = NtQueryObject(handle, ObjectNameInformation, IntPtr.Zero, 0, ref length);
            if (length == 0)
                return null;

            IntPtr nameInfoPtr = Marshal.AllocHGlobal((int)length);
            try
            {
                status = NtQueryObject(handle, ObjectNameInformation, nameInfoPtr, length, ref length);
                if (status != 0)
                    return null;

                // OBJECT_NAME_INFORMATION �͐擪�� UNICODE_STRING ������
                UNICODE_STRING unicodeStr = Marshal.PtrToStructure<UNICODE_STRING>(nameInfoPtr);
                if (unicodeStr.Length <= 0)
                    return null;
                // UNICODE_STRING �� Buffer ���當������擾
                return Marshal.PtrToStringUni(unicodeStr.Buffer, unicodeStr.Length / 2);
            }
            finally
            {
                Marshal.FreeHGlobal(nameInfoPtr);
            }
        }

        // UNICODE_STRING �̍\���̒�`
        [StructLayout(LayoutKind.Sequential)]
        struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }
    }
}