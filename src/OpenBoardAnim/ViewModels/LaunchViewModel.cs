using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class LaunchViewModel : ViewModel
    {
        private BindingList<RecentProjectModel> _recentProjects;
        private INavigationService _navigation;
        private readonly IPubSubService _pubSub;
        private readonly CacheService _cache;

        public LaunchViewModel(INavigationService navigation, IPubSubService pubSub,CacheService cache)
        {
            try
            {
                Navigation = navigation;
                _pubSub = pubSub;
                _cache = cache;
                CreateNewWindowCommand = new RelayCommand(
                    execute: o => CreateAndLaunchNewProject(),
                    canExecute: o => true);
                RecentProjects = cache.RecentProjects;
                foreach (var proj in RecentProjects)
                {
                    proj.EditProject = EditProjectHandler;
                    proj.DeleteProject = DeleteProjectHandler;
                }
                // Deferred: this constructor is itself running inside an in-flight
                // Navigation.NavigateTo<LaunchViewModel>() call. Navigating to the editor
                // synchronously from here would get clobbered the instant that outer call
                // finishes and sets CurrentView back to this LaunchViewModel. Posting it to
                // the dispatcher lets that outer call complete first.
                Application.Current.Dispatcher.BeginInvoke(new Action(OfferBackupRecovery));
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Offers to recover the last periodic autosave backup, e.g. after a crash or a
        // close without saving. Asked once - the backup is cleared either way afterward so
        // the prompt doesn't keep reappearing on every future launch.
        private void OfferBackupRecovery()
        {
            try
            {
                if (!_cache.BackupExists()) return;

                MessageBoxResult result = MessageBox.Show(
                    "OpenDoodler found an autosaved backup from a previous session that wasn't saved. Recover it?",
                    "Recover Unsaved Project",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ProjectDetails project;
                    try
                    {
                        project = _cache.LoadBackup();
                    }
                    catch (Exception ex)
                    {
                        // A backup can be truncated if the app crashed mid-write - don't let
                        // an unreadable backup block the Launch screen or nag on every future
                        // launch, just discard it and carry on.
                        Logger.LogWarning($"Autosave backup was unreadable, discarding it: {ex.Message}");
                        _cache.ClearBackup();
                        return;
                    }

                    Navigation.NavigateTo<EditorViewModel>();
                    _pubSub.Publish(SubTopic.ProjectLaunched, project);
                    // The recovered content is ahead of whatever's saved on disk (or was
                    // never saved) - ProjectLaunchedHandler marks every launch "clean" by
                    // default, which would be wrong here and risk silently losing it again.
                    if (Navigation.CurrentView is EditorViewModel editor)
                        editor.Actions.MarkProjectUnsaved();
                }

                _cache.ClearBackup();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void DeleteProjectHandler(RecentProjectModel model)
        {
            try
            {
                _cache.DeleteProject(model);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void EditProjectHandler(RecentProjectModel model)
        {
            try
            {
                ProjectDetails project = _cache.LoadProjectFromFile(model);
                Navigation.NavigateTo<EditorViewModel>();
                _pubSub.Publish(SubTopic.ProjectLaunched, project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CreateAndLaunchNewProject()
        {
            try
            {
                Navigation.NavigateTo<EditorViewModel>();
                _pubSub.Publish(SubTopic.ProjectLaunched, new ProjectDetails());
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        public ICommand CreateNewWindowCommand { get; set; }
        public BindingList<RecentProjectModel> RecentProjects {
            get => _recentProjects;
            set
            {
                _recentProjects = value;
                OnPropertyChanged();
            }
        }
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