using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;
using NoFrillsTransformation.Plugins.Salesforce.DotNet.Salesforce37;

namespace NoFrillsTransformation.Plugins.Salesforce.DotNet
{
    public class SfdcDotNetBase : IDisposable
    {
        protected const string SFDC_VERSION = "37.0";

        internal SfdcDotNetBase(IContext context, SfdcConfig config)
        {
            _config = config;
            _context = context;
        }

        protected SfdcConfig _config;
        protected IContext _context;
        protected SforceService _sfdc;

        protected void Login()
        {
            if (string.IsNullOrEmpty(_config.SfdcPassword))
                throw new ArgumentException("SfdcDotNet: In configuration file, the tag <SfdcPassword> must be provided.");
            if (string.IsNullOrEmpty(_config.SfdcUsername))
                throw new ArgumentException("SfdcDotNet: In configuration file, the tag <SfdcUsername> must be provided.");

            _sfdc = new SforceService();
            if (!string.IsNullOrEmpty(_config.SfdcEndPoint))
            {
                var endPoint = _config.SfdcEndPoint;
                if (!endPoint.Contains("/Soap/"))
                {
                    if (!endPoint.EndsWith("/"))
                        endPoint += "/";
                    endPoint += "services/Soap/u/" + SFDC_VERSION;
                }
                _sfdc.Url = endPoint;
            }

            _context.Logger.Info("SfdcDotNetBase: Salesforce login URL " + _sfdc.Url);

            LoginResult loginResult = null;
            try
            {
                loginResult = _sfdc.login(_config.SfdcUsername, _config.SfdcPassword);
                _context.Logger.Info("SfdcDotNetBase: Successfully logged in as '" + _config.SfdcUsername + "'.");
            }
            catch (Exception)
            {
                _sfdc.Dispose();
                _sfdc = null;
                throw;
            }
            _sfdc.Url = loginResult.serverUrl;

            _sfdc.SessionHeaderValue = new SessionHeader();
            _sfdc.SessionHeaderValue.sessionId = loginResult.sessionId;
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (null != _sfdc)
                {
                    try 
                    { 
                        _sfdc.logout();
                        _context.Logger.Info("SfdcDotNetBase: Successfully logged out.");
                    } 
                    catch (Exception ex) 
                    {
                        _context.Logger.Warning("SfdcDotNetBase: logout() was not successful: " + ex.Message);
                    }

                    _sfdc.Dispose();
                    _sfdc = null;
                }
            }
        }
    }
}
