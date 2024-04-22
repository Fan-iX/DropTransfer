using System;
using System.Configuration;
using System.Windows.Forms;
using System.Collections.Specialized;

namespace DropTransfer
{
    static class Program
    {
        public static transDropBucket browser;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            NameValueCollection section = ConfigurationManager.GetSection("System.Windows.Forms.ApplicationConfigurationSection") as NameValueCollection;
            if (section != null)
            {
                section["DpiAwareness"] = "PerMonitorV2";
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            browser = new transDropBucket();
            Application.AddMessageFilter(new MouseMoveFilter());
            Application.Run(browser);
        }
        public class MouseMoveFilter : IMessageFilter
        {
            bool mouseOut = true;
            #region IMessageFilter Members
            private const int WM_MOUSELEAVE = 0x02A3;
            private const int WM_NCMOUSEMOVE = 0x0A0;
            private const int WM_MOUSEMOVE = 0x0200;
            private const int WM_NCMOUSELEAVE = 0x2A2;

            public bool PreFilterMessage(ref Message m)
            {
                switch (m.Msg)
                {
                    case WM_NCMOUSEMOVE:
                    case WM_MOUSEMOVE:
                        if (mouseOut)
                        {
                            browser.OnMouseEnterWindow();
                            mouseOut = false;
                        }
                        break;
                    case WM_NCMOUSELEAVE:
                    case WM_MOUSELEAVE:
                        if (!browser.Bounds.Contains(Control.MousePosition))
                        {
                            browser.OnMouseLeaveWindow();
                            mouseOut = true;
                        }
                        break;
                }
                return false;
            }

            #endregion
        }
    }
}
