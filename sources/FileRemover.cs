using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Goodbye_F__king_File
{
    public class FileRemover
    {
        private readonly string _FilePath;
        public string VerifyError { get; set; }

        public FileRemover(string fp)
        {
            _FilePath = fp;
        }

        // �w�肳�ꂽ�p�X���L���ȃt�@�C�����ǂ���������
        public bool VerifyIfItsValid()
        {
            if (string.IsNullOrWhiteSpace(_FilePath))
            {
                VerifyError = "�t�@�C���p�X���w�肳��Ă��܂���B";
                return false;
            }
            if (Directory.Exists(_FilePath))
            {
                VerifyError = "�f�B���N�g�����w�肳��Ă��܂��B�t�@�C�����w�肵�Ă��������B";
                return false;
            }
            if (!File.Exists(@"\\?\" + _FilePath))
            {
                VerifyError = "�w�肳�ꂽ�t�@�C�������݂��܂���B";
                return false;
            }
            return true;
        }

        // �����I�Ƀt�@�C���폜�����s
        public void ForceRMFile()
        {
            Logger.Log(Logger.LogType.DEBUG, "==== �t�@�C���폜�������J�n���܂� ====");
            Logger.Log(Logger.LogType.DEBUG, "FilePath: " + _FilePath);

            // �t�@�C�������̊m�F�ƁA�ǂݎ���p�����ł���Ή���
            try
            {
                FileInfo file = new FileInfo(_FilePath);
                Logger.Log(Logger.LogType.DEBUG, "���݂̃t�@�C������: " + file.Attributes);
                if ((file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    Logger.Log(Logger.LogType.WARN, "�t�@�C�����ǂݎ���p�����̂��߁A�ʏ푮���ɕύX���܂��B");
                    file.Attributes = FileAttributes.Normal;
                    Logger.Log(Logger.LogType.DEBUG, "�����ύX��̃t�@�C������: " + file.Attributes);
                }
                else
                {
                    Logger.Log(Logger.LogType.DEBUG, "�t�@�C���͓ǂݎ���p�ł͂���܂���B");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.ERROR, "�t�@�C�������̊m�F���ɃG���[���������܂���: " + ex.Message);
            }

            // QuickUnlink �ɂ�鏉��폜���s
            Logger.Log(Logger.LogType.DEBUG, "QuickUnlink �ɂ��폜���s���J�n���܂�...");
            Logger.LogNotNewLine(Logger.LogType.INFO, "QuickUnlink " + _FilePath + " ");
            int resultQuickUnlink = QuickUnlink();

            switch (resultQuickUnlink)
            {
                case 0:
                    Logger.LogNotNewLine_Next("-> [DONE]");
                    return;
                case 1:
                    Logger.LogNotNewLine_Next("-> [ArgumentException] �p�X���󕶎��A�܂��͋󔒂̂݁A�܂��͖����ȕ������܂�ł��܂��B");
                    break;
                case 2:
                    Logger.LogNotNewLine_Next("-> [DirectoryNotFoundException] �w�肳�ꂽ�p�X�͖����ł��B");
                    return;
                case 3:
                    Logger.LogNotNewLine_Next("-> [PathTooLongException] �p�X�A�t�@�C�����A�܂��͂��̗������V�X�e����`�̍ő咷�𒴂��Ă��܂��B");
                    break;
                case 4:
                    Logger.LogNotNewLine_Next("-> [IOException] �w�肳�ꂽ�t�@�C���͎g�p���ł��B");
                    break;
                case 5:
                    Logger.LogNotNewLine_Next("-> [NotSupportedException] �p�X�̌`���������ł��B");
                    break;
                case 6:
                    Logger.LogNotNewLine_Next("-> [UnauthorizedAccessException] �K�v�Ȍ���������܂���B�܂��́A���s���̎��s�\�t�@�C���A�f�B���N�g���A�ǂݎ���p�t�@�C�����w�肳��Ă��܂��B");
                    break;
                default:
                    Logger.LogNotNewLine_Next("-> [Exception] �s���ȃG���[���������܂����B");
                    Logger.Log(Logger.LogType.ERROR, "�s���ȃG���[�̂��߃t�@�C�����폜�ł��܂���B");
                    return;
            }

            // ���s���̃v���Z�X�̏ꍇ�A�����I�������݂�
            Logger.Log(Logger.LogType.INFO, "�t�@�C�����g�p���̃v���Z�X���������Ă��܂�...");
            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.MainModule.FileName.Equals(_FilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Log(Logger.LogType.WARN, $"���s���̃v���Z�X {proc.Id} ({proc.ProcessName}) ���t�@�C�����g�p���ł��B�����I�������݂܂�...");
                        try
                        {
                            proc.Kill();
                            Logger.Log(Logger.LogType.INFO, $"�v���Z�X {proc.Id} �𐳏�ɏI�����܂����B");
                        }
                        catch
                        {
                            Logger.Log(Logger.LogType.WARN, $"�v���Z�X {proc.Id} �̏I���Ɏ��s�������߁AProcessKiller �𗘗p���ċ����I�������݂܂�...");
                            ProcessKiller.ForceKillProcess(proc);
                        }
                        try
                        {
                            proc.WaitForExit();
                        }
                        catch { }
                    }
                }
                catch { }
            }

            // �t�@�C�������b�N���Ă���v���Z�X������
            Logger.Log(Logger.LogType.INFO, "�t�@�C�������b�N���Ă���v���Z�X���擾���Ă��܂�...");
            try
            {
                List<Process> lockingProcesses = FileLockHelper.GetLockingProcesses(_FilePath);

                foreach (var proc in lockingProcesses)
                {
                    Logger.Log(Logger.LogType.WARN, $"�v���Z�X {proc.Id} ({proc.ProcessName}) ���t�@�C�������b�N���Ă��܂��B");

                    // �܂��̓n���h����������݂�
                    bool handleClosed = FileLockHelper.ForceCloseFileHandle(proc, _FilePath);
                    if (!handleClosed)
                    {
                        Logger.Log(Logger.LogType.ERROR, $"�n���h���̉���Ɏ��s���܂����B�v���Z�X {proc.Id} �������I�����܂��B");
                        ProcessKiller.ForceKillProcess(proc);
                    }
                    else
                    {
                        Logger.Log(Logger.LogType.INFO, $"�v���Z�X {proc.Id} �̃n���h���������I�ɉ�����܂����B");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogType.ERROR, "�t�@�C�����b�N�̎擾�Ɏ��s���܂����B�G���[: " + e.Message);
            }

            // ���L��/�A�N�Z�X�����ύX
            // TrustedInstaller �̏ꍇ�ASeTcbPrivilege ������S�ăo�C�p�X�ł��邽�ߕs�v�����O�̂��ߎ���
            try
            {
                FileInfo di = new FileInfo(_FilePath);
                FileSecurity ds = di.GetAccessControl();

                // TrustedInstaller �� NTAccount ���擾
                var trustedInstaller = new NTAccount("NT SERVICE", "TrustedInstaller");

                // TrustedInstaller �̍폜���������ɂ��邩�m�F����
                bool hasDeletePermission = false;
                AuthorizationRuleCollection rules = ds.GetAccessRules(true, true, typeof(NTAccount));
                foreach (FileSystemAccessRule rule in rules)
                {
                    // ���[���̑Ώۂ�TrustedInstaller���m�F
                    if (rule.IdentityReference.Value.Equals(trustedInstaller.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        // �폜������������Ă��邩�`�F�b�N
                        if ((rule.FileSystemRights & FileSystemRights.Delete) == FileSystemRights.Delete &&
                            rule.AccessControlType == AccessControlType.Allow)
                        {
                            hasDeletePermission = true;
                            break;
                        }
                    }
                }

                // TrustedInstaller�ɍ폜�������Ȃ������ꍇ�̂݁A���L�҂̕ύX�ƃt���R���g���[���̕t�^���s��
                if (!hasDeletePermission)
                {
                    // ���L�҂̐ݒ�
                    ds.SetOwner(trustedInstaller);

                    // TrustedInstaller �Ƀt���R���g���[����t�^
                    var accessRule = new FileSystemAccessRule(
                        trustedInstaller,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow);
                    ds.AddAccessRule(accessRule);

                    di.SetAccessControl(ds);
                    Logger.Log(Logger.LogType.INFO, "�t�@�C���̏��L�҂���ь����� TrustedInstaller �ɏC�����܂����B");
                }
                else
                {
                    Logger.Log(Logger.LogType.INFO, "TrustedInstaller �͊��ɍ폜������L���Ă��܂��B");
                }
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogType.ERROR, $"�t�@�C���̌����C���Ɏ��s���܂����B�G���[: {e.Message}");
            }


            // DOS Device Path �𗘗p���ăt�@�C���폜���Ď��s
            Logger.Log(Logger.LogType.INFO, "DOS Device Path �𗘗p���čŏI�I�ȃt�@�C���폜�����s���܂�...");
            try
            {
                File.Delete(@"\\?\" + _FilePath);
                Logger.Log(Logger.LogType.INFO, "�ŏI�I�Ƀt�@�C���̍폜�ɐ������܂����B");
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.ERROR, "�ŏI�I�ȍ폜�Ɏ��s���܂����B�G���[: " + ex.Message);
            }
        }

        int QuickUnlink()
        {
            try
            {
                File.Delete(_FilePath);
            }
            catch (ArgumentException)
            {
                return 1;
            }
            catch (DirectoryNotFoundException)
            {
                return 2;
            }
            catch (PathTooLongException)
            {
                return 3;
            }
            catch (IOException)
            {
                return 4;
            }
            catch (NotSupportedException)
            {
                return 5;
            }
            catch (UnauthorizedAccessException)
            {
                return 6;
            }
            catch (Exception)
            {
                return -1;
            }
            return 0;
        }
    }
}