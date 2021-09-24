﻿using Caliburn.Micro;
using OpenBoardAnim.EventModels;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
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
            await _window.ShowDialogAsync(viewModel, null, settings);

        }
    }
}