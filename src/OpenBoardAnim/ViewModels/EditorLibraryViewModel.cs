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
        private readonly ICacheService _cache;
        private readonly IDialogService _dialog;
        private string _oldSearchText = string.Empty;
        // Tracked purely so "Save Current Scene as Template" knows what to save - Save/Insert
        // template graphics stay independent of the current on-canvas selection.
        private SceneModel _currentScene;
        public ICommand AddTextCommand { get; set; }
        public ICommand ImportGraphicsCommand { get; set; }
        public ICommand LoadMoreGraphicsCommand { get; set; }
        public ICommand SearchGraphicsCommand { get; set; }
        public ICommand SaveCurrentSceneAsTemplateCommand { get; set; }
        public ICommand ManageLibraryCommand { get; set; }
        public ICommand CleanupInvalidGraphicsCommand { get; set; }


        public EditorLibraryViewModel(IPubSubService pubSub, ICacheService cache, IDialogService dialog)
        {
            try
            {
                _pubSub = pubSub;
                _cache = cache;
                _dialog = dialog;
                _pubSub.Subscribe(SubTopic.SceneChanged, SceneChangedHandler);
                Graphics = cache.LoadedGraphics;
                Shapes = cache.AllShapes;
                foreach (var graphic in Graphics)
                {
                    graphic.AddGraphic = AddGraphicHandler;
                    graphic.DeleteGraphic = DeleteGraphicHandler;
                }
                foreach (var shape in Shapes)
                {
                    shape.AddGraphic = AddGraphicHandler;
                }
                SceneTemplates = cache.LoadedSceneTemplates;
                WireSceneTemplateActions(SceneTemplates);
                AddTextCommand = new RelayCommand(AddTextCommandHandler,
                    canExecute: o => { return !string.IsNullOrEmpty(RawText) && SelectedFontFamily is not null && SelectedTypeFace is not null; });
                ImportGraphicsCommand = new RelayCommand(ImportGraphicsCommandHandler, o => true);
                LoadMoreGraphicsCommand = new RelayCommand(LoadMoreGraphicsCommandHandler, o => true);
                SearchGraphicsCommand = new RelayCommand(SearchGraphicsCommandHandler, o => true);
                SaveCurrentSceneAsTemplateCommand = new RelayCommand(o => SaveCurrentSceneAsTemplateHandler(), canExecute: o => _currentScene != null);
                ManageLibraryCommand = new RelayCommand(o => ManageLibraryHandler(), canExecute: o => true);
                CleanupInvalidGraphicsCommand = new RelayCommand(o => CleanupInvalidGraphicsHandler(), canExecute: o => true);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SearchGraphicsCommandHandler(object obj)
        {
            try
            {
                Graphics.Clear();
                _oldSearchText = _searchText;
                List<DrawingModel> drawingModels = _cache.GetGraphics(_searchText, 0);
                foreach (var model in drawingModels)
                {
                    model.AddGraphic = AddGraphicHandler;
                    model.DeleteGraphic = DeleteGraphicHandler;
                    Graphics.Add(model);
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void LoadMoreGraphicsCommandHandler(object obj)
        {
            try
            {
                int last = 0;
                if (Graphics?.Count > 0)
                    last = Graphics.Last().ID;
                List<DrawingModel> drawingModels = _cache.GetGraphics(_oldSearchText, last);
                foreach (var model in drawingModels)
                {
                    model.AddGraphic = AddGraphicHandler;
                    model.DeleteGraphic = DeleteGraphicHandler;
                    Graphics.Add(model);
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private async void ImportGraphicsCommandHandler(object obj)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Multiselect = true,
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
                    graphic.DeleteGraphic = DeleteGraphicHandler;
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
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

        private bool _isUnderline;
        public bool IsUnderline
        {
            get { return _isUnderline; }
            set
            {
                _isUnderline = value;
                OnPropertyChanged();
            }
        }

        private string _selectedTextColorHex = "#FF000000";
        public string SelectedTextColorHex
        {
            get { return _selectedTextColorHex; }
            set
            {
                _selectedTextColorHex = value;
                try { _selectedTextColor = (Brush)new BrushConverter().ConvertFromString(value); }
                catch (FormatException) { /* keep the previous color on an unparsable hex value */ }
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedTextColor));
            }
        }

        // Paired with SelectedTextColorHex (same pattern as TextModel.SelectedColorHex/SelectedColor)
        // so the "Sample" preview's Foreground can bind to an actual Brush rather than relying on
        // an implicit string-to-Brush binding conversion.
        private Brush _selectedTextColor = Brushes.Black;
        public Brush SelectedTextColor => _selectedTextColor;

        private BindingList<SceneTemplateModel> _sceneTemplates;

        public BindingList<SceneTemplateModel> SceneTemplates
        {
            get { return _sceneTemplates; }
            set
            {
                _sceneTemplates = value;
                OnPropertyChanged();
            }
        }

        private void WireSceneTemplateActions(BindingList<SceneTemplateModel> templates)
        {
            foreach (SceneTemplateModel template in templates)
            {
                template.InsertTemplate = InsertTemplateHandler;
                template.DeleteTemplate = DeleteTemplateHandler;
            }
        }

        // Inserts the template as a brand-new scene rather than overwriting anything, so
        // picking a starter layout never destroys existing work.
        private void InsertTemplateHandler(SceneTemplateModel template)
        {
            try
            {
                _pubSub.Publish(SubTopic.SceneTemplateInserted, template.Scene.Clone());
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void DeleteTemplateHandler(SceneTemplateModel template)
        {
            try
            {
                _cache.DeleteSceneTemplate(template);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SaveCurrentSceneAsTemplateHandler()
        {
            try
            {
                if (_currentScene == null) return;
                SceneTemplateModel prompt = new()
                {
                    Name = "My Scene",
                    Scene = _currentScene,
                    SaveTemplate = SaveTemplateHandler
                };
                _ = _dialog.ShowDialog(DialogType.SaveSceneTemplate, prompt);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SaveTemplateHandler(SceneTemplateModel prompt)
        {
            try
            {
                _cache.SaveSceneAsTemplate(prompt.Scene, prompt.Name);
                SceneTemplates = _cache.LoadedSceneTemplates;
                WireSceneTemplateActions(SceneTemplates);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneChangedHandler(object obj)
        {
            _currentScene = obj as SceneModel;
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
        private BindingList<DrawingModel> _shapes;

        public BindingList<DrawingModel> Shapes
        {
            get { return _shapes; }
            set
            {
                _shapes = value;
                OnPropertyChanged();
            }
        }
        private void AddGraphicHandler(DrawingModel model)
        {
            try
            {
                _pubSub.Publish(SubTopic.GraphicAdded, model.Clone());
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // No confirmation prompt - matches DeleteSceneTemplate's own behavior, and only ever
        // removes the library entry (see CacheService.DeleteGraphic), not anything already
        // placed on a canvas.
        private void DeleteGraphicHandler(DrawingModel model)
        {
            try
            {
                _cache.DeleteGraphic(model);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ManageLibraryHandler()
        {
            try
            {
                _ = _dialog.ShowDialog(DialogType.LibraryManager, this);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CleanupInvalidGraphicsHandler()
        {
            try
            {
                int removed = _cache.CleanupInvalidGraphics();
                MessageBox.Show(
                    removed > 0 ? $"Removed {removed} invalid graphic(s) from the library." : "No invalid graphics found.",
                    "Library Cleanup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void AddTextCommandHandler(object obj)
        {
            try
            {
                PathGeometry pathGeometry = GeometryHelper.ConvertTextToGeometry(RawText, SelectedFontFamily,
                        SelectedTypeFace.Style, SelectedTypeFace.Weight, FontSize, IsUnderline);
                TextModel textModel = new TextModel
                {
                    TextGeometry = pathGeometry,
                    RawText = RawText,
                    SelectedFontFamily = SelectedFontFamily,
                    SelectedFontStyle = SelectedTypeFace.Style,
                    SelectedFontWeight = SelectedTypeFace.Weight,
                    SelectedFontSize = FontSize,
                    IsUnderline = IsUnderline,
                    SelectedColorHex = SelectedTextColorHex
                };
                _pubSub.Publish(SubTopic.GraphicAdded, textModel);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
