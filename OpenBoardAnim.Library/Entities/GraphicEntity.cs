using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OpenBoardAnim.Library
{
    public class GraphicEntity
    {
        [Key]
        public int GraphicID { get; set; } 
        [Required]
        public string Name { get; set; }
        [Required]
        public string SVGText { get; set; }
    }
}
