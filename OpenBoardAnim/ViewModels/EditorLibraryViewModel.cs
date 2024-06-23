using OpenBoardAnim.Core;
using OpenBoardAnim.Library.Repositories;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace OpenBoardAnim.ViewModels
{
    public class EditorLibraryViewModel : ViewModel
    {
        private IPubSubService _pubSub;
        private readonly SceneRepository _sRepo;
        private readonly GraphicRepository _gRepo;

        public EditorLibraryViewModel(IPubSubService pubSub,SceneRepository sRepo,GraphicRepository gRepo)
        {
            _pubSub = pubSub;
            _sRepo = sRepo;
            _gRepo = gRepo;
            string folder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            List <SceneModel> scenes = sRepo.SceneEntities.Select(e =>
            new SceneModel
            {
                Name = e.Name,
                ImgPath = e.FilePath,
                ReplaceScene = ReplaceSceneHandler,
                ImgGeometry = GetPathGeometryFromSVG(Path.Combine(folder, e.FilePath))
            }).ToList();
            Scenes = new BindingList<SceneModel>(scenes);

            List<GraphicModel> graphics = gRepo.GraphicEntities.Select(e =>
            new GraphicModel
            {
                Name = e.Name,
                ImgPath = e.FilePath,
                AddGraphic = AddGraphicHandler,
                ImgGeometry = GetPathGeometryFromSVG(Path.Combine(folder, e.FilePath))
            }).ToList();
            Graphics = new BindingList<GraphicModel>(graphics);
        }

        private Geometry GetPathGeometryFromSVG(string filePath)
        {
            var svgFileReader = new FileSvgReader(new WpfDrawingSettings());
            var drawingGroup = svgFileReader.Read(filePath);
            GeometryGroup geometryGroup = ConvertToGeometry(drawingGroup);
            return geometryGroup;
        }

        private static GeometryGroup ConvertToGeometry(DrawingGroup drawingGroup)
        {
            var geometryGroup = new GeometryGroup();
            if (drawingGroup != null)
            {
                foreach (var drawing in drawingGroup.Children)
                {
                    if (drawing is GeometryDrawing geometryDrawing)
                    {
                        geometryGroup.Children.Add(geometryDrawing.Geometry);
                    }
                    else if (drawing is DrawingGroup innerGroup)
                    {
                        geometryGroup.Children.Add(ConvertToGeometry(innerGroup));
                    }
                }
            }

            return geometryGroup;
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
            _pubSub.Publish(SubTopic.SceneReplaced, model.ImgGeometry);
        }

        private BindingList<GraphicModel> _graphics;

        public BindingList<GraphicModel> Graphics
        {
            get { return _graphics; }
            set
            {
                _graphics = value;
                OnPropertyChanged();
            }
        }
        private void AddGraphicHandler(GraphicModel model)
        {
            _pubSub.Publish(SubTopic.GraphicAdded, model.Clone());
        }

    }
}
