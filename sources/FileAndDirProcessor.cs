using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System;

namespace Goodbye_F__king_File
{
    class FileAndDirectoryProcessor
    {
        public static void RemoveUsingfileRemover(string filePath)
        {
            Logger.Log(Logger.LogType.DEBUG, $"RemoveUsingfileRemover => filePath = '{filePath}'");

            // �t�@�C���̏ꍇ�͒��ڍ폜
            if (!Directory.Exists(filePath))
            {
                Logger.Log(Logger.LogType.DEBUG, $"'{filePath}' �̓t�@�C���Ƃ��ĔF������܂����BFileRemover �C���X�^���X�𐶐����܂��B");
                var fileRemover = new FileRemover(filePath);
                Logger.Log(Logger.LogType.DEBUG, $"FileRemover �𐶐����܂����B���Ƀt�@�C���̗L���������؂��܂��B");
                if (fileRemover.VerifyIfItsValid())
                {
                    Logger.Log(Logger.LogType.DEBUG, $"�t�@�C�� '{filePath}' �̌��؂ɐ����B�폜���������s���܂��B");
                    fileRemover.ForceRMFile();
                    Logger.Log(Logger.LogType.DEBUG, $"ForceRMFile() �̎��s���������܂����B");
                }
                else
                {
                    Logger.Log(Logger.LogType.DEBUG, $"�t�@�C�� '{filePath}' �̌��؂Ɏ��s�B�G���[���e�����O�o�͂��܂��B");
                    Logger.Log(Logger.LogType.ERROR, "�G���[: " + fileRemover.VerifyError);

                    if (Logger.ShowDebug)
                    {
                        Logger.Log(Logger.LogType.INFO, "�f�o�b�O���[�h���L��������Ă��邽�߁A�G���[�𖳎����ď����𑱍s���܂��B");
                        fileRemover.ForceRMFile();
                        Logger.Log(Logger.LogType.DEBUG, $"ForceRMFile() �̎��s���������܂����B");
                    }
                }
                return;
            }

            Logger.Log(Logger.LogType.DEBUG, $"'{filePath}' �̓f�B���N�g���Ƃ��ĔF������܂����B�f�B���N�g�����̃t�@�C�����擾���܂��B");
            // �f�B���N�g���̏ꍇ�́A�܂������̃t�@�C�����폜
            string[] files = GetFilesInDirectory(filePath);
            Logger.Log(Logger.LogType.DEBUG, $"GetFilesInDirectory ����: {FormatArrayForLog(files)}");
            foreach (string file in files)
            {
                Logger.Log(Logger.LogType.DEBUG, $"�f�B���N�g�����̃t�@�C�����ċA�I�ɏ������܂�: '{file}'");
                // �ċA�Ăяo���i�t�@�C���̏ꍇ�͏�L���������s�����j
                RemoveUsingfileRemover(file);
            }

            Logger.Log(Logger.LogType.DEBUG, $"�f�B���N�g�� '{filePath}' �̃T�u�f�B���N�g���ꗗ���擾���܂��B");
            // ���ɁA�T�u�f�B���N�g�����ċA�I�ɏ���
            string[] subDirectories = GetDirectoriesInDirectory(filePath);
            Logger.Log(Logger.LogType.DEBUG, $"GetDirectoriesInDirectory ����: {FormatArrayForLog(subDirectories)}");
            foreach (string subDir in subDirectories)
            {
                Logger.Log(Logger.LogType.DEBUG, $"�T�u�f�B���N�g�����ċA�I�ɏ������܂�: '{subDir}'");
                RemoveUsingfileRemover(subDir);
            }

            // �S�Ă̒��g���폜�ł����̂ŁA���݂̃f�B���N�g�����폜
            Logger.Log(Logger.LogType.DEBUG, $"�S�Ă̒��g�̍폜�������B�f�B���N�g�� '{filePath}' �̍폜�����݂܂��B");
            try
            {
                Directory.Delete(filePath, false);
                Logger.Log(Logger.LogType.DEBUG, $"�f�B���N�g�� '{filePath}' �̍폜�ɐ������܂����B");
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{filePath}' �̍폜�Ɏ��s���܂����B�G���[: {e.Message}");
                Logger.Log(Logger.LogType.DEBUG, $"��O�ڍ�: {e}");
            }
        }

