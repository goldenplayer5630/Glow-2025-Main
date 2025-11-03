using Flower.Core.Models;
using Flower.Core.Records;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public interface IShowCreatorViewModel
    {
        // Props
        string FilePath { get; set; }
        ShowProject ShowProject { get; set; }
        ICollection<ShowEvent> AvailableShowEvents { get; set; }

        // Show Project State
        /// <summary>
        /// Way to save the current show project.
        /// </summary>
        ReactiveCommand<Unit, Unit> SaveShowProjectCommand { get; }
        /// <summary>
        /// Gets the command that triggers the editing and display of project properties.
        /// </summary>
        ReactiveCommand<Unit, Unit> EditShowProjectPropertiesCommand { get; }
        /// <summary>
        /// Shows a preview of the current show project.
        /// </summary>
        ReactiveCommand<Unit, Unit> PreviewShowCommand { get; }
        /// <summary>
        /// Loads an existing show project from disk.
        /// </summary>
        ReactiveCommand<Unit, Unit> LoadShowProjectCommand { get; }
        /// <summary>
        /// Creates a new show project (empty show).
        /// </summary>
        ReactiveCommand<Unit, Unit> NewShowProjectCommand { get; }


        // Flower Unit Management
        /// <summary>
        /// Opens a dialog to add a new flower unit to the show project.
        /// </summary>
        ReactiveCommand<Unit, Unit> AddFlowerUnitCommand { get; }
        /// <summary>
        /// Removes the selected flower unit from the show project.
        /// </summary>
        ReactiveCommand<Unit, Unit> RemoveFlowerUnitCommand { get; }

        // Show Track Management
        /// <summary>
        /// Opens a dialog to add a new show track to the show project.
        /// </summary>
        ReactiveCommand<Unit, Unit> AddShowTrackCommand { get; }
        /// <summary>
        /// Deletes the selected show track from the show project.
        /// </summary>
        ReactiveCommand<Unit, Unit> RemoveShowTrackCommand { get; }

        // Track Event Management
        /// <summary>
        /// Opens a dialog to add a new track event to the selected show track. In the dialog, the user can select the Show Event type and configure its properties.
        /// (it would be nice if the dialog could be replaced by a timeline editor in the future, on which a user can click and then get a dropdown for an event)
        /// </summary>
        ReactiveCommand<Unit, Unit> AddTrackEventCommand { get; }
        /// <summary>
        /// Removes the selected track event from the selected show track.
        /// </summary>
        ReactiveCommand<Unit, Unit> RemoveTrackEventCommand { get; }

        // Show Event Management
        /// <summary>
        /// Opens a dialog to add a new show event to the available show events list. The user can select the Show Event Command and configure its properties (arguments).
        /// </summary>
        ReactiveCommand<Unit, Unit> AddShowEvent { get; }
        /// <summary>
        /// Deletes the selected show event from the available show events list.
        /// </summary>
        ReactiveCommand<Unit, Unit> RemoveShowEvent { get; }

        }
}
