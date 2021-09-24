using Caliburn.Micro;
using OpenBoardAnim.EventModels;
using OpenBoardAnim.Models;
using System.Collections.ObjectModel;

namespace OpenBoardAnim.ViewModels
{
    internal class IndexViewModel : Screen
    {
        private readonly IEventAggregator _events;

        public IndexViewModel(IEventAggregator events)
        {
            _events = events;
        }

        protected override void OnViewLoaded(object view)
        {
            RecentProjects.Add(new ProjectModel { Title = "Project1" });
            RecentProjects.Add(new ProjectModel { Title = "Project2" });
            base.OnViewLoaded(view);
        }

        public async void CreateNew()
        {
            await _events.PublishOnUIThreadAsync(new CreateNewProjectEvent());
        }

        private ObservableCollection<ProjectModel> _recentProjects = new ObservableCollection<ProjectModel>();

        public ObservableCollection<ProjectModel> RecentProjects
        {
            get { return _recentProjects; }
            set
            {
                _recentProjects = value;

                NotifyOfPropertyChange(() => RecentProjects);
            }
        }
    }
}