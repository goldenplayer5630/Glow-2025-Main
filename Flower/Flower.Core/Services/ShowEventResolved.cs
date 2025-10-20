using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Services
{
    public sealed record ShowEventResolved(long AtMs, IReadOnlyList<byte[]> Frames);
}
