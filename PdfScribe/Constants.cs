using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfScribe
{
    public class Constants
    {
        public const string SvProductName = "Aivika";
        public const string ClientProductName = "Aivika Capture";
        public const string AllUsersInstalledToken = "Installed for all users";
        public const string PrinterInstalledOnlyToken = "Printer Installed Only";
        public const string PrinterUploadApplicationToken = "Printer Upload Application";
        public static readonly string ClientRegistryRootPath = $@"SOFTWARE\{SvProductName}";
        public static readonly string ClientRegistryKeyPath = $@"{ClientRegistryRootPath}\{ClientProductName}";

        private static string _clientDataFolder;
        public static string ClientDataFolder
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_clientDataFolder))
                    return _clientDataFolder;

                _clientDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AivikaClient");

                if (!System.IO.Directory.Exists(_clientDataFolder))
                {
                    System.IO.Directory.CreateDirectory(_clientDataFolder);
                }

                return _clientDataFolder;
            }
        }

        public static string PrintSpoolFolder => System.IO.Path.Combine(ClientDataFolder, "Spool");
    }
}
