using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class TransformerFactory
    {
        public TransformerFactory(CompositionHost container)
        {
            container.SatisfyImports(this);
        }

        [ImportMany]
        private ISourceTransformerFactory[]? TransformerFactories { get; set; }

        public ISourceTransformer CreateTransformer(IContext context, string source, string? config, IParameter[] parameters, ISetting[] settings)
        {
            if (null == TransformerFactories)
                throw new InvalidOperationException("No transformer factories found.");
            foreach (var rf in TransformerFactories)
            {
                if (rf.CanPerformTransformation(source))
                    return rf.CreateTransformer(context, source, config, parameters, settings);
            }
            throw new InvalidOperationException("Could not find a suitable transfomer for source '" + source + "'.");
        }
    }
}
