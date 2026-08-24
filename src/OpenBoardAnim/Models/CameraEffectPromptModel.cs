using OpenBoardAnim.Core;
using System;
using System.Windows.Media;

namespace OpenBoardAnim.Models
{
    // Backing model for the "Camera Effect" pop-up (add or edit one CameraEffectModel).
    // Effect is either a brand-new blank effect (not yet in any scene's CameraEffects list) or
    // a clone of an existing one being edited - SaveEffect is only invoked on explicit confirm,
    // so cancelling/closing the dialog discards whatever was typed instead of committing it.
    public class CameraEffectPromptModel : ObservableObject
    {
        public CameraEffectModel Effect { get; set; }

        public Action<CameraEffectModel> SaveEffect;

        // A static render of the target scene's final-state graphics (see
        // PreviewAndExportHandler.RenderSceneSnapshot), shown as the background the Start/End
        // rectangles are drawn over, plus the editor's own coordinate space they're drawn in -
        // both needed by the view to convert between screen pixels and editor-space units.
        public ImageSource SceneSnapshot { get; set; }
        public double EditorWidth { get; set; }
        public double EditorHeight { get; set; }
    }
}
