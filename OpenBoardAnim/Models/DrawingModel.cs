﻿using OpenBoardAnim.Core;
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
        }
        [JsonIgnore]
        public ICommand AddGraphicCommand { get; set; }

        public Action<DrawingModel> AddGraphic;
        protected void AddGraphicCommandHandler(object obj)
        {
            AddGraphic?.Invoke(this);
        }

        public string SVGPath { get; set; }
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
                SVGPath = SVGPath,
                Delay = Delay,
                Duration = Duration,
            };
        }
    }
}
