using System.ComponentModel.DataAnnotations;

namespace OpenBoardAnim.Library
{
    public class SceneEntity
    {
        [Required]
        public int SceneId { get; set; }
        [Required]
        public string Name { get; set; }
        public List<int> GraphicIDs { get; } = new();
    }
}
