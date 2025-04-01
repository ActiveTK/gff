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
            // �t�@�C���̏ꍇ�͒��ڍ폜
            if (!Directory.Exists(filePath))
            {
                var fileRemover = new FileRemover(filePath);
                if (fileRemover.VerifyIfItsValid())
                {
                    fileRemover.ForceRMFile();
                }
                else
                {
                    Logger.Log(Logger.LogType.ERROR, "�G���[: " + fileRemover.VerifyError);
                }
                return;
            }

            // �f�B���N�g���̏ꍇ�́A�܂������̃t�@�C�����폜
            string[] files = GetFilesInDirectory(filePath);
            foreach (string file in files)
            {
                // �ċA�Ăяo���i�t�@�C���̏ꍇ�͏�L���������s�����j
                RemoveUsingfileRemover(file);
            }

            // ���ɁA�T�u�f�B���N�g�����ċA�I�ɏ���
            string[] subDirectories = GetDirectoriesInDirectory(filePath);
            foreach (string subDir in subDirectories)
            {
                RemoveUsingfileRemover(subDir);
            }

            // �S�Ă̒��g���폜�ł����̂ŁA���݂̃f�B���N�g�����폜
            try
            {
                Directory.Delete(filePath, false);
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{filePath}' �̍폜�Ɏ��s���܂����B�G���[: {e.Message}");
            }
        }

        // �w�肵���f�B���N�g�������̃t�@�C���ꗗ���擾����
        private static string[] GetFilesInDirectory(string path)
        {
            try
            {
                return Directory.GetFiles(path, "*");
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̃t�@�C���擾�ɃA�N�Z�X���ۂ��������܂����B�G���[: {ex.Message}");
                FixDirectoryPermissions(path);
                try
                {
                    return Directory.GetFiles(path, "*");
                }
                catch (Exception ex2)
                {
                    Logger.Log(Logger.LogType.ERROR, $"�����C������f�B���N�g�� '{path}' �̃t�@�C���擾�Ɏ��s���܂����B�G���[: {ex2.Message}");
                    return new string[0];
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̃t�@�C���擾�Ɏ��s���܂����B�G���[: {ex.Message}");
                return new string[0];
            }
        }

        // �w�肵���f�B���N�g�������̃T�u�f�B���N�g���ꗗ���擾����
        private static string[] GetDirectoriesInDirectory(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̃T�u�f�B���N�g���擾�ɃA�N�Z�X���ۂ��������܂����B�G���[: {ex.Message}");
                FixDirectoryPermissions(path);
                try
                {
                    return Directory.GetDirectories(path);
                }
                catch (Exception ex2)
                {
                    Logger.Log(Logger.LogType.ERROR, $"�����C������f�B���N�g�� '{path}' �̃T�u�f�B���N�g���擾�Ɏ��s���܂����B�G���[: {ex2.Message}");
                    return new string[0];
                }
            }
            catch (Exception ex)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̃T�u�f�B���N�g���擾�Ɏ��s���܂����B�G���[: {ex.Message}");
                return new string[0];
            }
        }

        // TrustedInstaller �Ƀt���R���g���[���̃A�N�Z�X����t�^
        private static void FixDirectoryPermissions(string path)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                DirectorySecurity ds = di.GetAccessControl();

                // TrustedInstaller �� NTAccount ���擾
                var trustedInstaller = new NTAccount("NT SERVICE", "TrustedInstaller");

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
                Logger.Log(Logger.LogType.INFO, $"�f�B���N�g�� '{path}' �̏��L�҂���ь����� TrustedInstaller �ɏC�����܂����B");
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogType.ERROR, $"�f�B���N�g�� '{path}' �̌����C���Ɏ��s���܂����B�G���[: {e.Message}");
            }
        }
    }
}