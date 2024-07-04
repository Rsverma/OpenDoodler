using OpenBoardAnim.Core;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBoardAnim.Models
{
    public class GraphicModel : ObservableObject
    {
        public string Name { get; set; }
        public string SVGPath { get; set; }
        [JsonIgnore]
        public DrawingGroup ImgGeometry { get; set; }
        [JsonIgnore]
        public ICommand AddGraphicCommand { get; set; }
        public GraphicModel()
        {

            AddGraphicCommand = new RelayCommand(AddGraphicCommandHandler,
                canExecute: o => true);
        }
        public Action<GraphicModel> AddGraphic;
        
        public GraphicModel Clone()
        {
            return new GraphicModel
            {
                Height = Height,
                Width = Width,
                ImgGeometry = ImgGeometry,
                Name = Name,
                X = X,
                Y = Y,
                SVGPath = SVGPath,
                Delay  = Delay,
                Duration    = Duration,
            };
        }

        private double x;
        public double X
        {
            get { return x; }
            set
            {
                x = value;
                OnPropertyChanged();
            }
        }

        private double y;
        public double Y
        {
            get { return y; }
            set
            {
                y = value;
                OnPropertyChanged();
            }
        }

        private double _delay = 0;
        public double Delay
    {
            get { return _delay; }
            set
            {
                _delay = value;
                OnPropertyChanged();
            }
        }

        private double _duration = 1;
        public double Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;
                OnPropertyChanged();
            }
        }

        private double _height = 100;
        public double Height
        {
            get { return _height; }
            set
            {
                _height = value;
                OnPropertyChanged();
            }
        }

        private double _width = 100;
        public double Width
        {
            get { return _width; }
            set
            {
                _width = value;
                OnPropertyChanged();
            }
        }

        private void AddGraphicCommandHandler(object obj)
        {
            AddGraphic?.Invoke(this);
        }


    }
}
