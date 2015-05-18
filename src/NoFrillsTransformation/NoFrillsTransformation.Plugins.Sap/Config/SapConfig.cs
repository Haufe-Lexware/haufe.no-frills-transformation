using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

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
    }
}
