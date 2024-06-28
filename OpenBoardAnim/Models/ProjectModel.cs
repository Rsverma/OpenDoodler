using OpenBoardAnim.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenBoardAnim.Models
{
    public class ProjectModel
    {
        public ProjectModel()
        {
            EditProjectCommand = new RelayCommand(EditProjectCommandHandler,
                canExecute: o => true);
        }
        public Action<ProjectModel> EditProject;

        public ICommand EditProjectCommand { get; set; }

        private void EditProjectCommandHandler(object obj)
        {
            EditProject?.Invoke(this);
        }
        public string FilePath { get; set; }
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

        private DateTime _modifiedOn;

        public DateTime ModifiedOn
        {
            get { return _modifiedOn; }
            set { _modifiedOn = value; }
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
