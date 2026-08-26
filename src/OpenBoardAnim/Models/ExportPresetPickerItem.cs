using OpenBoardAnim.Core;
using OpenBoardAnim.Utils;

namespace OpenBoardAnim.Models
{
    // One row in ProjectSettingsView's export-preset picker - wraps a real ExportPresetOption, or
    // Preset == null for the trailing "Custom" row (which reveals the raw Aspect Ratio combo
    // instead of setting AspectRatio directly). Built fresh per ProjectSettingsView instance
    // (rather than binding straight to the shared, static ExportPresets.All) so IsSelected is
    // scoped to one view - e.g. the New Project popup and an in-editor Project Settings dialog
    // must be able to show different active presets at the same time.
    public class ExportPresetPickerItem : ObservableObject
    {
        public string Name { get; set; }
        public ExportPresetOption Preset { get; set; }
        public bool HasResolution => Preset != null;
        public bool IsCustom => Preset == null;

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
