using OpenBoardAnim.Core;

namespace OpenBoardAnim.Models
{
    // A scene's position and width (in pixels) on the project-wide timeline, derived from its
    // estimated on-screen duration. View-only - never persisted.
    public class SceneTimelineSegment : ObservableObject
    {
        public SceneModel Scene { get; set; }

        private double _x;
        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        private double _width;
        public double Width
        {
            get { return _width; }
            set
            {
                _width = value;
                OnPropertyChanged();
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }
}
