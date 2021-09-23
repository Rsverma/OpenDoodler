using Caliburn.Micro;
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
    public class ShellViewModel : Conductor<object>
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
    }
}
