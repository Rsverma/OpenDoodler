using OpenBoardAnim.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenBoardAnim.Models
{
    public class ProjectDetails : ObservableObject
    {
        public ProjectDetails()
        {
            Scenes = new List<SceneModel> { new SceneModel
                {
                    Name="1",
                    Index = 1
                } ,new SceneModel
                {
                    Name="+",
                    Index = 2
                }
            };
            Settings = new ProjectSettings();
        }

        public string Title { get; set; } = "Untitled Project";
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string Path { get; set; }

        private ProjectSettings _settings;
        public ProjectSettings Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public List<SceneModel> Scenes { get; set; }

        // Transient - never persisted. Set just before opening the preview dialog (and cleared
        // again once it closes) to isolate playback to a single scene instead of the whole
        // project; null means "preview everything, from scene 1" as before. Lives directly on
        // the project object (rather than a separate wrapper) to avoid restructuring
        // ProjectPreviewView's DataContext, matching how other UI-only wiring already sits
        // directly on domain models in this app (e.g. SceneModel's Scene*Action delegates).
        [JsonIgnore]
        public int? PreviewSceneIndex { get; set; }

        private string _audioPath;
        public string AudioPath
        {
            get { return _audioPath; }
            set
            {
                _audioPath = value;
                OnPropertyChanged();
            }
        }

        private double _audioVolume = 100;
        public double AudioVolume
        {
            get { return _audioVolume; }
            set
            {
                _audioVolume = value;
                OnPropertyChanged();
            }
        }

        // Seconds into the background music file to start playback/looping from. Both trim
        // fields are plain seconds rather than nullable so they bind directly to a
        // NumericUpDown; 0 for the end is the sentinel for "no explicit end - play to the
        // file's natural end", since an actual end time of 0 (silence) is never a value
        // anyone would intentionally choose.
        private double _audioTrimStart;
        public double AudioTrimStart
        {
            get { return _audioTrimStart; }
            set
            {
                _audioTrimStart = value;
                OnPropertyChanged();
            }
        }

        private double _audioTrimEnd;
        public double AudioTrimEnd
        {
            get { return _audioTrimEnd; }
            set
            {
                _audioTrimEnd = value;
                OnPropertyChanged();
            }
        }

        public ProjectDetails Clone()
        {
            return new ProjectDetails
            {
                Title = Title,
                CreatedOn = CreatedOn,
                Path = Path,
                Settings = Settings == null ? null : new ProjectSettings
                {
                    BoardType = Settings.BoardType,
                    StrokeColorHex = Settings.StrokeColorHex,
                    StrokeWidth = Settings.StrokeWidth,
                    EntranceStyle = Settings.EntranceStyle,
                    SceneTransition = Settings.SceneTransition,
                    TransitionDurationSeconds = Settings.TransitionDurationSeconds,
                    AspectRatio = Settings.AspectRatio
                },
                Scenes = Scenes.Select(s => s.Clone()).ToList(),
                AudioPath = AudioPath,
                AudioVolume = AudioVolume,
                AudioTrimStart = AudioTrimStart,
                AudioTrimEnd = AudioTrimEnd
            };
        }
    }
    public enum BoardType
    {
        WhiteBoard,
        Blackboard,
        Greenboard
    }

    public enum EntranceStyle
    {
        HandDrawn,
        FadeIn,
        PopIn
    }

    public enum AspectRatioPreset
    {
        Widescreen16x9,
        Vertical9x16,
        Square1x1
    }

    public enum SceneTransition
    {
        None,
        Crossfade,
        Wipe
    }

    public class ProjectSettings :ObservableObject
    {
        private BoardType _boardType;
        public BoardType BoardType
        {
            get { return _boardType; }
            set
            {
                _boardType = value;
                OnPropertyChanged();
            }
        }

        private string _strokeColorHex = "#FF000000";
        public string StrokeColorHex
        {
            get { return _strokeColorHex; }
            set
            {
                _strokeColorHex = value;
                OnPropertyChanged();
            }
        }

        private double _strokeWidth = 2;
        public double StrokeWidth
        {
            get { return _strokeWidth; }
            set
            {
                _strokeWidth = value;
                OnPropertyChanged();
            }
        }

        private EntranceStyle _entranceStyle = EntranceStyle.HandDrawn;
        public EntranceStyle EntranceStyle
        {
            get { return _entranceStyle; }
            set
            {
                _entranceStyle = value;
                OnPropertyChanged();
            }
        }

        private SceneTransition _sceneTransition = SceneTransition.None;
        public SceneTransition SceneTransition
        {
            get { return _sceneTransition; }
            set
            {
                _sceneTransition = value;
                OnPropertyChanged();
            }
        }

        // How long the crossfade/wipe overlay animation runs for, in seconds - was hardcoded
        // to 0.6 in PreviewAndExportHandler.PlaySceneTransition; has no effect when
        // SceneTransition is None.
        private double _transitionDurationSeconds = 0.6;
        public double TransitionDurationSeconds
        {
            get { return _transitionDurationSeconds; }
            set
            {
                _transitionDurationSeconds = value;
                OnPropertyChanged();
            }
        }

        private AspectRatioPreset _aspectRatio = AspectRatioPreset.Widescreen16x9;
        public AspectRatioPreset AspectRatio
        {
            get { return _aspectRatio; }
            set
            {
                _aspectRatio = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditorWidth));
                OnPropertyChanged(nameof(EditorHeight));
                OnPropertyChanged(nameof(ExportWidth));
                OnPropertyChanged(nameof(ExportHeight));
            }
        }

        // Export is always exactly 2x the editor's coordinate space - the export canvas
        // reuses editor-authored X/Y positions as-is under a fixed 2x LayoutTransform
        // (see EditorActionsViewModel.ExportProject), so these must stay in that ratio.
        [JsonIgnore]
        public double ExportWidth => AspectRatio switch
        {
            AspectRatioPreset.Vertical9x16 => 1080,
            AspectRatioPreset.Square1x1 => 1080,
            _ => 1920
        };

        [JsonIgnore]
        public double ExportHeight => AspectRatio switch
        {
            AspectRatioPreset.Vertical9x16 => 1920,
            AspectRatioPreset.Square1x1 => 1080,
            _ => 1080
        };

        [JsonIgnore]
        public double EditorWidth => ExportWidth / 2;

        [JsonIgnore]
        public double EditorHeight => ExportHeight / 2;
    }
}
