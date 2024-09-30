using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Threading;


namespace PdfScribe
{
    public class Program
    {
        #region Message constants
        private static readonly string ErrorDialogCaption = Properties.Resources.ProductCaption; // Error taskdialog caption text

        private static readonly string ErrorDialogInstructionPDFGeneration = Properties.Resources.ErrorDialogInstructionPDFGeneration;
        private static readonly string ErrorDialogInstructionCouldNotWrite = Properties.Resources.ErrorDialogInstructionCouldNotWrite;
        private static readonly string ErrorDialogInstructionUnexpectedError = Properties.Resources.ErrorDialogInstructionUnexpectedError;

        private static readonly string ErrorDialogOutputFilenameInvalid = Properties.Resources.ErrorDialogOutputFilenameInvalid;
        private static readonly string ErrorDialogOutputFilenameTooLong = Properties.Resources.ErrorDialogOutputFilenameTooLong;
        private static readonly string ErrorDialogOutputFileAccessDenied = Properties.Resources.ErrorDialogOutputFileAccessDenied;
        private static readonly string ErrorDialogTextFileInUse = Properties.Resources.ErrorDialogTextFileInUse;
        private static readonly string ErrorDialogTextGhostScriptConversion = Properties.Resources.ErrorDialogTextGhostScriptConversion;

        private static readonly string WarnFileNotDeleted = Properties.Resources.WarnFileNotDeleted;
        private static readonly string ErrorDialogSpoolService = Properties.Resources.ErrorDialogSpoolService;
        #endregion

        #region Other constants
        const string TraceSourceName = "AivikaVpd";

        static Process _parentProcess;
        #endregion

        private static readonly TraceSource LogEventSource = new TraceSource(TraceSourceName);

