using OpenBoardAnim.Core;

namespace OpenBoardAnim.Models
{
    // One row in ProjectSettingsView's hand-style picker - wraps a HandStyleOption. Built fresh
    // per ProjectSettingsView instance (same reasoning as ExportPresetPickerItem) so IsSelected
    // stays scoped to one view rather than a shared static list.
    public class HandStylePickerItem : ObservableObject
    {
        public string Name { get; set; }
        public HandStyle Style { get; set; }
        public string ThumbnailUri { get; set; }
        public bool HasThumbnail => ThumbnailUri != null;
        public bool HasNoThumbnail => ThumbnailUri == null;

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
