using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenBoardAnim.Core;
using OpenBoardAnim.Library;
using OpenBoardAnim.Library.Repositories;
using OpenBoardAnim.Services;
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
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<DataContext>();
            services.AddSingleton<ShapeRepository>();
            services.AddSingleton<GraphicRepository>();
            services.AddSingleton<SceneRepository>();
            services.AddSingleton<ProjectRepository>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IPubSubService, PubSubService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<CacheService>();
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
        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            base.OnStartup(e);
        }
    }

}
