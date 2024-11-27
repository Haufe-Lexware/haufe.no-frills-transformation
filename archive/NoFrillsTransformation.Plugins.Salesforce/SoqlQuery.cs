using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Plugins.Salesforce
{
    public class SoqlQuery
    {
        public string Soql { get; set; }
        public string Entity { get; set; }
        public string[] FieldNames { get; set; }
    }
}
