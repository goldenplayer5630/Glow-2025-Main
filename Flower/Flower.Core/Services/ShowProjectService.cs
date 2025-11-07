using Flower.Core.Abstractions.Services;
using Flower.Core.Abstractions.Stores;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Services
{
    public sealed class ShowProjectService : IShowProjectService, IDisposable
    {
        private readonly IShowProjectStore _store;
        private readonly ObservableCollection<ShowProject> _projects = new();
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _disposed;

        /// <summary>Live, bindable list of projects.</summary>
        public ReadOnlyObservableCollection<ShowProject> Projects { get; }

        public ShowProjectService(IShowProjectStore store)
        {
            _store = store;
            Projects = new ReadOnlyObservableCollection<ShowProject>(_projects);
        }

        public async Task<IReadOnlyList<ShowProject>> GetAllAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try { return _projects.ToList(); }
            finally { _gate.Release(); }
        }

        /// <summary>Loads projects from disk (default file: "shows.json").</summary>
        public async Task LoadAsync(string? fileNameOrPath = "shows.json")
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var loaded = await _store.LoadAsync(fileNameOrPath).ConfigureAwait(false);

                _projects.Clear();
                if (loaded is IEnumerable<ShowProject> collection)
                {
                    foreach (var p in collection)
                        _projects.Add(p);
                }
                else if (loaded is ShowProject single)
                {
                    _projects.Add(single);
                }
            }
            finally { _gate.Release(); }
        }

        /// <summary>Saves projects to disk (if null, store decides target file).</summary>
        public async Task SaveAsync(string? fileNameOrPath = null)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                // Fix: Save each project individually since IJsonStore<ShowProject> expects a single ShowProject, not a list.
                foreach (var project in _projects)
                {
                    await _store.SaveAsync(project, fileNameOrPath).ConfigureAwait(false);
                }
            }
            finally { _gate.Release(); }
        }

        /// <summary>Gets a project by ID.</summary>
        public async Task<ShowProject?> GetAsync(string id)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                // If your key is named differently, update here:
                return _projects.FirstOrDefault(p => p.ProjectId == id);
            }
            finally { _gate.Release(); }
        }

        /// <summary>Adds a new project (throws if ID already exists).</summary>
        public async Task AddAsync(ShowProject project)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var id = project.ProjectId; // adjust if different
                if (_projects.Any(p => p.ProjectId == id))
                    throw new InvalidOperationException($"ShowProject with Id '{id}' already exists.");

                _projects.Add(project);
            }
            finally { _gate.Release(); }
        }

        /// <summary>Replaces an existing project with the same ID.</summary>
        public async Task<bool> UpdateAsync(ShowProject project)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var id = project.ProjectId; // adjust if different
                for (int i = 0; i < _projects.Count; i++)
                {
                    if (_projects[i].ProjectId == id)
                    {
                        _projects[i] = project; // replace in-place
                        return true;
                    }
                }
                return false;
            }
            finally { _gate.Release(); }
        }

        /// <summary>Deletes a project by ID.</summary>
        public async Task<bool> DeleteAsync(string id)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                for (int i = 0; i < _projects.Count; i++)
                {
                    if (_projects[i].ProjectId == id)
                    {
                        _projects.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }
            finally { _gate.Release(); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gate.Dispose();
        }
    }
}
