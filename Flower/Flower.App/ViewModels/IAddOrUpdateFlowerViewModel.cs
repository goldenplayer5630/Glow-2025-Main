// Flower.App/ViewModels/IAddFlowerViewModel.cs
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
    public interface IAddOrUpdateFlowerViewModel : INotifyPropertyChanged
    {
        event EventHandler<FlowerUnit?>? CloseRequested;

        // Static previews (readonly computed)
        string StatusPreview { get; }
        string FlowerStatusPreview { get; }
        string BrightnessPreview { get; }

        // Bindable lists
        IReadOnlyList<FlowerCategory> CategoryItems { get; }
        IReadOnlyList<FlowerStatus> StatusItems { get; }

        // Editable / user selections
        bool UseManualStatusOverride { get; set; }
        FlowerStatus SelectedStatus { get; set; }
        int PriorityValue { get; set; }
        int IdValue { get; set; }
        FlowerCategory SelectedCategory { get; set; }

        // UX state
        bool IsBusy { get; }
        bool CanConfirm { get; }

        // Validation / errors
        string? PriorityError { get; }
        bool HasPriorityError { get; }
        string? IdError { get; }
        bool HasIdError { get; }
        string? FormError { get; }
        bool HasFormError { get; }

        // Lifecycle
        Task InitAsync(FlowerUnit? existing = null);


        // Actions
        Task ConfirmAsync();
        void Cancel();
    }
}




