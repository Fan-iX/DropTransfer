namespace DropTransfer.Properties
{
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.3.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {

        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100, 100")]
        public global::System.Drawing.Point WindowLocation
        {
            get
            {
                return ((global::System.Drawing.Point)(this["WindowLocation"]));
            }
            set
            {
                this["WindowLocation"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("400, 350")]
        public global::System.Drawing.Size FullSize
        {
            get
            {
                return ((global::System.Drawing.Size)(this["FullSize"]));
            }
            set
            {
                this["FullSize"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public global::System.Collections.Generic.List<string> Favorites
        {
            get
            {
                return ((global::System.Collections.Generic.List<string>)(this["Favorites"]));
            }
            set
            {
                this["Favorites"] = value;
            }
        }
    }
}
