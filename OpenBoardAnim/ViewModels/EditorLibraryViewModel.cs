﻿using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBoardAnim.ViewModels
{
    public class EditorLibraryViewModel : ViewModel
    {
        private IPubSubService _pubSub;
        private readonly CacheService _cache;
        public ICommand AddTextCommand { get; set; }


        public EditorLibraryViewModel(IPubSubService pubSub,CacheService cache)
        {
            _pubSub = pubSub;
            _cache = cache;
            Graphics = cache.LoadedGraphics;
            foreach (var graphic in Graphics)
            {
                graphic.AddGraphic = AddGraphicHandler;
            }
            Scenes = cache.LoadedScenes;
            foreach (var scene in Scenes)
            {
                scene.ReplaceScene = ReplaceSceneHandler;
            }
            AddTextCommand = new RelayCommand(AddTextCommandHandler,
                canExecute: o => { return !string.IsNullOrEmpty(RawText) && SelectedFontFamily is not null && SelectedTypeFace is not null; });
        }

        private string _rawText;

        public string RawText
        {
            get { return _rawText; }
            set
            {
                _rawText = value;
                OnPropertyChanged();
            }
        }

        private FontFamily _selectedFontFamily;

        public FontFamily SelectedFontFamily
        {
            get { return _selectedFontFamily; }
            set
            {
                _selectedFontFamily = value;
                OnPropertyChanged();
            }
        }

        private FamilyTypeface _selectedTypeFace;

        public FamilyTypeface SelectedTypeFace
        {
            get { return _selectedTypeFace; }
            set
            {
                _selectedTypeFace = value;
                OnPropertyChanged();
            }
        }

        private double _fontSize=20;

        public double FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                OnPropertyChanged();
            }
        }

        private BindingList<SceneModel> _scenes;

        public BindingList<SceneModel> Scenes
        {
            get { return _scenes; }
            set
            {
                _scenes = value;
                OnPropertyChanged();
            }
        }

        private void ReplaceSceneHandler(SceneModel model)
        {
            _pubSub.Publish(SubTopic.SceneReplaced, model.Clone());
        }

        private BindingList<DrawingModel> _graphics;

        public BindingList<DrawingModel> Graphics
        {
            get { return _graphics; }
            set
            {
                _graphics = value;
                OnPropertyChanged();
            }
        }
        private void AddGraphicHandler(DrawingModel model)
        {
            _pubSub.Publish(SubTopic.GraphicAdded, model.Clone());
        }

        private void AddTextCommandHandler(object obj)
        {
            PathGeometry pathGeometry = GeometryHelper.ConvertTextToGeometry(RawText, SelectedFontFamily, SelectedTypeFace.Style, SelectedTypeFace.Weight, FontSize);
            TextModel textModel = new TextModel
            {
                TextGeometry = pathGeometry
            };
            _pubSub.Publish(SubTopic.GraphicAdded, textModel);
        }
    }
}
