using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models
{
    public class ShowProject
    {
        public int Version { get; set; } = 1;
        public string Title { get; set; } = "Untitled Show";
        public string ProjectId { get; set; } = "M";
        public List<ShowTrack> Tracks { get; set; } = new();
        public bool Repeat { get; set; } = true;
    }

}
