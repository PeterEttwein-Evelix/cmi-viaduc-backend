﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CMI.Manager.DocumentConverter.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.8.1.0")]
    internal sealed partial class DocumentConverterSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static DocumentConverterSettings defaultInstance = ((DocumentConverterSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new DocumentConverterSettings())));
        
        public static DocumentConverterSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Temp\\ConverterBaseFolder")]
        public string BaseDirectory {
            get {
                return ((string)(this["BaseDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8032")]
        public int Port {
            get {
                return ((int)(this["Port"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("{MachineName}")]
        public string BaseAddress {
            get {
                return ((string)(this["BaseAddress"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string PathToAbbyyFrEngineDll {
            get {
                return ((string)(this["PathToAbbyyFrEngineDll"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ABBYY IS NOT INSTALLED")]
        public string MissingAbbyyPathInstallationMessage {
            get {
                return ((string)(this["MissingAbbyyPathInstallationMessage"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.DocumentConverter.Properties.DocumentConverterSettings.SftpLicenseK" +
            "ey@@")]
        public string SftpLicenseKey {
            get {
                return ((string)(this["SftpLicenseKey"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("TextExtraction_Accuracy")]
        public string OCRTextExtractionProfile {
            get {
                return ((string)(this["OCRTextExtractionProfile"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("TextExtraction_Speed")]
        public string PDFTextLayerExtractionProfile {
            get {
                return ((string)(this["PDFTextLayerExtractionProfile"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.DocumentConverter.Properties.DocumentConverterSettings.SftpPrivateC" +
            "ertKey@@")]
        public string SftpPrivateCertKey {
            get {
                return ((string)(this["SftpPrivateCertKey"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.DocumentConverter.Properties.DocumentConverterSettings.SftpPrivateC" +
            "ertPassword@@")]
        public string SftpPrivateCertPassword {
            get {
                return ((string)(this["SftpPrivateCertPassword"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4")]
        public int AbbyyEnginePoolSize {
            get {
                return ((int)(this["AbbyyEnginePoolSize"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("@@CMI.Manager.DocumentConverter.Properties.DocumentConverterSettings.AbbyySerialN" +
            "umber@@")]
        public string AbbyySerialNumber {
            get {
                return ((string)(this["AbbyySerialNumber"]));
            }
        }
    }
}