        private static string _fileName;
        private static string _printFolder;
        private static Dispatcher _dispatcher;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                switch (args[0])
                {
                    //case "-s":
                    //    ShowSetupWindow();
                    //    base.OnStartup(e);
                    //    break;
                    case "-i":
                        PerformTask(() =>
                        {
                            PrinterDriver.InstallPrinter();
                        });
                        break;
                    case "-u":
                        PerformTask(() =>
                        {
                            PrinterDriver.UnInstallPrinter();
                        });
                        break;
                    default:
                        RunTask();
                        break;
                }
            }
            else
            {
                RunTask();
            }
        }
        private static void PerformTask(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          ErrorDialogInstructionUnexpectedError +
                                          Environment.NewLine +
                                          string.Format(Properties.Resources.ErrorExceptionMsg, ex.Message));
            }
        }

        private static void RunTask()
        {
            if (!Directory.Exists(Constants.PrintSpoolFolder))
            {
                Directory.CreateDirectory(Constants.PrintSpoolFolder);
            }
            var guid = Guid.NewGuid().ToString("B").ToUpper();
            _printFolder = Path.Combine(Constants.PrintSpoolFolder, guid);

            Directory.CreateDirectory(_printFolder);

            _fileName = Path.Combine(_printFolder, $"Aivika Capture Document {guid}.pdf");
            GetPrintedFile(_fileName);
            PrintJobDone();
        }

        private static void PrintJobDone()
        {
            try
            {
                if (!File.Exists(PrinterDriver.ShellExe))
                {
                    MessageBox.Show(Properties.Resources.ErrorUnableLocateAivika,
                            ErrorDialogCaption,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly);
                }
                else if (!string.IsNullOrWhiteSpace(_fileName))
                {
                    Process.Start(PrinterDriver.ShellExe, $"-u \"{_fileName}\" -d");
                }
            }
            catch (Exception ex)
            {
                DoCleanup();
                MessageBox.Show(ex.Message,
                            ErrorDialogCaption,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private static void DoCleanup()
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

        [STAThread]
        public static void GetPrintedFile(string fileName)
        {
            // Install the global exception handler
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Application_UnhandledException);

            _parentProcess = ProcessHelper.GetParentProcess();
            if (_parentProcess == null || _parentProcess.ProcessName != "spoolsv")
            {
                if (Environment.UserInteractive)
                {
                    MessageBox.Show(ErrorDialogSpoolService,
                        ErrorDialogCaption,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.DefaultDesktopOnly);
                }
                return;
            }

            String standardInputFilename = Path.GetTempFileName();
            String outputFilename = fileName;//String.Empty;

            try
            {
                using (BinaryReader standardInputReader = new BinaryReader(Console.OpenStandardInput()))
                {
                    using (FileStream standardInputFile = new FileStream(standardInputFilename, FileMode.Create, FileAccess.ReadWrite))
                    {
                        standardInputReader.BaseStream.CopyTo(standardInputFile);
                    }
                }

                StripNoDistill(standardInputFilename);

                if (GetPdfOutputFilename(ref outputFilename))
                {
                    // Remove the existing PDF file if present
                    File.Delete(outputFilename);
                    // Only set absolute minimum parameters, let the postscript input
                    // dictate as much as possible
                    String[] ghostScriptArguments = { "-dBATCH", "-dNOPAUSE", "-dSAFER", "-dAutoRotatePages=/None",  "-sDEVICE=pdfwrite", $"-sOutputFile={outputFilename}", 
                        standardInputFilename, "-c", @"[/Creator(AivikaVpd " + Assembly.GetExecutingAssembly().GetName().Version + " (PSCRIPT5)) /DOCINFO pdfmark", "-f"};
                    GhostScript64.CallAPI(ghostScriptArguments);
                    //DisplayPdf(outputFilename);
                }
            }
            catch (IOException ioEx)
            {
                // We couldn't delete, or create a file
                // because it was in use
                LogEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          ErrorDialogInstructionCouldNotWrite +
                                          Environment.NewLine +
                                          string.Format(Properties.Resources.ErrorExceptionMsg, ioEx.Message));
                DisplayErrorMessage(ErrorDialogCaption,
                                    ErrorDialogInstructionCouldNotWrite + Environment.NewLine +
                                    String.Format(Properties.Resources.ErrorInUse, outputFilename));
            }
            catch (UnauthorizedAccessException unauthorizedEx)
            {
                // Couldn't delete a file
                // because it was set to readonly
                // or couldn't create a file
                // because of permissions issues
                LogEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          ErrorDialogInstructionCouldNotWrite +
                                          Environment.NewLine +
                                          string.Format(Properties.Resources.ErrorExceptionMsg, unauthorizedEx.Message));
                DisplayErrorMessage(ErrorDialogCaption,
                                    ErrorDialogInstructionCouldNotWrite + Environment.NewLine +
                                    String.Format(Properties.Resources.ErrorInsufficientPrivileges, outputFilename));


            }
            catch (ExternalException ghostscriptEx)
            {
                // Ghostscript error
                LogEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          String.Format(ErrorDialogTextGhostScriptConversion, ghostscriptEx.ErrorCode.ToString()) +
                                          Environment.NewLine +
                                          string.Format(Properties.Resources.ErrorExceptionMsg, ghostscriptEx.Message));
                DisplayErrorMessage(ErrorDialogCaption,
                                    ErrorDialogInstructionPDFGeneration + Environment.NewLine +
                                    String.Format(ErrorDialogTextGhostScriptConversion, ghostscriptEx.ErrorCode.ToString()));

            }
            finally
            {
                try
                {
                    File.Delete(standardInputFilename);
                }
                catch
                {
                    LogEventSource.TraceEvent(TraceEventType.Warning,
                                              (int)TraceEventType.Warning,
                                              String.Format(WarnFileNotDeleted, standardInputFilename));
                }
                LogEventSource.Flush();
            }
        }

        /// <summary>
        /// All unhandled exceptions will bubble their way up here -
        /// a final error dialog will be displayed before the crash and burn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogEventSource.TraceEvent(TraceEventType.Critical,
                                      (int)TraceEventType.Critical,
                                      ((Exception)e.ExceptionObject).Message + Environment.NewLine + 
                                      ((Exception)e.ExceptionObject).StackTrace);

            var message = ((Exception)e.ExceptionObject).Message;
            DisplayErrorMessage(ErrorDialogCaption,
                //ErrorDialogInstructionUnexpectedError);
                !string.IsNullOrEmpty(message) ? message : ErrorDialogInstructionUnexpectedError);
        }

        private static bool GetPdfOutputFilename(ref String outputFile)
        {
            bool filenameRetrieved = false;
            //outputFile = GetFreeRdpOutputFilename(_parentProcess);
            if (!string.IsNullOrEmpty(outputFile))
            {
                filenameRetrieved = true;
            }
            else
            {
                switch (Properties.Settings.Default.AskUserForOutputFilename)
                {
                    case (true):
                        using (SetOutputFilename dialogOwner = new SetOutputFilename())
                        {
                            dialogOwner.TopMost = true;
                            dialogOwner.TopLevel = true;
                            dialogOwner.Show(); // Form won't actually show - Application.Run() never called
                                                // but having a topmost/toplevel owner lets us bring the SaveFileDialog to the front
                            dialogOwner.BringToFront();
                            using (SaveFileDialog pdfFilenameDialog = new SaveFileDialog())
                            {
                                pdfFilenameDialog.AddExtension = true;
                                pdfFilenameDialog.AutoUpgradeEnabled = true;
                                pdfFilenameDialog.CheckPathExists = true;
                                pdfFilenameDialog.Filter = "pdf files (*.pdf)|*.pdf";
                                pdfFilenameDialog.ShowHelp = false;
                                pdfFilenameDialog.Title = Properties.Resources.ProductCaption + " - " + Properties.Resources.SetOutputFilename;
                                pdfFilenameDialog.ValidateNames = true;
                                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("REDMON_DOCNAME")))
                                {
                                    // Replace illegal characters with spaces
                                    Regex regEx = new Regex(@"[\\/:""*?<>|]");
                                    pdfFilenameDialog.FileName = regEx.Replace(Environment.GetEnvironmentVariable("REDMON_DOCNAME"), " ");
                                }
                                if (pdfFilenameDialog.ShowDialog(dialogOwner) == DialogResult.OK)
                                {
                                    outputFile = pdfFilenameDialog.FileName;
                                    filenameRetrieved = true;
                                }
                            }
                            dialogOwner.Close();
                        }
                        break;
                    default:
                        try
                        {
                            outputFile = GetOutputFilename();
                            // Test if we can write to the destination
                            FileStream newOutputFile = File.Create(outputFile);
                            File.Delete(outputFile);
                            newOutputFile.Dispose();
                            filenameRetrieved = true;
                        }
                        catch (Exception ex) when (ex is ArgumentException ||
                                                   ex is ArgumentNullException ||
                                                   ex is NotSupportedException ||
                                                   ex is DirectoryNotFoundException)
                        {
                            LogEventSource.TraceEvent(TraceEventType.Error,
                                                     (int)TraceEventType.Error,
                                                     ErrorDialogOutputFilenameInvalid + Environment.NewLine +
                                                     string.Format(Properties.Resources.ErrorExceptionMsg, ex.Message));
                            DisplayErrorMessage(ErrorDialogCaption,
                                                ErrorDialogOutputFilenameInvalid);
                        }
                        catch (PathTooLongException ex)
                        {
                            // filename is greater than 260 characters
                            LogEventSource.TraceEvent(TraceEventType.Error,
                                                     (int)TraceEventType.Error,
                                                     ErrorDialogOutputFilenameTooLong + Environment.NewLine +
                                                     string.Format(Properties.Resources.ErrorExceptionMsg, ex.Message));
                            DisplayErrorMessage(ErrorDialogCaption,
                                                ErrorDialogOutputFilenameTooLong);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            LogEventSource.TraceEvent(TraceEventType.Error,
                                                     (int)TraceEventType.Error,
                                                     ErrorDialogOutputFileAccessDenied + Environment.NewLine +
                                                     string.Format(Properties.Resources.ErrorExceptionMsg, ex.Message));
                            // Can't write to target dir
                            DisplayErrorMessage(ErrorDialogCaption,
                                                ErrorDialogOutputFileAccessDenied);
                        }
                        break;
                }
            }
            return filenameRetrieved;

        }

        //private static string GetFreeRdpOutputFilename(Process spooler)
        //{
        //    var filename = string.Empty;
        //    // myrtille print jobs are prefixed by "FREERDPjob" and concatenate the wfreerdp process id and a timestamp, thus should be unique and secure
        //    // the resulting pdf files are deleted once downloaded to the browser
        //    if (spooler?.StartInfo.EnvironmentVariables == null
        //        || string.IsNullOrEmpty(spooler.StartInfo.EnvironmentVariables["REDMON_DOCNAME"])
        //        || !spooler.StartInfo.EnvironmentVariables["REDMON_DOCNAME"].StartsWith("FREERDPjob"))
        //        return filename;
        //    var systemTempPath = Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine);
        //    var pdfFile = string.Concat(spooler.StartInfo.EnvironmentVariables["REDMON_DOCNAME"], ".pdf");
        //    if (systemTempPath != null)
        //        filename = Path.Combine(systemTempPath, pdfFile);
        //    return filename;
        //}

        private static String GetOutputFilename()
        {
            String outputFilename = Path.GetFullPath(Environment.ExpandEnvironmentVariables(Properties.Settings.Default.OutputFile));
            // Check if there are any % characters -
            // even though it's a legal Windows filename character,
            // it is a special character to Ghostscript
            if (outputFilename.Contains("%"))
                throw new ArgumentException(Properties.Resources.ErrorOutputFileContainInvalidCharacter);
            return outputFilename;
        }


        /// <summary>
        /// Opens the PDF in the default viewer
        /// if the OpenAfterCreating app setting is "True"
        /// and the file extension is .PDF
        /// </summary>
        /// <param name="pdfFilename"></param>
        static void DisplayPdf(String pdfFilename)
        {
            if (Properties.Settings.Default.OpenAfterCreating &&
                !String.IsNullOrEmpty(Path.GetExtension(pdfFilename)) &&
                (Path.GetExtension(pdfFilename).ToUpper() == ".PDF"))
            {
                Process.Start(pdfFilename);
            }
        }

        /// <summary>
        /// Displays up a topmost, OK-only message box for the error message
        /// </summary>
        /// <param name="boxCaption">The box's caption</param>
        /// <param name="boxMessage">The box's message</param>
        static void DisplayErrorMessage(String boxCaption,
                                        String boxMessage)
        {

            MessageBox.Show(boxMessage,
                            boxCaption,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly);

        }

        static void StripNoDistill(String postscriptFile)
        {
            if (Properties.Settings.Default.StripNoRedistill)
            {
                String strippedFile = Path.GetTempFileName();

                using (StreamReader inputReader = new StreamReader(File.OpenRead(postscriptFile), System.Text.Encoding.UTF8))
                using (StreamWriter strippedWriter = new StreamWriter(File.OpenWrite(strippedFile), new UTF8Encoding(false)))
                {
                    NoDistillStripping strippingStatus = NoDistillStripping.Searching;
                    String inputLine;
                    while (!inputReader.EndOfStream)
                    {
                        inputLine = inputReader.ReadLine();
                        if (inputLine != null)
                        {
                            switch ((int)strippingStatus)
                            {
                                case (int)NoDistillStripping.Searching:
                                    if (inputLine == "%ADOBeginClientInjection: DocumentSetup Start \"No Re-Distill\"")
                                        strippingStatus = NoDistillStripping.Removing;
                                    else
                                        strippedWriter.WriteLine(inputLine);
                                    break;
                                case (int)NoDistillStripping.Removing:
                                    if (inputLine == "%ADOEndClientInjection: DocumentSetup Start \"No Re-Distill\"")
                                        strippingStatus = NoDistillStripping.Complete;
                                    break;
                                case (int)NoDistillStripping.Complete:
                                    strippedWriter.WriteLine(inputLine);
                                    break;
                            }
                        }
                    }
                    strippedWriter.Close();
                    inputReader.Close();
                }

                File.Delete(postscriptFile);
                File.Move(strippedFile, postscriptFile);
            }

        }
    }
}
