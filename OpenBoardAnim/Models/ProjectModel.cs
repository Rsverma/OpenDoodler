using OpenBoardAnim.AppConstants;
using System;

namespace OpenBoardAnim.Models
{
    public class ProjectModel
    {
        public string Title { get; set; }
        public BoardStyle BoardStyle { get; set; }
        public Resolution Resolution { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime ModifiedOn { get; set; } = DateTime.Now;
        public TimeSpan Length { get; set; } = TimeSpan.Zero;
        public int Scenes { get; set; }
    }
}