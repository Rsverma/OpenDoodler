using OpenBoardAnim.Core;
using OpenBoardAnim.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenBoardAnim.Models
{
    public class RecentProjectModel : ObservableObject
    {
        public RecentProjectModel()
        {
            EditProjectCommand = new RelayCommand(EditProjectCommandHandler,
                canExecute: o => true);
            DeleteProjectCommand = new RelayCommand(DeleteProjectCommandHandler,
                canExecute: o => true);
        }
        public Action<RecentProjectModel> EditProject;
        public Action<RecentProjectModel> DeleteProject;

        public ICommand EditProjectCommand { get; set; }

        private void EditProjectCommandHandler(object obj)
        {
            EditProject?.Invoke(this);
        }
        public ICommand DeleteProjectCommand { get; set; }

        private void DeleteProjectCommandHandler(object obj)
        {
            DeleteProject?.Invoke(this);
        }
        public int ProjectID { get; set; }
        public string FilePath { get; set; }

        // Computed rather than stored - see ThumbnailHelper for why the thumbnail cache is
        // keyed off the project's own file path instead of ProjectID or a new DB column.
        public string ThumbnailPath => ThumbnailHelper.GetThumbnailPath(FilePath);
        private string _title;

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        private DateTime _createdOn;

        public DateTime CreatedOn
        {
            get { return _createdOn; }
            set { _createdOn = value; }
        }

        private DateTime _latestLaunchTime;

        public DateTime LatestLaunchTime
        {
            get { return _latestLaunchTime; }
            set { _latestLaunchTime = value; }
        }

        private int _length;

        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }

        private int _scenes;

        public int Scenes
        {
            get { return _scenes; }
            set { _scenes = value; }
        }

    }
}
