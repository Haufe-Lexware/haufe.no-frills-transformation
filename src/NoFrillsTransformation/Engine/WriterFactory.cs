using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class WriterFactory
    {
        public WriterFactory(CompositionHost container)
        {
            container.SatisfyImports(this);
        }


        [ImportMany]
        private ITargetWriterFactory[]? WriterFactories { get; set; }

        public ITargetWriter CreateWriter(IContext context, string target, TargetFieldDef[] fieldDefs, string? config)
        {
            if (null == WriterFactories)
                throw new InvalidOperationException("No writer factories found.");
            foreach (var wf in WriterFactories)
            {
                if (wf.CanWriteTarget(target))
                    return wf.CreateWriter(context, target, fieldDefs, config);
            }
            throw new InvalidOperationException("Could not find a suitable writer for target '" + target + "'.");
        }
    }
}
