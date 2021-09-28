﻿using Caliburn.Micro;
using OpenBoardAnim.AppConstants;
using OpenBoardAnim.EventModels;
using OpenBoardAnim.Models;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace OpenBoardAnim.ViewModels
{
    public class ShellViewModel : Conductor<object>, IHandle<CreateNewProjectEvent>
    {
        private readonly IEventAggregator _events;
        private readonly IWindowManager _window;

        public ShellViewModel(IEventAggregator events, IWindowManager window)
        {
            _events = events;
            _window = window;
            _events.SubscribeOnPublishedThread(this);
            ActivateItemAsync(IoC.Get<IndexViewModel>());
        }

        public async Task HandleAsync(CreateNewProjectEvent message, CancellationToken cancellationToken)
        {
            dynamic settings = new ExpandoObject();
            settings.ShowInTaskbar = false;
            settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CreateNewViewModel viewModel = IoC.Get<CreateNewViewModel>();
            if (await _window.ShowDialogAsync(viewModel, null, settings))
            {
                ProjectViewModel projectView = IoC.Get<ProjectViewModel>();
                projectView.Project = new ProjectModel
                {
                    Title = viewModel.ProjectTitle,
                    BoardStyle = (BoardStyle)viewModel.SelectedBoardStyle,
                    Resolution = (Resolution)viewModel.SelectedResolution
                };
                await ActivateItemAsync(projectView);
            }
        }
    }
}