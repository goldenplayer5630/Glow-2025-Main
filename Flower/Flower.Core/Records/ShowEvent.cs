using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Records;

public sealed record ShowEvent(int Priority, string CmdId, Dictionary<string, object?> Args);


