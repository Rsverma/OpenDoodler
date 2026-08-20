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
        private readonly CacheService _cache;
        private EditorActionsViewModel actions;
        private readonly DispatcherTimer _snapshotTimer;
        // Periodic disk backup - distinct from _snapshotTimer's in-memory undo/redo stack,
        // which is lost on crash or close-without-saving. Runs less often than the snapshot
        // timer since it's real file I/O, not just an in-memory push.
        private readonly DispatcherTimer _backupTimer;
        private readonly StateSnapshotService _stateSnapshotService;

        public EditorViewModel(INavigationService navigation,
                               IPubSubService pubSub,
                               CacheService cache,
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
                _cache = cache;
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
                _backupTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
                _backupTimer.Tick += SaveProjectBackup;
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
                actions.RefreshUnsavedStatus();
            }
        }

        private void SaveProjectBackup(object sender, EventArgs e)
        {
            // Skip the write entirely if nothing has changed since the project was last saved
            // (or, for a never-saved project, since it was loaded/created) - HasUnsavedChanges
            // does its own live comparison, so this stays accurate even though it's a plain
            // computed property rather than something this timer tracks itself.
            if (actions?.Project != null && actions.HasUnsavedChanges)
            {
                _cache.SaveBackup(actions.Project);
            }
        }

        private void SwitchToLaunchHandler(object obj)
        {
            try
            {
                _snapshotTimer.Stop();
                _backupTimer.Stop();
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
                _backupTimer.Start();
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
            Timeline.Project = project;
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
