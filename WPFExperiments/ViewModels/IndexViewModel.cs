using Caliburn.Micro;
using OpenBoardAnim.Models;
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
            RecentProjects.Add(new ProjectRowModel());
            base.OnViewLoaded(view);
        }
        public async Task CreateNew()
        {
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
