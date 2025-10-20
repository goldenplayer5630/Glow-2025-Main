using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models
{
    public sealed record TrackEvent(long AtMs, ShowEvent Event);
}
