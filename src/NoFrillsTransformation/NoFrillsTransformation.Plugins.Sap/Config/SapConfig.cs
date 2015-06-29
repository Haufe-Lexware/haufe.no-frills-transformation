using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Sap.Config
{
    [XmlRoot("SapConfig")]
    public class SapConfig
    {
        public string AppServerHost { get; set; }
        public string SystemNumber { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Client { get; set; }
        public string Language { get; set; }

        public string RfcDestination { get; set; }

        internal static SapConfig ParseConfig(IContext context, string configFile)
        {
            try
            {
                string resolvedPath = context.ResolveFileName(configFile);
                XmlSerializer serializer = new XmlSerializer(typeof(SapConfig));
                using (var fs = new FileStream(resolvedPath, FileMode.Open))
                {
                    var config = (SapConfig)serializer.Deserialize(fs);
                    //config.DataLoaderDir = context.ReplaceParameters(config.DataLoaderDir);
                    //config.LogFileDir = context.ResolveFileName(context.ReplaceParameters(config.LogFileDir), false);
                    //config.SuccessFileName = context.ResolveFileName(context.ReplaceParameters(config.SuccessFileName), false);
                    //config.ErrorFileName = context.ResolveFileName(context.ReplaceParameters(config.ErrorFileName), false);
                    //config.SfdcEncryptedPassword = context.ReplaceParameters(config.SfdcEncryptedPassword);
                    //config.SfdcEndPoint = context.ReplaceParameters(config.SfdcEndPoint);
                    //config.SfdcUsername = context.ReplaceParameters(config.SfdcUsername);
                    config.AppServerHost = context.ReplaceParameters(config.AppServerHost);
                    config.Client = context.ReplaceParameters(config.Client);
                    config.Language = context.ReplaceParameters(config.Language);
                    config.User = context.ReplaceParameters(config.User);
                    config.Password = context.ReplaceParameters(config.Password);
                    config.SystemNumber = context.ReplaceParameters(config.SystemNumber);
                    config.RfcDestination = context.ReplaceParameters(config.RfcDestination);
                    return config;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not read SAP configuration file: " + configFile + ", error message: " + ex.Message);
            }
        }
    }
}
