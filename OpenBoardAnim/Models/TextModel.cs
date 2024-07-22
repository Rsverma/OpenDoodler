using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace OpenBoardAnim.Models
{
    public class TextModel : GraphicModelBase
    {
        public PathGeometry TextGeometry { get; set; }

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
            };
        }
    }
}
