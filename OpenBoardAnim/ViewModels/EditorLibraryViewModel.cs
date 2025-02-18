using Microsoft.Win32;
using OpenBoardAnim.Core;
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
        private string _oldSearchText = string.Empty;
        public ICommand AddTextCommand { get; set; }
        public ICommand ImportGraphicsCommand { get; set; }
        public ICommand LoadMoreGraphicsCommand { get; set; }
        public ICommand SearchGraphicsCommand { get; set; }


        public EditorLibraryViewModel(IPubSubService pubSub, CacheService cache)
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
            ImportGraphicsCommand = new RelayCommand(ImportGraphicsCommandHandler, o => true);
            LoadMoreGraphicsCommand = new RelayCommand(LoadMoreGraphicsCommandHandler, o => true);
            SearchGraphicsCommand = new RelayCommand(SearchGraphicsCommandHandler, o => true);
        }

        private void SearchGraphicsCommandHandler(object obj)
        {
            Graphics.Clear();
            _oldSearchText = _searchText;
            List<DrawingModel> drawingModels = _cache.GetGraphics(_searchText, 0);
            foreach (var model in drawingModels)
            {
                model.AddGraphic = AddGraphicHandler;
                Graphics.Add(model);
            }
        }

        private void LoadMoreGraphicsCommandHandler(object obj)
        {
            int last = 0;
            if (Graphics?.Count > 0)
                last = Graphics.Last().ID;
            List<DrawingModel> drawingModels = _cache.GetGraphics(_oldSearchText, last);
            foreach (var model in drawingModels)
            {
                model.AddGraphic = AddGraphicHandler;
                Graphics.Add(model);
            }
        }

        private async void ImportGraphicsCommandHandler(object obj)
        {
            OpenFileDialog openFileDialog = new()
            {
                Multiselect=true,
                Filter = "SVG File (*.svg)|*.svg",
            };
            if (openFileDialog.ShowDialog() == true)
            {
                await _cache.SaveNewGraphics(openFileDialog.FileNames);
            }

            Graphics = _cache.LoadedGraphics;
            foreach (var graphic in Graphics)
            {
                graphic.AddGraphic = AddGraphicHandler;
            }
        }

        private string _searchText;

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
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
            PathGeometry pathGeometry = GeometryHelper.ConvertTextToGeometry(RawText, SelectedFontFamily, 
                SelectedTypeFace.Style, SelectedTypeFace.Weight, FontSize);
            TextModel textModel = new TextModel
            {
                TextGeometry = pathGeometry,
                RawText = RawText,
                SelectedFontFamily = SelectedFontFamily,
                SelectedFontStyle = SelectedTypeFace.Style,
                SelectedFontWeight = SelectedTypeFace.Weight,
                SelectedFontSize = FontSize
            };
            _pubSub.Publish(SubTopic.GraphicAdded, textModel);
        }
    }
}
