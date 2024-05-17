using OpenBoardAnim.Core;
using OpenBoardAnim.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class EditorViewModel:ViewModel
    {
        private INavigationService _navigation;

        public EditorViewModel(INavigationService navigation)
        {
            _navigation = navigation;
            SwitchToLaunchCommand = new RelayCommand(
                execute: o => { Navigation.NavigateTo<LaunchViewModel>(); },
                canExecute: o => true);
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
    }
}
