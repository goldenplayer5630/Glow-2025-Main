using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Models
{
    [JsonObject(MemberSerialization.OptOut)]
    public sealed class ArgEntry : ReactiveObject
    {
        public string Name { get; }
        private double _value;
        public double Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }
        public ArgEntry(string name, double value) { Name = name; _value = value; }
    }

}
