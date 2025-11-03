using Flower.Core.Abstractions.Services;
using Flower.Core.Abstractions.Stores;
using Flower.Core.Enums;
using Flower.Core.Models;
using System.Collections.ObjectModel;

namespace Flower.Infrastructure.Persistence
{
    public class FlowerService : IFlowerService
    {
        private readonly IFlowerStore _store;
        private readonly ObservableCollection<FlowerUnit> _flowers = new();
        private readonly SemaphoreSlim _gate = new(1, 1);

        public ReadOnlyObservableCollection<FlowerUnit> Flowers { get; }

        public FlowerService(IFlowerStore store)
        {
            _store = store;
            Flowers = new ReadOnlyObservableCollection<FlowerUnit>(_flowers);
        }

        public async Task<IReadOnlyList<FlowerUnit>> GetAllAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try { return _flowers.ToList(); }
            finally { _gate.Release(); }
        }

        public async Task LoadAsync(string? fileNameOrPath = "flowers.json")
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var loaded = await _store.LoadAsync(fileNameOrPath).ConfigureAwait(false);

                _flowers.Clear();
                foreach (var l in loaded)
                {
                    // reset runtime-only fields
                    l.ConnectionStatus = ConnectionStatus.Disconnected;
                    l.FlowerStatus = FlowerStatus.Closed;
                    l.CurrentBrightness = 0;
                    _flowers.Add(l);
                }
            }
            finally { _gate.Release(); }
        }

        public async Task SaveAsync(string? fileNameOrPath = null)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try { await _store.SaveAsync(_flowers.ToList(), fileNameOrPath).ConfigureAwait(false); }
            finally { _gate.Release(); }
        }

        public async Task<FlowerUnit?> GetAsync(int id)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try { return _flowers.FirstOrDefault(f => f.Id == id); }
            finally { _gate.Release(); }
        }

        public async Task AddAsync(FlowerUnit flower)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var id = flower.Id;
                if (id <= 0) throw new ArgumentException("Flower.Id must be > 0", nameof(flower));
                if (_flowers.Any(f => f.Id == id))
                    throw new InvalidOperationException($"Flower with Id {id} already exists.");

                _flowers.Add(flower);
            }
            finally { _gate.Release(); }
        }

        public async Task<bool> UpdateAsync(FlowerUnit flower)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var id = flower.Id;
                for (int i = 0; i < _flowers.Count; i++)
                {
                    if (_flowers[i].Id == id)
                    {
                        _flowers[i] = flower; // replace
                        return true;
                    }
                }
                return false;
            }
            finally { _gate.Release(); }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                for (int i = 0; i < _flowers.Count; i++)
                {
                    if (_flowers[i].Id == id)
                    {
                        _flowers.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }
            finally { _gate.Release(); }
        }
    }
}
