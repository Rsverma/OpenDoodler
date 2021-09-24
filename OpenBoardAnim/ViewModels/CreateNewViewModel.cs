using Caliburn.Micro;
using OpenBoardAnim.AppConstants;

namespace OpenBoardAnim.ViewModels
{
    public class CreateNewViewModel : Conductor<object>
    {
        private int _selectedBoardStyle = 0;

        public int SelectedBoardStyle
        {
            get { return _selectedBoardStyle; }
            set
            {
                _selectedBoardStyle = value;
                NotifyOfPropertyChange(() => SelectedBoardStyle);
            }
        }

        private string _projectTitle;

        public string ProjectTitle
        {
            get { return _projectTitle; }
            set
            {
                _projectTitle = value;
                NotifyOfPropertyChange(() => ProjectTitle);
            }
        }

        private int _selectedResolution = 0;

        public int SelectedResolution
        {
            get { return _selectedResolution; }
            set
            {
                _selectedResolution = value;
                NotifyOfPropertyChange(() => SelectedResolution);
            }
        }
    }
}
