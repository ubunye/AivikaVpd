using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PdfScribe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string _fileName;
        private string _printFolder;

        public bool Printing { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            Printing = false;
            TopTextBlock.Text = "Preparing print...";
            Header = "Aivika Printer";

            Icon = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/PdfScribe;component/Graphics/Aivika.ico", UriKind.Absolute))
            };

            if (!Directory.Exists(Constants.PrintSpoolFolder))
            {
                Directory.CreateDirectory(Constants.PrintSpoolFolder);
            }
            Program.PrintStartedEvent += PrintHelper_PrintStartedEvent;
            var guid = Guid.NewGuid().ToString("B").ToUpper();
            _printFolder = System.IO.Path.Combine(Constants.PrintSpoolFolder, guid);

            Directory.CreateDirectory(_printFolder);

            _fileName = System.IO.Path.Combine(_printFolder, $"Aivika Capture Document {guid}.pdf");
            TopTextBlock.Text = "Print started.";
            Task.Run(Program.Print);
        }

        private void PrintHelper_PrintStartedEvent(object sender)
        {
            Printing = true;
            Program.GetPrintedFile(_fileName);
            PrintJobDone();
        }

        private void PrintJobDone()
        {
            try
            {
                RunOnMainThread(() => TopTextBlock.Text = "Print completed.");
                Printing = false;
                if (!File.Exists(PrinterDriver.ShellExe))
                {
                    System.Windows.Forms.MessageBox.Show("Unable to locate a required Aivika Capture component.",
                            "Aivika Printer",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);
                }
                else if (!string.IsNullOrWhiteSpace(_fileName))
                {
                    Process.Start(PrinterDriver.ShellExe, $"-u \"{_fileName}\" -d");
                }

                RunOnMainThread(() => System.Windows.Application.Current.Shutdown());

            }
            catch (Exception ex)
            {
                //Logger.Error(ex.Message);
                DoCleanup(); 
                System.Windows.Forms.MessageBox.Show(ex.Message,
                            "Aivika Printer",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private void RunOnMainThread(Action action)
        {
            void ThreadStart1()
            {
                if (action != null) Dispatcher.BeginInvoke(action);
            }

            var t2 = new Thread(ThreadStart1);
            t2.Start();

        }

        public void Dispose()
        {
        }

        public void CancelPrintJob()
        {
            if (File.Exists(_fileName))
            {
                File.Delete(_fileName);
            }
        }

        private void DoCleanup()
        {
            try
            {
                if (!Directory.Exists(_printFolder))
                    return;

                var files = Directory.GetFiles(_printFolder);

                foreach (var file in files)
                {
                    File.Delete(file);
                }

                Directory.Delete(_printFolder);
            }
            catch
            {
                // If this fails then there's nothing to be done.
            }
        }
    }
}
