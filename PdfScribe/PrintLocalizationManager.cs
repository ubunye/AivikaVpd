using Telerik.Windows.Controls;

namespace PdfScribe
{
    public class PrintLocalizationManager : LocalizationManager
    {
        public override string GetStringOverride(string key)
        {
            switch (key)
            {
                case "Close":
                    return "Close";
            }
            return base.GetStringOverride(key);
        }
    }
}
