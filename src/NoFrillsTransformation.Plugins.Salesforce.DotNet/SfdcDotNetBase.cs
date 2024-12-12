using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;
using NoFrillsTransformation.Plugins.Salesforce.DotNet.Salesforce62; // Adjust the namespace as per the generated classes
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace NoFrillsTransformation.Plugins.Salesforce.DotNet
{
    public class SfdcDotNetBase : IDisposable
    {
        protected const string SFDC_VERSION = "62.0";

        internal SfdcDotNetBase(IContext context, SfdcConfig config)
        {
            _config = config;
            _context = context;
        }

        protected SfdcConfig _config;
        protected IContext _context;
        protected SoapClient _sfdc;

        protected void Login()
        {
            if (string.IsNullOrEmpty(_config.SfdcPassword))
                throw new ArgumentException("SfdcDotNet: In configuration file, the tag <SfdcPassword> must be provided.");
            if (string.IsNullOrEmpty(_config.SfdcUsername))
                throw new ArgumentException("SfdcDotNet: In configuration file, the tag <SfdcUsername> must be provided.");

            var binding = new BasicHttpBinding
            {
                Security = new BasicHttpSecurity
                {
                    Mode = BasicHttpSecurityMode.Transport,
                    Transport = new HttpTransportSecurity
                    {
                        ClientCredentialType = HttpClientCredentialType.None
                    }
                }
            };

            var endpoint = new EndpointAddress("https://login.salesforce.com/services/Soap/u/" + SFDC_VERSION);
            _sfdc = new SoapClient(binding, endpoint);

            if (!string.IsNullOrEmpty(_config.SfdcEndPoint))
            {
                var endPoint = _config.SfdcEndPoint;
                if (!endPoint.Contains("/Soap/"))
                {
                    if (!endPoint.EndsWith("/"))
                        endPoint += "/";
                    endPoint += "services/Soap/u/" + SFDC_VERSION;
                }
                _sfdc.Endpoint.Address = new EndpointAddress(endPoint);
            }

            _context.Logger.Info("SfdcDotNetBase: Salesforce login URL " + _sfdc.Endpoint.Address);

            loginResponse loginResult = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                // var loginRequest = new loginRequest(null, null, _config.SfdcUsername, _config.SfdcPassword);
                // loginResult = _sfdc.loginAsync(loginRequest).Result;
                loginResult = _sfdc.loginAsync(null, null, _config.SfdcUsername, _config.SfdcPassword).GetAwaiter().GetResult();

                _context.Logger.Info("SfdcDotNetBase: Successfully logged in as '" + _config.SfdcUsername + "'.");
            }
            catch (Exception)
            {
                _sfdc.Close();
                _sfdc = null;
                throw;
            }

            _sfdc.Endpoint.Address = new EndpointAddress(loginResult.result.serverUrl);

            var sessionHeader = new SessionHeader
            {
                sessionId = loginResult.result.sessionId
            };
            _sfdc.Endpoint.EndpointBehaviors.Add(new SessionHeaderBehavior(sessionHeader));
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
                if (_sfdc != null)
                {
                    try
                    {
                        //_sfdc.logoutAsync(new logoutRequest()).Wait();
                        //_context.Logger.Info("SfdcDotNetBase: Successfully logged out.");
                    }
                    catch (Exception ex)
                    {
                        _context.Logger.Warning("SfdcDotNetBase: logout() was not successful: " + ex.Message);
                    }

                    _sfdc.Close();
                    _sfdc = null;
                }
            }
        }
    }

    public class SessionHeaderBehavior : IEndpointBehavior
    {
        private readonly SessionHeader _sessionHeader;

        public SessionHeaderBehavior(SessionHeader sessionHeader)
        {
            _sessionHeader = sessionHeader;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new SessionHeaderMessageInspector(_sessionHeader));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

        public void Validate(ServiceEndpoint endpoint) { }
    }

    public class SessionHeaderMessageInspector : IClientMessageInspector
    {
        private readonly SessionHeader _sessionHeader;

        public SessionHeaderMessageInspector(SessionHeader sessionHeader)
        {
            _sessionHeader = sessionHeader;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState) { }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            var httpRequestMessageProperty = new HttpRequestMessageProperty();
            httpRequestMessageProperty.Headers["Authorization"] = "Bearer " + _sessionHeader.sessionId;
            request.Properties[HttpRequestMessageProperty.Name] = httpRequestMessageProperty;
            return null;
        }
    }
}
