// Flower.App/ViewModels/IAddFlowerViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Flower.Core.Enums;
using Flower.Core.Models;

namespace Flower.App.ViewModels
{
    public interface IAddOrUpdateFlowerViewModel : INotifyPropertyChanged
    {
        Task InitAsync(FlowerUnit? existing = null);

        // ---- Bindable inputs ----
        int IdValue { get; set; }
        IReadOnlyList<FlowerCategory> CategoryItems { get; }
        FlowerCategory SelectedCategory { get; set; }

        // ---- Validation / form state ----
        string? IdError { get; }
        bool HasIdError { get; }
        string? FormError { get; }
        bool HasFormError { get; }

        // ---- Read-only previews ----
        string StatusPreview { get; }
        string FlowerStatusPreview { get; }
        string BrightnessPreview { get; }

        // ---- Actions (invoked by buttons) ----
        Task ConfirmAsync();
        void Cancel();

        // ---- Dialog close signal (returns the created FlowerUnit or null on cancel) ----
        event EventHandler<FlowerUnit?>? CloseRequested;
    }
}
