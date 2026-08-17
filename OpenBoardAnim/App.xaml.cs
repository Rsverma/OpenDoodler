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
                services.AddSingleton<ShapeRepository>();
                services.AddSingleton<GraphicRepository>();
                services.AddSingleton<SceneRepository>();
                services.AddSingleton<ProjectRepository>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IPubSubService, PubSubService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<CacheService>();
                services.AddSingleton<StateSnapshotService>();
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
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }

}
