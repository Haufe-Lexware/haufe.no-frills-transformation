using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;

namespace NoFrillsTransformation.Plugins.Salesforce.DotNet
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class SfdcDotNetReaderFactory : SfdcBaseFactory, ISourceReaderFactory
    {
        public bool CanReadSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;
            string temp = source.ToLowerInvariant();
            if (!temp.StartsWith("soql.net://"))
                return false;
            return true;
        }

        public ISourceReader CreateReader(IContext context, string source, string config)
        {
            string soql = source.Substring(11);
            var soqlQuery = ParseQuery(soql);
            var sfdcConfig = ParseConfig(context, config);
            SfdcDotNetReader sfdcReader = null;
            try
            {
                context.Logger.Info("SfdcDotNetReaderFactory: Attempting to create an SfdcReader.");

                sfdcReader = new SfdcDotNetReader(context, soqlQuery, sfdcConfig);
                sfdcReader.Initialize();
            }
            catch (Exception ex)
            {
                if (null != sfdcReader)
                {
                    sfdcReader.Dispose();
                    sfdcReader = null;
                }
                throw new InvalidOperationException("An error occurred while creating the SfdcReader: " + ex.Message);
            }

            context.Logger.Info("SfdcReaderFactory: Successfully created a SfdcReader.");

            return sfdcReader;
        }

        // 
        public bool SupportsQuery
        {
            get
            {
                return false;
            }
        }
    }
}
