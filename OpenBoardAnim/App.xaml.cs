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
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IPubSubService, PubSubService>();
            services.AddSingleton<DataContext>();
            services.AddSingleton<GraphicRepository>();
            services.AddSingleton<SceneRepository>();
            services.AddSingleton<ProjectRepository>();
            services.AddSingleton<MainWindow>(provider =>
            new MainWindow
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            });
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<LaunchViewModel>();
            services.AddSingleton<EditorActionsViewModel>();
            services.AddSingleton<EditorCanvasViewModel>();
            services.AddSingleton<EditorLibraryViewModel>();
            services.AddSingleton<EditorTimelineViewModel>();
            services.AddSingleton<EditorViewModel>();
            services.AddSingleton<Func<Type, ViewModel>>(sp => vMType => (ViewModel)sp.GetRequiredService(vMType));
            
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
