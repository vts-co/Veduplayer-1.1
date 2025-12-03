using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLMS.ObjectStorageS3
{
    public enum ServerTypes
    {
        AmazonS3 = 0,
        UploaderS3 = 1,
        Wasabi = 2,
        Cloudflare = 3
    }
}
