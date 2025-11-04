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
        public string Name { get; set; } = "";
        public FlowerCategory Category { get; set; }

        public string BusId { get; set; } = "bus0";

        public ConnectionStatus ConnectionStatus { get; set; }
        public FlowerStatus FlowerStatus { get; set; }
        public int CurrentBrightness { get; set; }
    }
}