        // �w�肵���f�B���N�g�������̃t�@�C���ꗗ���擾����
        private static string[] GetFilesInDirectory(string path)
        {
            Logger.Log(Logger.LogType.DEBUG, $"GetFilesInDirectory �J�n: path = '{path}'");
            try
            {
                string[] files = Directory.GetFiles(path, "*");
                Logger.Log(Logger.LogType.DEBUG, $"�f�B���N�g�� '{path}' �̃t�@�C���擾�ɐ����B�擾����: {files.Length}. ���e: {FormatArrayForLog(files)}");
                return files;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̃t�@�C���擾�ɃA�N�Z�X���ۂ��������܂����B�G���[: {ex.Message}");
                Logger.Log(Logger.LogType.DEBUG, $"�A�N�Z�X���ۗ�O�����B�����C�������݂܂��B path = '{path}'");
                FixDirectoryPermissions(path);
                try
                {
                    string[] files = Directory.GetFiles(path, "*");
                    Logger.Log(Logger.LogType.DEBUG, $"�����C����A�f�B���N�g�� '{path}' �̃t�@�C���擾�ɐ����B�擾����: {files.Length}. ���e: {FormatArrayForLog(files)}");
                    return files;
                }
                catch (Exception ex2)
                {
                    Logger.Log(Logger.LogType.ERROR, $"�����C������f�B���N�g�� '{path}' �̃t�@�C���擾�Ɏ��s���܂����B�G���[: {ex2.Message}");
                    Logger.Log(Logger.LogType.DEBUG, $"�Ď��s���s�B��O�ڍ�: {ex2}");
                    return new string[0];
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̃t�@�C���擾�Ɏ��s���܂����B�G���[: {ex.Message}");
                Logger.Log(Logger.LogType.DEBUG, $"��ʗ�O�����B��O�ڍ�: {ex}");
                return new string[0];
            }
        }

        // �w�肵���f�B���N�g�������̃T�u�f�B���N�g���ꗗ���擾����
        private static string[] GetDirectoriesInDirectory(string path)
        {
            Logger.Log(Logger.LogType.DEBUG, $"GetDirectoriesInDirectory �J�n: path = '{path}'");
            try
            {
                string[] directories = Directory.GetDirectories(path);
                Logger.Log(Logger.LogType.DEBUG, $"�f�B���N�g�� '{path}' �̃T�u�f�B���N�g���擾�ɐ����B����: {directories.Length} / ���e: {FormatArrayForLog(directories)}");
                return directories;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̃T�u�f�B���N�g���擾�ɃA�N�Z�X���ۂ��������܂����B�G���[: {ex.Message}");
                Logger.Log(Logger.LogType.DEBUG, $"�A�N�Z�X���ۗ�O�����B�����C�������݂܂��B path = '{path}'");
                FixDirectoryPermissions(path);
                try
                {
                    string[] directories = Directory.GetDirectories(path);
                    Logger.Log(Logger.LogType.DEBUG, $"�����C����A�f�B���N�g�� '{path}' �̃T�u�f�B���N�g���擾�ɐ����B����: {directories.Length} / ���e: {FormatArrayForLog(directories)}");
                    return directories;
                }
                catch (Exception ex2)
                {
                    Logger.Log(Logger.LogType.ERROR, $"�����C������f�B���N�g�� '{path}' �̃T�u�f�B���N�g���擾�Ɏ��s���܂����B�G���[: {ex2.Message}");
                    Logger.Log(Logger.LogType.DEBUG, $"�Ď��s���s�B��O�ڍ�: {ex2}");
                    return new string[0];
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̃T�u�f�B���N�g���擾�Ɏ��s���܂����B�G���[: {ex.Message}");
                Logger.Log(Logger.LogType.DEBUG, $"��ʗ�O�����B��O�ڍ�: {ex}");
                return new string[0];
            }
        }

        // TrustedInstaller �Ƀt���R���g���[���̃A�N�Z�X����t�^
        private static void FixDirectoryPermissions(string path)
        {
            Logger.Log(Logger.LogType.DEBUG, $"FixDirectoryPermissions �J�n: path = '{path}'");
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                Logger.Log(Logger.LogType.DEBUG, $"DirectoryInfo ���쐬���܂���: '{path}'");
                DirectorySecurity ds = di.GetAccessControl();
                Logger.Log(Logger.LogType.DEBUG, $"DirectorySecurity ���擾���܂���: '{path}'");

                // TrustedInstaller �� NTAccount ���擾
                var trustedInstaller = new NTAccount("NT SERVICE", "TrustedInstaller");
                Logger.Log(Logger.LogType.DEBUG, $"NTAccount (TrustedInstaller) �𐶐����܂����B");

                // ���L�҂̐ݒ�
                ds.SetOwner(trustedInstaller);
                Logger.Log(Logger.LogType.DEBUG, $"���L�҂� TrustedInstaller �ɐݒ肵�܂����B");

                // TrustedInstaller �Ƀt���R���g���[����t�^
                var accessRule = new FileSystemAccessRule(
                    trustedInstaller,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                ds.AddAccessRule(accessRule);
                Logger.Log(Logger.LogType.DEBUG, $"TrustedInstaller �Ƀt���R���g���[��������t�^���郋�[����ǉ����܂����B");

                di.SetAccessControl(ds);
                Logger.Log(Logger.LogType.INFO, $"�f�B���N�g�� '{path}' �̏��L�҂���ь����� TrustedInstaller �ɏC�����܂����B");
                Logger.Log(Logger.LogType.DEBUG, $"FixDirectoryPermissions ����������Ɋ������܂���: '{path}'");
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̌����C���Ɏ��s���܂����B�G���[: {e.Message}");
                Logger.Log(Logger.LogType.DEBUG, $"FixDirectoryPermissions ��O�ڍ�: {e}");
            }
        }

        // �z��̓��e�����O�o�͗p�Ƀt�H�[�}�b�g
        private static string FormatArrayForLog(string[] array)
        {
            if (array == null)
            {
                return "null";
            }
            int maxItems = 5;
            string result = "[";
            for (int i = 0; i < array.Length && i < maxItems; i++)
            {
                result += $"'{array[i]}'";
                if (i < array.Length - 1 && i < maxItems - 1)
                {
                    result += ", ";
                }
            }
            if (array.Length > maxItems)
            {
                result += $", ...�i{array.Length - maxItems} ���ȗ��j";
            }
            result += "]";
            return result;
        }
    }
}
