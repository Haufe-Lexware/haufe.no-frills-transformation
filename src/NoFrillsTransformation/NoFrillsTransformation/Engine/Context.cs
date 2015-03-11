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
        public ITargetWriter TargetWriter { get; set; }
        public IList<LookupMap> LookupMaps { get; set; }
        public LookupMap GetLookupMap(string id)
        {
            return null;
        }

        public IList<Mapping> Mappings { get; set; }

        public IRecord CurrentRecord { get; set; }
        public int SourceRecordsRead { get; set; }
        public int SourceRecordsFiltered { get; set; }
        public int SourceRecordsProcessed { get; set; }
        public int TargetRecordsWritten { get; set; }
    }
}
