using OpenBoardAnim.Core;
using System;

namespace OpenBoardAnim.Models
{
    // Backing model for the "New Project" pop-up shown before the editor opens, so the user
    // can pick project settings (aspect ratio, entrance style, stroke, etc.) up front instead
    // of only discovering them later inside the editor. Project is a fresh ProjectDetails the
    // dialog edits live via the embedded ProjectSettingsView; CreateProject is only invoked if
    // the user confirms, so cancelling the dialog discards it instead of opening the editor.
    public class NewProjectPromptModel : ObservableObject
    {
        public ProjectDetails Project { get; set; }

        public Action<ProjectDetails> CreateProject;
    }
}
