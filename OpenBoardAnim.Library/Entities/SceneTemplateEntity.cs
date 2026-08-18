using System.ComponentModel.DataAnnotations;

namespace OpenBoardAnim.Library
{
    public class SceneTemplateEntity
    {
        [Key]
        public int SceneTemplateID { get; set; }
        [Required]
        public string Name { get; set; }
        // Full serialized scene content (same JSON shape a project's own scenes are saved in),
        // so a template is self-contained and doesn't depend on any GraphicRepository rows
        // existing - it carries its own SVG/text content directly.
        [Required]
        public string SceneJson { get; set; }
        // Built-in templates ship with the app and can't be deleted from the gallery UI;
        // user-saved ones (via "Save as Template") can be.
        public bool IsBuiltIn { get; set; }
    }
}
