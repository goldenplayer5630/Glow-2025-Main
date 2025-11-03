using Flower.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models
{
    public class FlowerUnit
    {
        public int Id { get; set; }
        public FlowerCategory Category { get; set; } = FlowerCategory.SmallTulip;
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Disconnected;
        public FlowerStatus FlowerStatus { get; set; } = FlowerStatus.Closed;
        public int CurrentBrightness { get; set; } = 0;
    }
}
