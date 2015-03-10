using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Config;

namespace NoFrillsTransformation.Engine
{
    class LookupMapFactory
    {
        public static LookupMap CreateLookupMap(LookupMapXml config, NftConfigXml nft)
        {
            return new LookupMap();
        }
    }

    class LookupMap
    {
        public string Id { get; set; }
        public string KeyField { get; set; }
        public string GetValue(string key, string fieldName)
        {
            return "dumm";
        }
        public string GetValue(string key, int fieldIndex)
        {
            return "dumber";
        }
        public int GetFieldIndex(string fieldName)
        {
            return 1;
        }
    }
}
