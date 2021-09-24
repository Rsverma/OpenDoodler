using Caliburn.Micro;
using OpenBoardAnim.EventModels;
using OpenBoardAnim.Models;
using OpenBoardAnim.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBoardAnim.ViewModels
{
    class IndexViewModel : Screen
    {
        private readonly IEventAggregator _events;
        public IndexViewModel(IEventAggregator events)
        {
            _events = events;
        }

        protected override void OnViewLoaded(object view)
        {
            RecentProjects.Add(new ProjectRowModel { Title = "Project1" });
            RecentProjects.Add(new ProjectRowModel { Title = "Project2" });
            base.OnViewLoaded(view);
        }
        public async void CreateNew()
        {
            await _events.PublishOnUIThreadAsync(new CreateNewProjectEvent());
        }

        private ObservableCollection<ProjectRowModel> _recentProjects = new ObservableCollection<ProjectRowModel>();

        public ObservableCollection<ProjectRowModel> RecentProjects
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
