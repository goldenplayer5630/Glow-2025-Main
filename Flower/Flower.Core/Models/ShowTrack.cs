using Flower.Core.Records;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models
{
    [JsonObject(MemberSerialization.OptOut)]
    public sealed class ShowTrack
    {
        public string Name { get; set; } = "Track";
        public List<TrackEvent> Events { get; set; } = new();
        public int LoopMs { get; set; } = 0; // 0 = no loop per-track
    }
}
