using Flower.Core.Abstractions.Services;
using Flower.Core.Enums;
using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public class AddOrUpdateFlowerViewModel : ViewModelBase, IAddOrUpdateFlowerViewModel, INotifyPropertyChanged
    {
        private readonly IFlowerService _flowerService;

        // Update context (null => Add mode)
        private int? _originalId;
        private ConnectionStatus _origConnectionStatus;
        private FlowerStatus _origFlowerStatus;
        private int _origBrightness;
        private string? _origBusId;
        private int _origPriority;

        // Editable state
        private int _idValue = 1; // 1..30 (but we allow up to 999)
        private FlowerCategory _selectedCategory = FlowerCategory.SmallTulip;

        // UX state
        private bool _isBusy;
        private bool _canConfirm;

        private bool _useManualStatusOverride;
        private FlowerStatus _selectedStatus = FlowerStatus.Closed;

        private int _priorityValue;
        private string? _priorityError;

        // Errors
        private string? _idError;
        private string? _formError;

        // For fast duplicate checks
        private HashSet<int> _existingIds = new();
        private HashSet<int> _existingPriorities = new();

        // Events
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<FlowerUnit?>? CloseRequested;

        public string StatusPreview => ConnectionStatus.Disconnected.ToString();
        public string FlowerStatusPreview => FlowerStatus.Closed.ToString();
        public string BrightnessPreview => "0";

        // ===== Bindables =====
        public IReadOnlyList<FlowerCategory> CategoryItems { get; } =
            Enum.GetValues(typeof(FlowerCategory)).Cast<FlowerCategory>().ToList();
        public IReadOnlyList<FlowerStatus> StatusItems { get; } =
            Enum.GetValues(typeof(FlowerStatus)).Cast<FlowerStatus>().ToList();

        public bool UseManualStatusOverride
        {
            get => _useManualStatusOverride;
            set
            {
                if (SetField(ref _useManualStatusOverride, value))
                    RecomputeCanConfirm();
            }
        }

        public FlowerStatus SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetField(ref _selectedStatus, value))
                    RecomputeCanConfirm();
            }
        }

        public int PriorityValue
        {
            get => _priorityValue;
            set
            {
                if (SetField(ref _priorityValue, value))
                {
                    ValidatePriority();
                    RecomputeCanConfirm();
                }
            }
        }

        public string? PriorityError
        {
            get => _priorityError;
            private set
            {
                if (SetField(ref _priorityError, value))
                    OnPropertyChanged(nameof(HasPriorityError));
            }
        }
        public bool HasPriorityError => !string.IsNullOrWhiteSpace(PriorityError);

        public AddOrUpdateFlowerViewModel(IFlowerService flowerService)
        {
            _flowerService = flowerService ?? throw new ArgumentNullException(nameof(flowerService));

            CategoryItems = Enum.GetValues(typeof(FlowerCategory))
                                .Cast<FlowerCategory>()
                                .ToList();

            StatusItems = Enum.GetValues(typeof(FlowerStatus))
                               .Cast<FlowerStatus>()
                               .ToList();

            RecomputeCanConfirm();
        }

        // ===== Lifecycle =====
        /// <summary>
        /// Initializes the VM for Add (existing==null) or Update (existing!=null).
        /// - Preloads existing Ids and Priorities.
        /// - Suggests next free Id/Priority for Add.
        /// - Prefills & preserves runtime fields for Update, excluding current Id/Priority from duplicate checks.
        /// </summary>
        public async Task InitAsync(FlowerUnit? existing = null)
        {
            var list = await _flowerService.GetAllAsync().ConfigureAwait(false);

            _existingIds = list.Select(f => f.Id).ToHashSet();
            _existingPriorities = list
                .Select(f => f.Priority)
                .Where(p => p >= 1 && p <= 999) // ignore zeros or out-of-policy values
                .ToHashSet();

            if (existing is null)
            {
                _originalId = null;
                _origConnectionStatus = ConnectionStatus.Disconnected;
                _origFlowerStatus = FlowerStatus.Closed;
                _origBrightness = 0;
                _origBusId = null;
                _origPriority = 0;

                IdValue = SuggestNextFreeId(list);
                PriorityValue = SuggestNextFreePriority(list);
            }
            else
            {
                _originalId = existing.Id;

                // Allow keeping current Id/Priority by excluding them from duplicates
                _existingIds.Remove(existing.Id);
                _existingPriorities.Remove(existing.Priority);

                IdValue = existing.Id;
                SelectedCategory = existing.Category;

                _origConnectionStatus = existing.ConnectionStatus;
                _origFlowerStatus = existing.FlowerStatus;
                _origBrightness = existing.CurrentBrightness;
                _origBusId = existing.BusId;
                _origPriority = existing.Priority;

                PriorityValue = existing.Priority;
            }

            // refresh buttons after init
            RecomputeCanConfirm();
        }

        private static int SuggestNextFreeId(IReadOnlyList<FlowerUnit> items)
        {
            var used = items.Select(f => f.Id).Where(id => id >= 1 && id <= 999).ToHashSet();
            for (int candidate = 1; candidate <= 999; candidate++)
            {
                if (!used.Contains(candidate)) return candidate;
            }
            return 999;
        }

        private static int SuggestNextFreePriority(IReadOnlyList<FlowerUnit> items)
        {
            var used = items
                .Select(f => f.Priority)
                .Where(p => p >= 1 && p <= 999)
                .ToHashSet();

            for (int candidate = 1; candidate <= 999; candidate++)
            {
                if (!used.Contains(candidate)) return candidate;
            }
            // Fallback: mirror Id logic
            return 999;
        }

        public int IdValue
        {
            get => _idValue;
            set
            {
                if (SetField(ref _idValue, value))
                {
                    ValidateId();
                    RecomputeCanConfirm();
                }
            }
        }

        public FlowerCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetField(ref _selectedCategory, value))
                {
                    ValidateCategory();
                    RecomputeCanConfirm();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetField(ref _isBusy, value))
                    RecomputeCanConfirm();
            }
        }

        public bool CanConfirm
        {
            get => _canConfirm;
            private set => SetField(ref _canConfirm, value);
        }

        public string? IdError
        {
            get => _idError;
            private set
            {
                if (SetField(ref _idError, value))
                    OnPropertyChanged(nameof(HasIdError));
            }
        }
        public bool HasIdError => !string.IsNullOrWhiteSpace(IdError);

        public string? FormError
        {
            get => _formError;
            private set
            {
                if (SetField(ref _formError, value))
                    OnPropertyChanged(nameof(HasFormError));
            }
        }
        public bool HasFormError => !string.IsNullOrWhiteSpace(FormError);

        // ===== Actions =====
        public async Task ConfirmAsync()
        {
            FormError = null;

            if (!ValidateAll())
            {
                FormError = "Please fix validation errors.";
                return;
            }

            var newId = IdValue;
            var newPriority = PriorityValue;

            IsBusy = true;
            try
            {
                // Server-side double check
                var all = await _flowerService.GetAllAsync().ConfigureAwait(false);

                var dupId = all.Any(f => f.Id == newId && f.Id != _originalId);
                if (dupId)
                {
                    IdError = $"A flower with Id {newId} already exists.";
                    FormError = "Please choose a unique Id.";
                    return;
                }

                var dupPriority = all.Any(f => f.Priority == newPriority && f.Id != _originalId);
                if (dupPriority)
                {
                    PriorityError = $"Priority {newPriority} is already used by another flower.";
                    FormError = "Please choose a unique Priority.";
                    return;
                }

                // Decide which status to write
                var finalStatus = UseManualStatusOverride ? SelectedStatus
                                                          : (_originalId is null ? FlowerStatus.Closed : _origFlowerStatus);

                var unit = new FlowerUnit
                {
                    Id = newId,
                    Category = _selectedCategory,
                    ConnectionStatus = _originalId is null ? ConnectionStatus.Disconnected : _origConnectionStatus,
                    FlowerStatus = finalStatus,
                    CurrentBrightness = _originalId is null ? 0 : _origBrightness,
                    BusId = _originalId is null ? null : _origBusId,
                    Priority = newPriority
                };

                CloseRequested?.Invoke(this, unit);
            }
            catch (Exception ex)
            {
                FormError = $"Failed to confirm: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Cancel() => CloseRequested?.Invoke(this, null);

        // ===== Validation =====
        private bool ValidateAll()
        {
            var ok = true;
            ok &= ValidateId();
            ok &= ValidateCategory();
            ok &= ValidatePriority();
            return ok;
        }

        private bool ValidatePriority()
        {
            // Priority behaves like a secondary ID: range 1..999 and unique
            if (PriorityValue < 1 || PriorityValue > 999)
            {
                PriorityError = "Priority must be between 1 and 999.";
                return false;
            }

            if (_existingPriorities.Contains(PriorityValue))
            {
                PriorityError = $"Priority {PriorityValue} is already in use.";
                return false;
            }

            PriorityError = null;
            return true;
        }

        private bool ValidateId()
        {
            if (IdValue < 1 || IdValue > 999)
            {
                IdError = "Id must be between 1 and 999.";
                return false;
            }

            // _existingIds already excludes the original Id in Update mode
            if (_existingIds.Contains(IdValue))
            {
                IdError = $"A flower with Id {IdValue} already exists.";
                return false;
            }

            IdError = null;
            return true;
        }

        private bool ValidateCategory()
        {
            if (SelectedCategory == FlowerCategory.Unknown)
            {
                FormError = "Please pick a category.";
                return false;
            }

            if (FormError != null && FormError.StartsWith("Please pick a category", StringComparison.Ordinal))
                FormError = null;

            return true;
        }

        private void RecomputeCanConfirm()
        {
            CanConfirm = !IsBusy
                      && !HasIdError
                      && !HasPriorityError
                      && SelectedCategory != FlowerCategory.Unknown
                      && string.IsNullOrWhiteSpace(FormError);
        }

        // ----- helpers -----
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
