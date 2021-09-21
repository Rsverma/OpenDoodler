using Caliburn.Micro;
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
        public IndexViewModel()
        {
            
        }

        protected override void OnViewLoaded(object view)
        {
            RecentProjects.Add(new ProjectRowModel { Title = "Project1" });
            RecentProjects.Add(new ProjectRowModel { Title = "Project2" });
            base.OnViewLoaded(view);
        }
        public async void CreateNew()
        {
            CreateNewView popUp = new CreateNewView();

            if (popUp.ShowDialog() == true)
            {
                
            }
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
