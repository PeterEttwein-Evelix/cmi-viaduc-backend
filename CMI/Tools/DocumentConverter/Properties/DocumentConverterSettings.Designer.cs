﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CMI.Tools.DocumentConverter.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.7.0.0")]
    internal sealed partial class DocumentConverterSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static DocumentConverterSettings defaultInstance = ((DocumentConverterSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new DocumentConverterSettings())));
        
        public static DocumentConverterSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Tools.DocumentConverter.Properties.DocumentConverterSettings.SftpLicenseKey" +
            "@@")]
        public string SftpLicenseKey {
            get {
                return ((string)(this["SftpLicenseKey"]));
            }
        }
    }
}
