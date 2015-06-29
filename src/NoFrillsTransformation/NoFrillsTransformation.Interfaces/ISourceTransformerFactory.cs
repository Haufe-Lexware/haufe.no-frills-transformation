using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface ISourceTransformerFactory
    {
        bool CanPerformTransformation(string transform);

        ISourceTransformer CreateTransformer(IContext context, string source, string config, IParameter[] parameters, ISetting[] settings);
    }
}
