using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StyleMerge
{
    /// <summary>
    /// Indicates an issue with the HTML where we cannot parse it, or we are unable to handle a rule defined by the CSS to be inlined.
    /// </summary>
    public class InliningException : Exception
    {
        public InliningException(string message)
            : base(message)
        {

        }
    }
}
