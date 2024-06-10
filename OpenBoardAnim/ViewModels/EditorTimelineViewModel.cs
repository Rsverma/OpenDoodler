using OpenBoardAnim.Core;

namespace OpenBoardAnim.ViewModels
{
    public class EditorTimelineViewModel : ViewModel
    {
        public EditorTimelineViewModel()
        {
            Text1 = "library";
        }

        private string _text;

        public string Text1
        {
            get { return _text; }
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }
}
