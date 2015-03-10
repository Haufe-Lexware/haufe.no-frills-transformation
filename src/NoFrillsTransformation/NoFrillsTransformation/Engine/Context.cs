using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Config;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class ContextFactory
    {
        public static Context CreateContext(ConfigFileXml config, NftConfigXml nftConfig)
        {
            return new Context();
        }
    }
    class Context
    {
        public ISourceReader SourceReader { get; set; }
        public IList<LookupMap> LookupMaps { get; set; }
        public LookupMap GetLookupMap(string id)
        {
            return null;
        }

        public IList<Mapping> Mappings { get; set; }

        public IRecord CurrentRecord { get; set; }
    }
}
