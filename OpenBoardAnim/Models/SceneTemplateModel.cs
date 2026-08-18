using OpenBoardAnim.Core;
using System;
using System.Windows.Input;

namespace OpenBoardAnim.Models
{
    // A gallery entry in the scene-template library (EditorLibraryViewModel.SceneTemplates) -
    // wraps the actual starter-scene content (Scene) plus gallery metadata and the delegate
    // actions its card commands invoke. Never itself persisted - only Scene gets serialized
    // when saving a template (see CacheService.SaveSceneAsTemplate).
    public class SceneTemplateModel : ObservableObject
    {
        public SceneTemplateModel()
        {
            InsertTemplateCommand = new RelayCommand(o => InsertTemplate?.Invoke(this), canExecute: o => true);
            DeleteTemplateCommand = new RelayCommand(o => DeleteTemplate?.Invoke(this), canExecute: o => !IsBuiltIn);
        }

        public int Id { get; set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public bool IsBuiltIn { get; set; }
        public SceneModel Scene { get; set; }

        public Action<SceneTemplateModel> InsertTemplate;
        public Action<SceneTemplateModel> DeleteTemplate;
        public Action<SceneTemplateModel> SaveTemplate;

        public ICommand InsertTemplateCommand { get; set; }
        public ICommand DeleteTemplateCommand { get; set; }
    }
}
