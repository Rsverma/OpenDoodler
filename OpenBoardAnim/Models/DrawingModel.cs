using OpenBoardAnim.Core;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBoardAnim.Models
{
    public class DrawingModel : GraphicModelBase
    {
        public DrawingModel()
        {
            AddGraphicCommand = new RelayCommand(AddGraphicCommandHandler, canExecute: o => true);
            DeleteGraphicCommand = new RelayCommand(DeleteGraphicCommandHandler, canExecute: o => true);
        }
        [JsonIgnore]
        public ICommand AddGraphicCommand { get; set; }
        [JsonIgnore]
        public ICommand DeleteGraphicCommand { get; set; }

        public Action<DrawingModel> AddGraphic;
        // Only wired for library items (see EditorLibraryViewModel), same as AddGraphic - a
        // DrawingModel placed on the canvas (a clone) never gets this set, so its own
        // DeleteGraphicCommand is just inert there.
        public Action<DrawingModel> DeleteGraphic;

        public int ID { get; set; }
        public string SVGText { get; set; }
        [JsonIgnore]
        public DrawingGroup ImgDrawingGroup { get; set; }
        
        public override GraphicModelBase Clone()
        {
            return new DrawingModel
            {
                Height = Height,
                Width = Width,
                ImgDrawingGroup = ImgDrawingGroup,
                Name = Name,
                X = X,
                Y = Y,
                SVGText = SVGText,
                Delay = Delay,
                Duration = Duration,
                ID = ID,
                ResizeRatio = ResizeRatio,
                IsLocked = IsLocked,
                IsVisible = IsVisible
            };
        }
        protected void AddGraphicCommandHandler(object obj)
        {
            AddGraphic?.Invoke(this);
        }

        protected void DeleteGraphicCommandHandler(object obj)
        {
            DeleteGraphic?.Invoke(this);
        }
    }
}
