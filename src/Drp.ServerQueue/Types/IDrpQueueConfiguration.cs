using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drp.Types
{
    public interface IDrpQueueConfiguration
    {
        string ConnectionString { get; }
        TraceListener TraceListener { get; }
    }
}
