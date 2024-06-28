using System.ComponentModel.DataAnnotations;

namespace OpenBoardAnim.Library
{
    public class ProjectEntity
    {
        [Key]
        public int ProjectID { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string FilePath { get; set; }
        [Required]
        public DateTime CreatedOn { get; set; }
        [Required]
        public DateTime LatestLaunchTime { get; set; }
        [Required]
        public int SceneCount { get; set; }
    }
}
