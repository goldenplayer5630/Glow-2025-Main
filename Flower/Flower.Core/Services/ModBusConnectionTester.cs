using Flower.Core.Abstractions.Services;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Services
{
    public sealed class ModBusConnectionTester : IModBusConnectionTester
    {
        public Task<(bool ok, string message)> TestAsync(BusConfig bus, CancellationToken ct = default)
        {
            // TODO: implement real connect/read using EasyModbus
            if (string.IsNullOrWhiteSpace(bus.ModbusHost))
                return Task.FromResult((false, "Host is empty."));
            return Task.FromResult((true, $"OK (stub) {bus.ModbusHost}:{bus.ModbusPort} Unit {bus.ModbusUnitId}"));
        }
    }
}
