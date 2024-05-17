using System;
using System.Collections.Generic;

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
        public global::System.Drawing.Size UnfoldedSize
        {
            get
            {
                return ((global::System.Drawing.Size)(this["UnfoldedSize"]));
            }
            set
            {
                this["UnfoldedSize"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("All")]
        public global::System.Windows.Forms.DragDropEffects DragEffect
        {
            get
            {
                return ((global::System.Windows.Forms.DragDropEffects)(this["DragEffect"]));
            }
            set
            {
                this["DragEffect"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public List<(string, List<string>)> History
        {
            get
            {
                return ((List<(string, List<string>)>)(this["History"]));
            }
            set
            {
                this["History"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int SelectedIndex
        {
            get
            {
                return ((int)(this["SelectedIndex"]));
            }
            set
            {
                this["SelectedIndex"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("16")]
        public int IconSize
        {
            get
            {
                return ((int)(this["IconSize"]));
            }
            set
            {
                this["IconSize"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("false")]
        public bool UseThumbnail
        {
            get
            {
                return ((bool)(this["UseThumbnail"]));
            }
            set
            {
                this["UseThumbnail"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("true")]
        public bool UseNavigate
        {
            get
            {
                return ((bool)(this["UseNavigate"]));
            }
            set
            {
                this["UseNavigate"] = value;
            }
        }
    }
}
