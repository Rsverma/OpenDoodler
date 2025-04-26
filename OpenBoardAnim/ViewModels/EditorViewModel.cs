using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class EditorViewModel:ViewModel
    {
        private INavigationService _navigation;
        private readonly IPubSubService _pubSub;
        private EditorActionsViewModel actions;

        public EditorViewModel(INavigationService navigation,
                               IPubSubService pubSub,
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
                SwitchToLaunchCommand = new RelayCommand(execute: SwitchToLaunchHandler, canExecute: o => true);
                Actions = actions;
                Canvas = canvas;
                Library = library;
                Timeline = timeline;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SwitchToLaunchHandler(object obj)
        {
            try
            {

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
                Actions.Project = project;
                Timeline.Scenes = new BindingList<SceneModel>(project.Scenes);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
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
