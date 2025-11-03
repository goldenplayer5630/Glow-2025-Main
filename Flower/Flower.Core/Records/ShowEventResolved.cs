using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Records
{
    public sealed record ShowEventResolved(long AtMs, IReadOnlyList<byte[]> Frames);
}
