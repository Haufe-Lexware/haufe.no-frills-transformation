using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;

namespace NoFrillsTransformation.Plugins.Salesforce
{
    public class SfdcBaseFactory
    {
        protected static SfdcConfig ParseConfig(IContext context, string configFile)
        {
            try
            {
                string resolvedPath = context.ResolveFileName(configFile);
                XmlSerializer serializer = new XmlSerializer(typeof(SfdcConfig));
                using (var fs = new FileStream(resolvedPath, FileMode.Open))
                {
                    var config = (SfdcConfig)serializer.Deserialize(fs);
                    config.DataLoaderDir = context.ReplaceParameters(config.DataLoaderDir);
                    config.LogFileDir = context.ResolveFileName(context.ReplaceParameters(config.LogFileDir), false);
                    config.SuccessFileName = context.ResolveFileName(context.ReplaceParameters(config.SuccessFileName), false);
                    config.ErrorFileName = context.ResolveFileName(context.ReplaceParameters(config.ErrorFileName), false);
                    config.SfdcEncryptedPassword = context.ReplaceParameters(config.SfdcEncryptedPassword);
                    config.SfdcEndPoint = context.ReplaceParameters(config.SfdcEndPoint);
                    config.SfdcUsername = context.ReplaceParameters(config.SfdcUsername);

                    if (context.Parameters.ContainsKey("sfdckeeptempfiles"))
                    {
                        config.KeepTempFiles = SfdcBool(context.Parameters["sfdckeeptempfiles"]);
                        context.Logger.Info("SFDC Settings: Override KeepTempFiles=" + config.KeepTempFiles);
                    }
                    if (context.Parameters.ContainsKey("sfdcfailonerrors"))
                    {
                        config.FailOnErrors = SfdcBool(context.Parameters["sfdcfailonerrors"]);
                        context.Logger.Info("SFDC Settings: Override FailOnErrors=" + config.KeepTempFiles);
                    }

                    return config;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not read SFDC configuration file: " + configFile + ", error message: " + ex.Message);
            }
        }

        private static  bool SfdcBool(string boolString)
        {
            switch (boolString.ToLowerInvariant())
            {
                case "yes":
                case "on":
                case "1":
                case "true":
                    return true;
            }
            return false;
        }
    }
}
