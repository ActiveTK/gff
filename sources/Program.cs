using System;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Management;
using Microsoft.Win32;
using System.Threading;

namespace Goodbye_F__king_File
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // �Ǘ��Ҍ����ł͂Ȃ��ꍇ
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                // �G���[���b�Z�[�W���o��
                Logger.Log(Logger.LogType.ERROR, "�G���[: �Ǘ��Ҍ����Ŏ��s���Ă��������B");

                // ���[�U�[�ɊǗ��Ҍ����ōċN�����邩�₢���킹��
                if (Logger.AskYorN("�Ǘ��Ҍ����ōċN�����܂����H", true))
                {
                    if (CallMySelfRunAs(Process.GetCurrentProcess().MainModule.FileName, string.Join(" ", args), false))
                    {
                        // �ċN�������̂Ō��݂̃v���Z�X���I��
                        Environment.Exit(0);
                    }
                    else
                    {
                        // �Ǘ��Ҍ����̗v���Ɏ��s�����ꍇ
                        // UAC ���o�C�p�X���邩�q�˂�
                        if (Logger.AskYorN("���[�U�[�A�J�E���g������o�C�p�X���čċN�����܂����H", true))
                        {
                            if (CallMySelfRunAs(Process.GetCurrentProcess().MainModule.FileName, string.Join(" ", args), true))
                            {
                                // �ċN�������̂Ō��݂̃v���Z�X���I��
                                Environment.Exit(0);
                            }
                            else
                            {
                                Console.ReadKey();
                                Environment.Exit(-1);
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            // TrustedInstaller�Ŏ��s����Ă��邩�m�F
            bool isTrustedInstaller = false;
            foreach (IdentityReference group in identity.Groups)
            {
                try
                {
                    if (string.Equals(group.Translate(typeof(NTAccount)).ToString(), "NT SERVICE\\TrustedInstaller", StringComparison.OrdinalIgnoreCase))
                        isTrustedInstaller = true;
                }
                catch
                {
                    if (string.Equals(group.ToString(), "NT SERVICE\\TrustedInstaller", StringComparison.OrdinalIgnoreCase))
                        isTrustedInstaller = true;
                }
            }

            // �f�o�b�O���b�Z�[�W��\��
            if (CheckIfDebug())
            {
                Logger.ShowDebug = true;
            }

            // TrustedInstaller�ɏ��i����O�ɓ��͂��K�v�ȏ���������������
            string filePath = null;

            // TrustedInstaller�Ɍ������i
            if (!isTrustedInstaller)
            {
                string IfDebug = Logger.ShowDebug ? "[DEBUG] " : "";
                // �X�^�[�g�\��
                Console.WriteLine("**********************************************************************");
                Console.WriteLine("** Goodbye F**king Files " + IfDebug + "/ build 1 Apr, 2025");
                Console.WriteLine("** (c) 2025 ActiveTK. <+activetk.jp>");
                Console.WriteLine("** Released under the MIT License");
                Console.WriteLine("**********************************************************************");

                // �������n����Ă��Ȃ���΃��[�U�[�ɓ��͂����߂�
                if (args.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("** arg[0] => (string)FilePath > ");
                    Console.ResetColor();
                    filePath = Console.ReadLine();
                    Console.WriteLine("**********************************************************************");
                }
                else
                {
                    filePath = string.Join(" ", args);
                }
                // ���͒l�̑O��̋󔒂�����
                filePath = filePath.Trim();
                // ��d���p���ň͂܂�Ă����ꍇ�͏���
                if (filePath.StartsWith("\"") && filePath.EndsWith("\""))
                {
                    filePath = filePath.Substring(1, filePath.Length - 2);
                }
                // ���΃p�X�̏ꍇ�A�J�����g�f�B���N�g������ɐ�΃p�X�ɕϊ�
                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Path.GetFullPath(filePath);
                }

                // �m�F
                if (Directory.Exists(filePath))
                {
                    if (!Logger.AskYorN($"�{���Ƀf�B���N�g�� '{filePath}' ���̑S�t�@�C�����폜���܂����H", true))
                        return;
                    if (!Logger.AskYorN($"�{���̖{���ɍ폜���܂����H����x�m�F���Ă��������B(���̑���͕s�t�I�ł��I)", true))
                        return;
                    if (IsDriveRoot(filePath) && !VerifyAllowedDangerOps())
                    {
                        Console.WriteLine("**  �{���̖{���̖{���ɍ폜���܂����H");
                        Console.WriteLine("** ���ݓI�Ɋ댯���̍����p�X���w�肳��Ă���A���̃h���C�u {filePath} ���̃t�@�C���͑S�č폜����܂��B");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("** ���s����ɂ́A�t���� README.md �̖����u# AllowDangerOperation�v�̃R�����g�A�E�g���������Ă��������B");
                        Console.ResetColor();
                        Console.Write("�ҋ@���Ă��܂�");
                        int count = 0;
                        while (true)
                        {
                            count++;
                            if (count % 20 == 0)
                                Console.Write(".");
                            if (VerifyAllowedDangerOps())
                                break;
                            else
                                Thread.Sleep(200);
                        }
                        Console.WriteLine("�F�؂ɐ������܂����B�������J�n���܂��B");
                    }
                    else if (filePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("** �{���̖{���̖{���ɍ폜���܂����H");
                        Console.WriteLine("** ���ݓI�Ɋ댯���̍����p�X���w�肳��Ă���A���̃f�B���N�g����OS���\������V�X�e���t�@�C���̈ꕔ�ł��B");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("** ���s����ɂ́A�t���� README.md �̖����u# AllowDangerOperation�v�̃R�����g�A�E�g���������Ă��������B");
                        Console.ResetColor();
                        Console.Write("** �ҋ@���Ă��܂�");
                        int count = 0;
                        while (true)
                        {
                            count++;
                            if (count % 20 == 0)
                                Console.Write(".");
                            if (VerifyAllowedDangerOps())
                                break;
                            else
                                Thread.Sleep(200);
                        }
                        Console.WriteLine("�F�؂ɐ������܂����B�������J�n���܂��B");
                    }
                }
                else
                {
                    if (!Logger.AskYorN($"�{���Ƀt�@�C�� '{filePath}' ���폜���܂����H", true))
                        return;
                }
                Console.WriteLine("**********************************************************************");

                TrustedInstallerRunner.Run("\"" + Process.GetCurrentProcess().MainModule.FileName + "\" " + filePath);

                Logger.Log(Logger.LogType.INFO, "�������������܂����B");
                Console.WriteLine("**********************************************************************");

                RequireKeyIfCMD();

                return;
            }

            filePath = string.Join(" ", args);

            FileAndDirectoryProcessor.RemoveUsingfileRemover(filePath);
        }
        static bool VerifyAllowedDangerOps()
        {
            try
            {
                string readmefp = Path.GetFullPath(@".\README.md");
                if (!File.Exists(readmefp))
                {
                    InitREADME();
                    return false;
                }
                foreach (string line in File.ReadAllLines(readmefp))
                {
                    if (line.Trim().StartsWith("AllowDangerOperation", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch(Exception e)
            {
                Logger.Log(Logger.LogType.ERROR, $"README.md �̓ǂݎ��Ɏ��s���܂���: {e.Message}");
            }
            return false;
        }
        static bool CheckIfDebug()
        {
            try
            {
                string readmefp = Path.GetFullPath(@".\README.md");
                if (!File.Exists(readmefp))
                {
                    InitREADME();
                    return false;
                }
                foreach (string line in File.ReadAllLines(readmefp))
                {
                    if (line.Trim().StartsWith("ShowDebugMessages", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogType.ERROR, $"README.md �̓ǂݎ��Ɏ��s���܂���: {e.Message}");
            }
            return false;
        }
        static void InitREADME()
        {
            File.WriteAllText(@".\README.md",
                "This README.md is auto-generated;" + Environment.NewLine +
                Environment.NewLine +
                "# ShowDebugMessages" + Environment.NewLine +
                "# AllowDangerOperation" + Environment.NewLine
            );
        }
        static bool IsDriveRoot(string path)
        {
            string fullPath = Path.GetFullPath(path).TrimEnd('\\') + "\\";
            string root = Path.GetPathRoot(fullPath);
            return string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase);
        }
        static bool CallMySelfRunAs(string file, string arg, bool BypassUAC)
        {
            if (BypassUAC)
            {
                try
                {
                    // ���W�X�g���L�[�̍쐬�i���ɑ��݂���ꍇ�͏㏑���j
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\ms-settings\Shell\Open\command"))
                    {
                        if (key == null)
                        {
                            Logger.Log(Logger.LogType.ERROR, "���W�X�g���L�[�̍쐬�Ɏ��s���܂����B");
                            return false;
                        }

                        key.SetValue("DelegateExecute", "", RegistryValueKind.String);
                        key.SetValue("", "\"" + file + "\" "+ arg, RegistryValueKind.String);
                    }

                    // UAC �� bypass ����
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32") + @"\fodhelper.exe",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process.Start(psi);

                    Thread.Sleep(3000);

                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\ms-settings", false);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log(Logger.LogType.ERROR, $"���[�U�[�A�J�E���g����̃o�C�p�X�Ɏ��s���܂���: {ex.Message}");
                    return false;
                }
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = arg,
                    UseShellExecute = true,
                    Verb = "runas"  // �Ǘ��Ҍ����ł̋N����v��
                };

                try
                {
                    Process.Start(startInfo);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log(Logger.LogType.ERROR, $"�Ǘ��Ҍ����ł̍ċN���Ɏ��s���܂���: {ex.Message}");
                    return false;
                }
            }
        }
        private static void RequireKeyIfCMD()
        {
            Process currentProcess = Process.GetCurrentProcess();
            int parentPid = 0;
            // WMI��ManagementObject�𗘗p���āA�e�v���Z�XID���擾
            using (ManagementObject mo = new ManagementObject($"win32_process.handle='{currentProcess.Id}'"))
            {
                mo.Get();
                parentPid = Convert.ToInt32(mo["ParentProcessId"]);
            }

            Process parentProcess = null;
            try
            {
                parentProcess = Process.GetProcessById(parentPid);
            }
            catch (ArgumentException)
            {
            
            }

            if (parentProcess != null && parentProcess.ProcessName.ToLower().Contains("cmd"))
            {
                return;
            }
            else
            {
                Console.Write("�����L�[�������ƏI�����܂�...");
                Console.ReadKey();
                Console.WriteLine();
            }
        }
    }
}
