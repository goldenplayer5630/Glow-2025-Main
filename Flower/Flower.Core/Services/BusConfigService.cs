using Flower.Core.Abstractions.Services;
using Flower.Core.Abstractions.Stores;
using Flower.Core.Enums;
using Flower.Core.Models;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bus.Core.Services
{
    public class BusConfigService : IBusConfigService
    {
        private readonly IBusConfigStore _store;
        private readonly ObservableCollection<BusConfig> _buses = new();
        private readonly SemaphoreSlim _gate = new(1, 1);

        public ReadOnlyObservableCollection<BusConfig> Buses { get; }

        public BusConfigService(IBusConfigStore store)
        {
            _store = store;
            Buses = new ReadOnlyObservableCollection<BusConfig>(_buses);
        }

        public async Task<IReadOnlyList<BusConfig>> GetAllAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try { return _buses.ToList(); }
            finally { _gate.Release(); }
        }

        public async Task LoadAsync(string? fileNameOrPath = "buses.json")
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var loaded = await _store.LoadAsync(fileNameOrPath).ConfigureAwait(false);

                _buses.Clear();
                foreach (var l in loaded)
                {
                    _buses.Add(l);
                }
            }
            finally { _gate.Release(); }
        }

        public async Task SaveAsync(string? fileNameOrPath = null)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try {
                await _store.SaveAsync(_buses.ToList(), fileNameOrPath).ConfigureAwait(false); 
            }
            finally { _gate.Release(); }
        }

        public async Task<BusConfig?> GetAsync(string id)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try { return _buses.FirstOrDefault(f => f.BusId == id); }
            finally { _gate.Release(); }
        }

        public async Task AddAsync(BusConfig Bus)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var id = Bus.BusId;
                if (_buses.Any(f => f.BusId == id))
                    throw new InvalidOperationException($"Bus with Id {id} already exists.");

                _buses.Add(Bus);
            }
            finally { _gate.Release(); }
        }

        public async Task<bool> UpdateAsync(BusConfig Bus)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var id = Bus.BusId;
                for (int i = 0; i < _buses.Count; i++)
                {
                    if (_buses[i].BusId == id)
                    {
                        _buses[i] = Bus; // replace
                        return true;
                    }
                }
                return false;
            }
            finally { _gate.Release(); }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                for (int i = 0; i < _buses.Count; i++)
                {
                    if (_buses[i].BusId == id)
                    {
                        _buses.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }
            finally { _gate.Release(); }
        }
    }
}
