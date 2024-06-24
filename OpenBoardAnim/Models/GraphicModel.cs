using OpenBoardAnim.Core;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBoardAnim.Models
{
    public class GraphicModel : ObservableObject
    {
        public string Name { get; set; }
        public string ImgPath { get; set; }
        public DrawingGroup ImgGeometry { get; set; }
        public string Shape { get; set; }
        public ICommand AddGraphicCommand { get; set; }
        public double Width { get; set; } = 100;
        public double Height { get; set; } = 100;
        public double Delay { get; set; }
        public double Duration { get; set; }
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
                ImgPath = ImgPath,
                Name = Name,
                Shape = Shape,
                X = X,
                Y = Y
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

        private void AddGraphicCommandHandler(object obj)
        {
            if (AddGraphic != null) { AddGraphic(this); }
        }


    }
}
