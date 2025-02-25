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
                SelectedFontWeight = SelectedFontWeight
            };
        }
    }
}
