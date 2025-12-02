using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Plugins.Salesforce
{
    public class SfdcTarget
    {
        public string Entity { get; set; }
        public string Operation { get; set; }
        public string ExternalId { get; set; }
    }
}
