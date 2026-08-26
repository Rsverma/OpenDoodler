using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OpenBoardAnim.Models
{
    public class TextModel : GraphicModelBase
    {
        public TextModel()
        {

        }
        [JsonIgnore]
        public PathGeometry TextGeometry { get; set; }
        public string RawText { get;  set; }

        private string _selectedFontFamilyString;

        public string SelectedFontFamilyString
        {
            get { return _selectedFontFamilyString; }
            set
            {
                _selectedFontFamilyString = value;
                selectedFontFamily = new FontFamily(_selectedFontFamilyString);
            }
        }

        private FontFamily selectedFontFamily;
        [JsonIgnore]
        public FontFamily SelectedFontFamily
        {
            get => selectedFontFamily;
            set
            {
                selectedFontFamily = value;
                _selectedFontFamilyString = selectedFontFamily.Source;
                OnPropertyChanged();
            }
        }
        public FontStyle SelectedFontStyle { get;  set; }
        public FontWeight SelectedFontWeight { get;  set; }
        public double SelectedFontSize { get;  set; }
        public bool IsUnderline { get; set; }
        public bool IsStrikethrough { get; set; }
        // Per-block bold/italic/underline/strikethrough overrides on top of
        // SelectedFontStyle/SelectedFontWeight/IsUnderline/IsStrikethrough - empty for text
        // created before this existed, in which case those base properties apply to the whole
        // string exactly as before.
        public List<TextFormatRun> FormatRuns { get; set; } = new();

        private string _selectedColorHex = "#FF000000";
        public string SelectedColorHex
        {
            get { return _selectedColorHex; }
            set
            {
                _selectedColorHex = value;
                try { selectedColor = (Brush)new BrushConverter().ConvertFromString(value); }
                catch (FormatException) { /* keep the previous color on an unparsable hex value */ }
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedColor));
            }
        }

        private Brush selectedColor = Brushes.Black;
        [JsonIgnore]
        public Brush SelectedColor => selectedColor;

        public override GraphicModelBase Clone()
        {
            return new TextModel
            {
                Height = Height,
                Width = Width,
                TextGeometry = TextGeometry,
                Name = Name,
                X = X,
                Y = Y,
                Delay = Delay,
                Duration = Duration,
                RawText = RawText,
                ResizeRatio = ResizeRatio,
                SelectedFontFamily = SelectedFontFamily,
                SelectedFontSize = SelectedFontSize,
                SelectedFontStyle = SelectedFontStyle,
                SelectedFontWeight = SelectedFontWeight,
                IsLocked = IsLocked,
                IsVisible = IsVisible,
                IsUnderline = IsUnderline,
                IsStrikethrough = IsStrikethrough,
                SelectedColorHex = SelectedColorHex,
                FormatRuns = FormatRuns?.Select(r => new TextFormatRun
                {
                    Start = r.Start,
                    Length = r.Length,
                    IsBold = r.IsBold,
                    IsItalic = r.IsItalic,
                    IsUnderline = r.IsUnderline,
                    IsStrikethrough = r.IsStrikethrough
                }).ToList() ?? new List<TextFormatRun>()
            };
        }
    }
}
