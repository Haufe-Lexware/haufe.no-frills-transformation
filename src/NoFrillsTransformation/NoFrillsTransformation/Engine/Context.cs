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
        private Dictionary<string, LookupMap> _lookupMaps = new Dictionary<string, LookupMap>();
        public Dictionary<string, LookupMap> LookupMaps
        {
            get
            {
                return _lookupMaps;
            }
        }
        public LookupMap GetLookupMap(string id)
        {
            return LookupMaps[id];
        }

        public IList<Mapping> Mappings { get; set; }

        public TargetFieldDef[] TargetFields { get; set; }

        public int SourceRecordsRead { get; set; }
        public int SourceRecordsFiltered { get; set; }
        public int SourceRecordsProcessed { get; set; }
        public int TargetRecordsWritten { get; set; }
    }
}
