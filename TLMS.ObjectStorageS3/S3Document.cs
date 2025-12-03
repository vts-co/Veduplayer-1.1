using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLMS.ObjectStorageS3
{
    public class S3Document
    {
        public MemoryStream InputStream { get; set; }
        public string Key { get; set; }
        public string BucketName { get; set; }
    }
}
