using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Plugins.Sap.Config;
using SAP.Middleware.Connector;

namespace NoFrillsTransformation.Plugins.Sap
{
    internal class BackendConfig : IDestinationConfiguration
    {
        public BackendConfig(SapConfig config)
        {
            _config = config;
        }

        private SapConfig _config;

        public RfcConfigParameters GetParameters(String destinationName)
        {
            if (_config.RfcDestination.Equals(destinationName))
            {
                RfcConfigParameters parms = new RfcConfigParameters();
                parms.Add(RfcConfigParameters.AppServerHost, _config.AppServerHost);
                parms.Add(RfcConfigParameters.SystemNumber, _config.SystemNumber);
                parms.Add(RfcConfigParameters.Client, _config.Client);
                parms.Add(RfcConfigParameters.Language, _config.Language);

                parms.Add(RfcConfigParameters.User, _config.User);
                parms.Add(RfcConfigParameters.Password, _config.Password);

                //parms.Add(RfcConfigParameters.PoolSize, "5");
                //parms.Add(RfcConfigParameters.MaxPoolSize, "10");
                //parms.Add(RfcConfigParameters.IdleTimeout, "600");
                return parms;
            }
            else return null;
        }
        // The following two are not used in this example:
        public bool ChangeEventsSupported()
        {
            return false;
        }
#pragma warning disable 0067
        public event RfcDestinationManager.ConfigurationChangeHandler ConfigurationChanged;
#pragma warning restore 0067
    }
}
