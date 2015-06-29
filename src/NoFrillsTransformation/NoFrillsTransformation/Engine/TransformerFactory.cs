using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class TransformerFactory
    {
        public TransformerFactory(CompositionContainer container)
        {
            container.ComposeParts(this);
        }


#pragma warning disable 0649
        [ImportMany(typeof(NoFrillsTransformation.Interfaces.ISourceTransformerFactory))]
        private ISourceTransformerFactory[] _transformerFactories;
#pragma warning restore 0649

        public ISourceTransformer CreateTransformer(IContext context, string source, string config, IParameter[] parameters, ISetting[] settings)
        {
            foreach (var rf in _transformerFactories)
            {
                if (rf.CanPerformTransformation(source))
                    return rf.CreateTransformer(context, source, config, parameters, settings);
            }
            throw new InvalidOperationException("Could not find a suitable transfomer for source '" + source + "'.");
        }
    }
}
