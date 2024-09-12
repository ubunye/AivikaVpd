using System;
using System.Windows;
using System.Windows.Forms;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.ColorEditor;

namespace PdfScribe
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private MainWindow _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            LocalizationManager.Manager = new PrintLocalizationManager();
            SetPalette();

            ShowWindow();
        }

        private void ShowWindow()
        {
            _mainWindow = new MainWindow();

            _mainWindow.Loaded += (sender, args) =>
            {
                _mainWindow.Top = 50;
                _mainWindow.Left = 50;
            };

            _mainWindow.PreviewClosed += WdOnPreviewClosed;

            _mainWindow.Show();
        }

        private static void SetPalette()
        {
            MaterialPalette.Palette.AccentNormalColor = ColorConverter.ColorFromString("#FFFFAB40");
            MaterialPalette.Palette.AccentHoverColor = ColorConverter.ColorFromString("#FFFFD180");
            MaterialPalette.Palette.AccentPressedColor = ColorConverter.ColorFromString("#FFFF9100");
            MaterialPalette.Palette.DividerColor = ColorConverter.ColorFromString("#1E000000");
            MaterialPalette.Palette.IconColor = ColorConverter.ColorFromString("#FF000000");
            MaterialPalette.Palette.MainColor = ColorConverter.ColorFromString("#FFFFFFFF");
            MaterialPalette.Palette.MarkerColor = ColorConverter.ColorFromString("#FF000000");
            MaterialPalette.Palette.ValidationColor = ColorConverter.ColorFromString("#FFD50000");
            MaterialPalette.Palette.ComplementaryColor = ColorConverter.ColorFromString("#FFE0E0E0");
            MaterialPalette.Palette.AlternativeColor = ColorConverter.ColorFromString("#FFF5F5F5");
            MaterialPalette.Palette.MarkerInvertedColor = ColorConverter.ColorFromString("#FFFFFFFF");
            MaterialPalette.Palette.PrimaryColor = ColorConverter.ColorFromString("#FFFAFAFA");
            MaterialPalette.Palette.PrimaryNormalColor = ColorConverter.ColorFromString("#FF3E78B3");
            MaterialPalette.Palette.PrimaryFocusColor = ColorConverter.ColorFromString("#963E78B3");
            MaterialPalette.Palette.PrimaryHoverColor = ColorConverter.ColorFromString("#FF3E78B3");
            MaterialPalette.Palette.PrimaryPressedColor = ColorConverter.ColorFromString("#FFFFAB40");
            MaterialPalette.Palette.RippleColor = ColorConverter.ColorFromString("#FFFFFFFF");
            MaterialPalette.Palette.ReadOnlyBackgroundColor = ColorConverter.ColorFromString("#00FFFFFF");
            MaterialPalette.Palette.ReadOnlyBorderColor = ColorConverter.ColorFromString("#FFABABAB");
        }

        private void WdOnPreviewClosed(object o, WindowPreviewClosedEventArgs args)
        {
            if (_mainWindow.Printing)
            {
                DialogResult result = System.Windows.Forms.MessageBox.Show("Are you sure you want to cancel the current print job?",
                            "Aivika Printer",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);

                if (result == DialogResult.OK) 
                {
                    _mainWindow.CancelPrintJob();
                    _mainWindow.Dispose();
                }
                else
                {
                    args.Cancel = true;
                }
            }
            else
            {
                _mainWindow.Dispose();
            }
        }
    }
}
