using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Sap.Config;

namespace NoFrillsTransformation.Plugins.Sap
{
    [Export(typeof(NoFrillsTransformation.Interfaces.ISourceReaderFactory))]
    public class SapReaderFactory : ISourceReaderFactory
    {
        public bool CanReadSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;
            string temp = source.ToLowerInvariant();
            if (!temp.StartsWith("sap://"))
                return false;
            //if (!temp.EndsWith(".csv") && !temp.EndsWith(".txt"))
            //    return false;
            return true;
        }

        public ISourceReader CreateReader(IContext context, string source, string config)
        {
            var sapQuery = ParseQuery(context, context.ReplaceParameters(source.Substring(6)));
            //var soqlQuery = ParseQuery(soql);
            //var sfdcConfig = ParseConfig(context, config);
            //SfdcReader sfdcReader = null;
            //try
            //{
            //    context.Logger.Info("SfdcReaderFactory: Attempting to create an SfdcReader.");

            //    sfdcReader = new SfdcReader(context, soqlQuery, sfdcConfig);
            //    sfdcReader.Initialize();
            //}
            //catch (Exception ex)
            //{
            //    if (null != sfdcReader)
            //    {
            //        sfdcReader.Dispose();
            //        sfdcReader = null;
            //    }
            //    throw new InvalidOperationException("An error occurred while creating the SfdcReader: " + ex.Message);
            //}

            //context.Logger.Info("SfdcReaderFactory: Successfully created a SfdcReader.");

            var sapConfig = ParseConfig(context, config);

            return new SapReader(context, sapQuery, sapConfig);
        }

        private static SapConfig ParseConfig(IContext context, string configFile)
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

        private static int FindDelimiterPosition(string expression, int pos, char delimiter)
        {
            int delimPos = pos;
            bool inQuote = false;
            int parCount = 0;
            // This would be the last to read; in case we have parenthesis inside other ones,
            // this is important (nested expressions)
            if (delimiter == ')')
                parCount = 1;

            char c = expression[delimPos];
            int len = expression.Length;
            bool skipNext = false;
            while (c != delimiter || inQuote || parCount > 0)
            {
                if (delimPos >= len)
                    return -1; // Not found
                if (!skipNext)
                {
                    c = expression[delimPos];
                    if (c == '\\') // escape char
                        skipNext = true;
                    else if (c == '(' && !inQuote)
                        parCount++;
                    else if (c == ')' && !inQuote)
                        parCount--;
                    else if (c == '"' && (delimiter != '"'))
                    {
                        if (inQuote)
                            inQuote = false;
                        else
                            inQuote = true;
                    }
                }
                else
                {
                    skipNext = false;
                }
                delimPos++;
            }
            delimPos--;
            return delimPos;
        }

        private static string Unescape(string s)
        {
            StringBuilder sb = new StringBuilder();
            bool takeNext = false;
            foreach (char c in s)
            {
                if (!takeNext && c == '\\')
                {
                    takeNext = true;
                    continue;
                }
                takeNext = false;
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static SapQuery ParseQuery(IContext context, string query)
        {
            // Format: RFC:RFC_NAME(PARAM1="...", PARAM2="...")
            if (!query.StartsWith("rfc:", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("SapReaderFactory: Currently, only RFC calls are supported (starting with RFC:<rfc name>).");
            query = query.Substring(4); // Strip RFC:

            int parenIndex = query.IndexOf('(');
            if (parenIndex < 0)
                throw new ArgumentException("SapReaderFactory: Invalid format of query string, could not find opening parenthesis: " + query);
            int endParenIndex = FindDelimiterPosition(query, parenIndex + 1, ')');
            if (parenIndex < 0)
                throw new ArgumentException("SapReaderFactory: Invalid format of query string, could not find closing parenthesis: " + query);

            var fubaName = query.Substring(0, parenIndex);

            var paramList = new List<SapQueryParameter>();
            var parameters = query.Substring(parenIndex + 1, endParenIndex - parenIndex - 1);

            do
            {
                int eqIndex = parameters.IndexOf('=');
                if (eqIndex < 0)
                    break;

                var paramName = parameters.Substring(0, eqIndex).Trim();

                int quoteIndex = parameters.IndexOf('"', eqIndex);
                if (quoteIndex < 0)
                    throw new ArgumentException("Could not find starting quote \" for parameter value of parameter: " + paramName);

                int endQuoteIndex = FindDelimiterPosition(parameters, quoteIndex + 1, '"');
                if (endQuoteIndex < 0)
                    throw new ArgumentException("Could not find ending quote \" for parameter value of parameter: " + paramName);

                var paramValue = Unescape(parameters.Substring(quoteIndex + 1, endQuoteIndex - quoteIndex - 1));

                paramList.Add(new SapQueryParameter { Name = paramName, Value = paramValue });

                int commaIndex = parameters.IndexOf(',', endQuoteIndex);
                if (commaIndex < 0)
                    break;

                // Bite off processed parameter.
                parameters = parameters.Substring(commaIndex + 1);
            }
            while (true);

            return new SapQuery
            {
                RfcName = fubaName,
                Parameters = paramList.ToArray()
            };
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
