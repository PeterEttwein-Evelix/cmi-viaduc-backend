﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CMI.Utilities.FormTemplate.Helper.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.8.1.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("de,en,fr,it")]
        public string SupportedLanguages {
            get {
                return ((string)(this["SupportedLanguages"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute(@"@@CMI.Utilities.FormTemplate.Helper.Properties.Settings.DefaultConnection.connectionString_Credentials@@;Server=@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.OracleHost@@;Direct=True;Service Name=@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.OracleServiceName@@;Port=1521")]
        public string DefaultConnection {
            get {
                return ((string)(this["DefaultConnection"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.OracleSchemaNam" +
            "e@@")]
        public string OracleSchemaName {
            get {
                return ((string)(this["OracleSchemaName"]));
            }
        }
    }
}
