﻿using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Views;

namespace OpenBoardAnim.Services
{
    public enum DialogType
    {
        SceneSettings,
        ProjectSettings,
        AboutUs,
        PreviewProject
    }
    public interface IDialogService
    {
        bool? ShowDialog<T>(DialogType dialogType, T model) where T : ObservableObject;
    }
    public class DialogService : IDialogService
    {
        private readonly Func<Type, ViewModel> _viewModelFactory;

        public DialogService(Func<Type, ViewModel> viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public bool? ShowDialog<T>(DialogType dialogType, T model = null) where T : ObservableObject
        {
            switch (dialogType)
            {
                case DialogType.PreviewProject:
                    {
                        DialogWindow dialog = new DialogWindow
                        {
                            DataContext = model
                        };
                        return dialog.ShowDialog();
                    }
            }
            return null;
        }
    }
}
