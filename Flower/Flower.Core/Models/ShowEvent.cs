using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models;

public sealed record ShowEvent(int flowerId, string cmdId, Dictionary<string, object?> Args);

