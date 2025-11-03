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

        // Editable state
        private int _idValue = 1; // 1..30
        private FlowerCategory _selectedCategory = FlowerCategory.SmallTulip;

        // UX state
        private bool _isBusy;
        private bool _canConfirm;

        // Errors
        private string? _idError;
        private string? _formError;

        // For fast duplicate checks
        private HashSet<int> _existingIds = new();

        // Events
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<FlowerUnit?>? CloseRequested;

        public string StatusPreview => ConnectionStatus.Disconnected.ToString();
        public string FlowerStatusPreview => FlowerStatus.Closed.ToString();
        public string BrightnessPreview => "0";

        public AddOrUpdateFlowerViewModel(IFlowerService flowerService)
        {
            _flowerService = flowerService ?? throw new ArgumentNullException(nameof(flowerService));

            CategoryItems = Enum.GetValues(typeof(FlowerCategory))
                                .Cast<FlowerCategory>()
                                .ToList();

            RecomputeCanConfirm();
        }

        // ===== Lifecycle =====
        /// <summary>
        /// Initializes the VM for Add (existing==null) or Update (existing!=null).
        /// - Preloads existing Ids.
        /// - Suggests next free Id for Add.
        /// - Prefills & preserves runtime fields for Update.
        /// </summary>
        public async Task InitAsync(FlowerUnit? existing = null)
        {
            var list = await _flowerService.GetAllAsync().ConfigureAwait(false);
            _existingIds = list.Select(f => f.Id).ToHashSet();

            if (existing is null)
            {
                _originalId = null;
                _origConnectionStatus = ConnectionStatus.Disconnected;
                _origFlowerStatus = FlowerStatus.Closed;
                _origBrightness = 0;

                IdValue = SuggestNextFreeId(list);
            }
            else
            {
                _originalId = existing.Id;
                // Important: allow keeping the same Id by excluding it from duplicates
                _existingIds.Remove(existing.Id);

                IdValue = existing.Id;
                SelectedCategory = existing.Category;

                _origConnectionStatus = existing.ConnectionStatus;
                _origFlowerStatus = existing.FlowerStatus;
                _origBrightness = existing.CurrentBrightness;
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

        // ===== Bindables =====
        public IReadOnlyList<FlowerCategory> CategoryItems { get; } =
            Enum.GetValues(typeof(FlowerCategory)).Cast<FlowerCategory>().ToList();

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

            IsBusy = true;
            try
            {
                // Latest snapshot (robust if list changed since InitAsync)
                var all = await _flowerService.GetAllAsync().ConfigureAwait(false);

                // Ignore the original (update) when checking for duplicates
                var dup = all.Any(f => f.Id == newId && f.Id != _originalId);
                if (dup)
                {
                    IdError = $"A flower with Id {newId} already exists.";
                    FormError = "Please choose a unique Id.";
                    return;
                }

                // Build outgoing model; preserve runtime fields on update
                var unit = new FlowerUnit
                {
                    Id = newId,
                    Category = _selectedCategory,
                    ConnectionStatus = _originalId is null ? ConnectionStatus.Disconnected : _origConnectionStatus,
                    FlowerStatus = _originalId is null ? FlowerStatus.Closed : _origFlowerStatus,
                    CurrentBrightness = _originalId is null ? 0 : _origBrightness
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
            return ok;
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
