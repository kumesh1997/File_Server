using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileserver.DTO
{
    public class SaveFileDTO
    {
        public string FileName { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public string UserId { get; set; }
        public string ObjectURL { get; set; }
       
    }
}
