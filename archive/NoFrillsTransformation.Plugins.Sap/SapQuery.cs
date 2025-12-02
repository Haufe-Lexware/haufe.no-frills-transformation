using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Plugins.Sap
{
    class SapQuery
    {
        public string RfcName { get; set; }
        public SapQueryParameter[] Parameters { get; set; }
    }

    class SapQueryParameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
