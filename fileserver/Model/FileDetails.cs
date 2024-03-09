using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileserver.Model
{
    public class FileDetails
    {
        public string FileName { get; set; }
        public string NameOfTheFile { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public string UserId { get; set; }
        public int CreatedDate { get; set; }
        public int LastModifiedDate { get; set; }
        public string ObjectURL { get; set; }
        public bool Trash { get; set; }
        public bool Stared { get; set; }
    }
}
