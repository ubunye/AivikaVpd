using Microsoft.Win32;
using System;
using System.Reflection;
using System.IO;
using PdfScribeInstallCustomAction;
using System.Windows.Forms;

namespace PdfScribe
{
    public static class PrinterDriver
    {
        public static string ShellExe
        {
            get
            {
                try
                {
                    using (var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        var aivikaKey = localKey.OpenSubKey(Constants.ClientRegistryKeyPath,
                            RegistryKeyPermissionCheck.ReadSubTree);
                        var allUsersValue = aivikaKey?.GetValue(Constants.AllUsersInstalledToken);

                        if (allUsersValue == null)
                        {
                            //LogInfo(string.Format(Resources.ValueNotSet, Common.Constants.AllUsersInstalledToken));
                            return null;
                        }

                        if (bool.Parse(allUsersValue.ToString()))
                        {
                            var printerUploadValue = aivikaKey.GetValue(Constants.PrinterUploadApplicationToken);

                            return (string)printerUploadValue;
                        }

                        using (var currentUserKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                        {
                            var key = currentUserKey.OpenSubKey(Constants.ClientRegistryKeyPath, RegistryKeyPermissionCheck.ReadSubTree);
                            var value = key?.GetValue(Constants.PrinterUploadApplicationToken);

                            return (string)value;
                        }
                    }
                }
                catch (Exception)
                {
                    //LogError(ex.Message);
                    return null;
                }
            }
        }

        public static bool? PrinterInsstalledOnlyKey
        {
            get
            {
                try
                {
                    using (var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        var aivikaKey = localKey.OpenSubKey(Constants.ClientRegistryKeyPath,
                            RegistryKeyPermissionCheck.ReadSubTree);
                        var allUsersValue = aivikaKey?.GetValue(Constants.AllUsersInstalledToken);

                        if (allUsersValue == null)
                        {
                            //LogInfo(string.Format(Resources.ValueNotSet, Common.Constants.AllUsersInstalledToken));
                            return null;
                        }

                        if (bool.Parse(allUsersValue.ToString()))
                        {
                            var printerUploadValue = aivikaKey.GetValue(Constants.PrinterInstalledOnlyToken);

                            return Convert.ToBoolean(printerUploadValue);
                        }

                        using (var currentUserKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                        {
                            var key = currentUserKey.OpenSubKey(Constants.ClientRegistryKeyPath, RegistryKeyPermissionCheck.ReadSubTree);
                            var value = key?.GetValue(Constants.PrinterInstalledOnlyToken);
                            
                            return Convert.ToBoolean(value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //LogError(ex.Message);
                    return null;
                }
            }
        }

        private static readonly string ExecutableSourceDirectory =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static readonly PdfScribeInstaller PdfScribeInstaller = new PdfScribeInstaller();

        private static readonly string OutputHandlerCommand =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "AivikaVirtualPrinter.exe");

        public static bool InstallPrinter()
        {
            return PdfScribeInstaller.InstallPdfScribePrinter(ExecutableSourceDirectory, OutputHandlerCommand, "");
        }

        public static bool UnInstallPrinter()
        {
            return PdfScribeInstaller.UninstallPdfScribePrinter();
        }
    }
}
