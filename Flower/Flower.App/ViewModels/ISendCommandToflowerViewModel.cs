using Flower.Core.Models;
using Flower.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public interface ISendCommandToflowerViewModel
    {
        string HeaderText { get; }
        IReadOnlyList<CommandOption> CommandOptions { get; }   // Id + Title to show in ComboBox
        bool IsBusy { get; }
        bool CanConfirm { get; }

        Task InitAsync(FlowerUnit target);
        Task ConfirmAsync();
        void Cancel();

        bool CloseAfterSend { get; set; }

        event EventHandler<string?>? CloseRequested;           // returns the commandId (or null if canceled)
    }
}
