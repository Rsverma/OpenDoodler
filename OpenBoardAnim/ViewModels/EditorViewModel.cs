using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace OpenBoardAnim.ViewModels
{
    public class EditorViewModel:ViewModel
    {
        private INavigationService _navigation;
        private readonly IPubSubService _pubSub;
        private EditorActionsViewModel actions;
        private readonly DispatcherTimer _snapshotTimer;
        private readonly StateSnapshotService _stateSnapshotService;

        public EditorViewModel(INavigationService navigation,
                               IPubSubService pubSub,
                               StateSnapshotService stateSnapshotService,
                               EditorActionsViewModel actions,
                               EditorCanvasViewModel canvas,
                               EditorLibraryViewModel library,
                               EditorTimelineViewModel timeline)
        {
            try
            {
                _navigation = navigation;
                _pubSub = pubSub;
                _pubSub.Subscribe(SubTopic.ProjectLaunched, ProjectLaunchedHandler);
                _pubSub.Subscribe(SubTopic.ProjectStateRestored, ProjectStateRestoredHandler);
                SwitchToLaunchCommand = new RelayCommand(execute: SwitchToLaunchHandler, canExecute: o => true);
                Actions = actions;
                Canvas = canvas;
                Library = library;
                Timeline = timeline;
                _stateSnapshotService = stateSnapshotService;
                _snapshotTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _snapshotTimer.Tick += SaveProjectSnapshot;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }

        }

        private void SaveProjectSnapshot(object sender, EventArgs e)
        {
            if(actions?.Project != null)
            {
                _stateSnapshotService.SaveState(actions.Project);
            }
        }

        private void SwitchToLaunchHandler(object obj)
        {
            try
            {
                _snapshotTimer.Stop();
                Navigation.NavigateTo<LaunchViewModel>();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ProjectLaunchedHandler(object obj)
        {
            try
            {
                ProjectDetails project = (ProjectDetails)obj;
                _stateSnapshotService.Clear();
                LoadProjectIntoEditor(project);
                Actions.MarkProjectSaved();
                _snapshotTimer.Start();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ProjectStateRestoredHandler(object obj)
        {
            try
            {
                ProjectDetails project = (ProjectDetails)obj;
                int previousSceneIndex = Timeline.SelectedScene?.Index ?? 0;
                LoadProjectIntoEditor(project);
                // LoadProjectIntoEditor's Timeline.Scenes assignment always resets selection
                // to the first scene (the right default for a fresh project launch) - for an
                // undo/redo restore, re-select whatever scene the user was actually viewing.
                SceneModel sceneToReselect = Timeline.Scenes.FirstOrDefault(s => s.Index == previousSceneIndex);
                if (sceneToReselect != null)
                    Timeline.SelectedScene = sceneToReselect;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void LoadProjectIntoEditor(ProjectDetails project)
        {
            Actions.Project = project;
            Timeline.Scenes = new BindingList<SceneModel>(project.Scenes);
        }

        public ICommand SwitchToLaunchCommand { get; set; }
        public INavigationService Navigation
        {
            get => _navigation;
            set
            {
                _navigation = value;
                OnPropertyChanged();
            }
        }
        private EditorLibraryViewModel _library;

        public EditorLibraryViewModel Library
        {
            get { return _library; }
            set { _library = value;
                OnPropertyChanged();
            }
        }

        public EditorActionsViewModel Actions { get => actions; set => actions = value; }
        public EditorCanvasViewModel Canvas { get; set; }
        public EditorTimelineViewModel Timeline { get; set; }
    }
}
