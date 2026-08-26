using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenBoardAnim.Core;
using OpenBoardAnim.Library;
using OpenBoardAnim.Library.Repositories;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.ViewModels;
using System.Windows;

namespace OpenBoardAnim
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            Logger.ErrorLogged += (s, msg) => Dispatcher.Invoke(() => Growl.Error(msg));
            Logger.WarningLogged += (s, msg) => Dispatcher.Invoke(() => Growl.Warning(msg));
            Logger.MessageLogged += (s, msg) => Dispatcher.Invoke(() => Growl.Info(msg));
            try
            {
                using (var migrationContext = new DataContext())
                {
                    migrationContext.Database.Migrate();
                }

                IServiceCollection services = new ServiceCollection();
                services.AddSingleton<Func<DataContext>>(_ => () => new DataContext());
                services.AddSingleton<IShapeRepository, ShapeRepository>();
                services.AddSingleton<IGraphicRepository, GraphicRepository>();
                services.AddSingleton<ISceneRepository, SceneRepository>();
                services.AddSingleton<IProjectRepository, ProjectRepository>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IPubSubService, PubSubService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IFileDialogService, FileDialogService>();
                services.AddSingleton<IOpenFileDialogService, OpenFileDialogService>();
                services.AddSingleton<IMessageBoxService, MessageBoxService>();
                services.AddSingleton<IApplicationService, ApplicationService>();
                services.AddSingleton<IDispatcherService, DispatcherService>();
                services.AddSingleton<Func<IAppTimer>>(_ => () => new DispatcherAppTimer());
                services.AddSingleton<ICacheService, CacheService>();
                services.AddSingleton<StateSnapshotService>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<MainViewModel>();
                services.AddTransient<LaunchViewModel>();
                services.AddSingleton<EditorActionsViewModel>();
                services.AddSingleton<EditorCanvasViewModel>();
                services.AddSingleton<EditorLibraryViewModel>();
                services.AddSingleton<EditorTimelineViewModel>();
                services.AddSingleton<EditorViewModel>();
                services.AddSingleton<Func<Type, ViewModel>>(sp => vMType => (ViewModel)sp.GetRequiredService(vMType));

                services.AddSingleton<MainWindow>(provider =>
                new MainWindow
                {
                    DataContext = provider.GetRequiredService<MainViewModel>()
                });
                _serviceProvider = services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                _serviceProvider.GetRequiredService<IThemeService>().ApplySkin();
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // OnExit only runs on a graceful shutdown (File > Exit, close button, Alt+F4
                // confirmed) - a crash never reaches this. The periodic autosave backup exists
                // purely for crash recovery, but the 30s timer keeps writing it regardless of
                // whether there are unsaved changes, and can recreate it after an explicit save
                // too if it ticks again before the app closes. Clearing it here, unconditionally,
                // on every clean exit is what keeps the recovery prompt from firing next launch
                // for a session that closed normally instead of crashing.
                _serviceProvider?.GetRequiredService<ICacheService>().ClearBackup();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to clear autosave backup on exit: {ex.Message}");
            }

            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }

}
