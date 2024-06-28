using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
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
            _navigation = navigation;
            _pubSub = pubSub;
            _pubSub.Subscribe(SubTopic.ProjectLaunched, ProjectLaunchedHandler);
            SwitchToLaunchCommand = new RelayCommand(
                execute: o => { Navigation.NavigateTo<LaunchViewModel>(); },
                canExecute: o => true);
            Actions = actions;
            Canvas = canvas;
            Library = library;
            Timeline = timeline;
        }

        private void ProjectLaunchedHandler(object obj)
        {
            ProjectDetails project = (ProjectDetails)obj;
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
