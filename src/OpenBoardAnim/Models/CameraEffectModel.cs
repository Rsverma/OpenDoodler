using OpenBoardAnim.Core;
using OpenBoardAnim.Utilities;
using System;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace OpenBoardAnim.Models
{
    // One camera pan/zoom move within a scene's camera-effects layer (SceneModel.CameraEffects).
    // FocusX/FocusY is the canvas-space point the camera centers on; Zoom is a scale factor
    // where 1.0 fits the whole EditorWidth x EditorHeight canvas in the viewport. StartTime/
    // EndTime are absolute seconds from the scene's own start - effects play back sorted by
    // StartTime (not list order), holding wherever the camera last landed until the next
    // effect's StartTime arrives, then snapping to this effect's Start and animating to its End
    // over (EndTime - StartTime) seconds. Start/End are independently authored - if Start
    // doesn't match the previous effect's End, that's an intentional jump-cut, not a bug.
    public class CameraEffectModel : ObservableObject
    {
        public CameraEffectModel()
        {
            EditEffectCommand = new RelayCommand(o => EditEffect?.Invoke(this), canExecute: o => true);
            DeleteEffectCommand = new RelayCommand(o => DeleteEffect?.Invoke(this), canExecute: o => true);
        }

        private double _startFocusX;
        public double StartFocusX { get => _startFocusX; set { _startFocusX = value; OnPropertyChanged(); } }
        private double _startFocusY;
        public double StartFocusY { get => _startFocusY; set { _startFocusY = value; OnPropertyChanged(); } }
        private double _startZoom = 1.0;
        public double StartZoom { get => _startZoom; set { _startZoom = value; OnPropertyChanged(); } }
        private double _endFocusX;
        public double EndFocusX { get => _endFocusX; set { _endFocusX = value; OnPropertyChanged(); } }
        private double _endFocusY;
        public double EndFocusY { get => _endFocusY; set { _endFocusY = value; OnPropertyChanged(); } }
        private double _endZoom = 1.0;
        public double EndZoom { get => _endZoom; set { _endZoom = value; OnPropertyChanged(); } }

        // Absolute seconds from the scene's start when this move begins/ends.
        private double _startTime;
        public double StartTime { get => _startTime; set { _startTime = value; OnPropertyChanged(); } }
        private double _endTime = 2.0;
        public double EndTime { get => _endTime; set { _endTime = value; OnPropertyChanged(); } }

        // How this move's progress is remapped before interpolating start->end - see
        // CameraTransformMath.ApplyEasing. Defaults to Linear, the only curve this move ever had
        // before easing existed, so an already-saved effect keeps playing exactly as before.
        private CameraEasing _easing = CameraEasing.Linear;
        public CameraEasing Easing { get => _easing; set { _easing = value; OnPropertyChanged(); } }

        public CameraEffectModel Clone()
        {
            return new CameraEffectModel
            {
                StartFocusX = StartFocusX,
                StartFocusY = StartFocusY,
                StartZoom = StartZoom,
                EndFocusX = EndFocusX,
                EndFocusY = EndFocusY,
                EndZoom = EndZoom,
                StartTime = StartTime,
                EndTime = EndTime,
                Easing = Easing
            };
        }

        public Action<CameraEffectModel> EditEffect;
        public Action<CameraEffectModel> DeleteEffect;

        [JsonIgnore]
        public ICommand EditEffectCommand { get; set; }
        [JsonIgnore]
        public ICommand DeleteEffectCommand { get; set; }
    }
}
