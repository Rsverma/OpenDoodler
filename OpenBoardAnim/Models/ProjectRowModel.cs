using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBoardAnim.Models
{
    public class ProjectRowModel
    {
        public string Title { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public TimeSpan Length { get; set; }
        public int Scenes { get; set; }
    }
}
