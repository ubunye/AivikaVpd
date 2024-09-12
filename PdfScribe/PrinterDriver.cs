using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
