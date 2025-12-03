using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLMS.ObjectStorageS3
{
    public class S3Service
    {
        private readonly string AccessKey;
        private readonly string SecretKey;
        private readonly string ServiceURL;
        private readonly string BucketName;
        private readonly string BucketFolderName;
        private readonly int ServerType;
        private readonly string AmazonSecretKey;
        private readonly string AmazonAccessKey;
        private readonly Amazon.RegionEndpoint AmazonRegion;

        public S3Service(string Bucket, string FolderName, int serverType)
        {
    


            ServerType = serverType;
            BucketName = Bucket;
            BucketFolderName = FolderName;
        }

        public AmazonS3Client GetClient()
        {
            try
            {
                if (ServerType == (int)ServerTypes.AmazonS3)
                {
                    return new AmazonS3Client(AmazonAccessKey, AmazonSecretKey, AmazonRegion);


                }

                else
                {


                    var credentials = new BasicAWSCredentials(AccessKey, SecretKey);
                    if (ServerType == (int)ServerTypes.Cloudflare)
                    {
                        var config = new AmazonS3Config()
                        {
                            RegionEndpoint = AmazonRegion,
                            AuthenticationRegion = "auto",
                            ServiceURL = ServiceURL,
                            ForcePathStyle = true,
                            SignatureVersion = "4",
                            UseHttp = false

                        };
                        return new AmazonS3Client(credentials, config);

                    }
                    else
                    {

                        var config = new AmazonS3Config()
                        {
                            RegionEndpoint = AmazonRegion,
                            ServiceURL = ServiceURL,
                            ForcePathStyle = true,


                        };
                        return new AmazonS3Client(credentials, config);
                    }
                }


            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error creating bucket: '{ex.Message}'");
                return new AmazonS3Client();

            }

        }

        #region create bucket
        public async Task<bool> CreateBucketAsync(IAmazonS3 client, string bucketName)
        {

            try
            {
                var request = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true,
                };
                var response = await client.PutBucketAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error creating bucket: '{ex.Message}'");
                return false;
            }
        }
        public async Task<bool> CreateFolderAsync(IAmazonS3 client, string bucketName, string folderName)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = folderName.EndsWith("/") ? folderName : folderName + "/",
                    ContentBody = string.Empty // Empty object
                };

                var response = await client.PutObjectAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error creating folder: '{ex.Message}'");
                return false;
            }
        }
        public async Task<bool> CreateBucketIfNotExists(string bucketName, string BucketFileName = null)
        {
            var client = GetClient();
            var listBuckets = client.ListBuckets();
            //var li = listBuckets.Buckets.Where(x => x.BucketName == "shabannn").FirstOrDefault();
            if (!listBuckets.Buckets.Any(x => x.BucketName == bucketName))
            {

                await CreateBucketAsync(client, bucketName);

            }

            if (!string.IsNullOrEmpty(BucketFileName))
            {
                bool folderExists = FolderExists(client, bucketName, BucketFileName);
                if (!folderExists)
                    CreateFolder(client, bucketName, BucketFileName);
            }
            return true;
        }

        public bool FolderExists(IAmazonS3 client, string bucketName, string folderName)
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = folderName.EndsWith("/") ? folderName : folderName + "/",
                    MaxKeys = 1 // Check if at least one object exists
                };

                var response = client.ListObjectsV2Async(request).GetAwaiter().GetResult(); // Synchronously wait for result

                return response.S3Objects.Any(); // Folder exists if any object has the prefix
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error checking folder: {ex.Message}");
                return false;
            }
        }


        public bool CreateFolder(IAmazonS3 client, string bucketName, string folderName)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = folderName.EndsWith("/") ? folderName : folderName + "/",
                    ContentBody = string.Empty // Empty object to represent a folder
                };

                var response = client.PutObjectAsync(request).GetAwaiter().GetResult(); // Synchronously wait for result

                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error creating folder: '{ex.Message}'");
                return false;
            }
        }


        #endregion

        #region Upload file
        public async Task<bool> UploadFiletoS3BucketAsync(S3Document s3Document)
        {

            try
            {
                var client = GetClient();

                //var uploadRequest = new TransferUtilityUploadRequest()
                //{
                //    InputStream = s3Document.InputStream,
                //    Key = s3Document.Key,
                //    BucketName = BucketName,
                //    CannedACL = S3CannedACL.PublicReadWrite,
                //};
                //var fileTransferUtility = new TransferUtility(client);
                //await fileTransferUtility.UploadAsync(uploadRequest);

                var fileTransferUtility = new TransferUtility(client);

                var uploadRequest = new TransferUtilityUploadRequest()
                {
                    InputStream = s3Document.InputStream,
                    Key = s3Document.Key,
                    BucketName = BucketName,
                    CannedACL = S3CannedACL.PublicReadWrite,
                };
                uploadRequest.UploadProgressEvent +=
                    new EventHandler<UploadProgressArgs>(
                        UploadRequest_UploadPartProgressEvent);

                await fileTransferUtility.UploadAsync(uploadRequest);
                Console.WriteLine("Upload completed");
                //await fileTransferUtility.UploadAsync(uploadRequest);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                return false;
            }

            return true;
        }
        public static void UploadRequest_UploadPartProgressEvent(object sender, UploadProgressArgs e)
        {
            // Process event.
            //int pctProgress = (int)(e.TransferredBytes * 100 / e.TotalBytes);
            //progressBarUpload.Value = pctProgress;
            //progressBarUpload.Invalidate();
            Console.WriteLine($"{e.TransferredBytes}/{e.TotalBytes}");
        }
        /// <summary>
        /// Shows how to upload a file from the local computer to an Amazon S3
        /// bucket.
        /// </summary>
        /// <param name="client">An initialized Amazon S3 client object.</param>
        /// <param name="bucketName">The Amazon S3 bucket to which the object
        /// will be uploaded.</param>
        /// <param name="objectName">The object to upload.</param>
        /// <param name="filePath">The path, including file name, of the object
        /// on the local computer to upload.</param>
        /// <returns>A boolean value indicating the success or failure of the
        /// upload procedure.</returns>
        public async Task<bool> UploadFileAsync(string objectName, string filePath, string folderName = null)
        {
            var client = GetClient();
            var bucketName = BucketName;

            string objectKey = objectName;
            if (!string.IsNullOrEmpty(folderName))
                objectKey = (folderName.EndsWith("/") ? folderName : folderName + "/") + objectName;


            if (ServerType == (int)ServerTypes.Cloudflare)
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    FilePath = filePath,
                    ContentType = "application/octet-stream", // Adjust based on file type
                    UseChunkEncoding = false
                };
                var response = await client.PutObjectAsync(request);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"Successfully uploaded {objectName} to {bucketName}.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Could not upload {objectName} to {bucketName}.");
                    return false;
                }

            }
            else
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    FilePath = filePath,
                };
                var response = await client.PutObjectAsync(request);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine($"Successfully uploaded {objectName} to {bucketName}.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Could not upload {objectName} to {bucketName}.");
                    return false;
                }
            }




        }

        #endregion

        #region Generate Presigned URL(temp)
        /// <summary>
        /// Generate a presigned URL that can be used to access the file named
        /// in the objectKey parameter for the amount of time specified in the
        /// duration parameter.
        /// </summary>
        /// <param name="client">An initialized S3 client object used to call
        /// the GetPresignedUrl method.</param>
        /// <param name="bucketName">The name of the S3 bucket containing the
        /// object for which to create the presigned URL.</param>
        /// <param name="objectKey">The name of the object to access with the
        /// presigned URL.</param>
        /// <param name="duration">The length of time for which the presigned
        /// URL will be valid.</param>
        /// <returns>A string representing the generated presigned URL.</returns>
        public string GeneratePresignedURL(string objectKey, double durationHour)
        {
            string urlString = string.Empty;
            var client = GetClient();
            var bucketName = BucketName;
            var expireDate = DateTime.UtcNow.AddHours(durationHour);
            if (!string.IsNullOrEmpty(BucketFolderName))
                objectKey = (BucketFolderName.EndsWith("/") ? BucketFolderName : BucketFolderName + "/") + objectKey;

            string ContentData = "";
            ContentData = "video/mp4";





            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    Expires = expireDate,

                    // Fix: Ensure video plays instead of downloading
                    ResponseHeaderOverrides = new ResponseHeaderOverrides
                    {

                        ContentType = ContentData,   // Ensures correct MIME type

                        ContentDisposition = "inline"  // Forces browser to play instead of downloading
                    }
                };


                urlString = client.GetPreSignedURL(request);
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error:'{ex.Message}'");
            }

            return urlString;
        }
        //public string GeneratePresignedURLR(string objectKey, double durationHour, AmazonS3Client client)
        //{
        //    string urlString = string.Empty;
        //    var bucketName = BucketName;
        //    var expireDate = DateTime.UtcNow.AddHours(durationHour);
        //    if (!string.IsNullOrEmpty(BucketFolderName))
        //        objectKey = (BucketFolderName.EndsWith("/") ? BucketFolderName : BucketFolderName + "/") + objectKey;


        //    try
        //    {

        //        var request = new GetPreSignedUrlRequest()
        //        {
        //            BucketName = bucketName,
        //            Key = objectKey,
        //            Expires = expireDate,
        //        };
        //        urlString = client.GetPreSignedURL(request);
        //    }
        //    catch (AmazonS3Exception ex)
        //    {
        //        Console.WriteLine($"Error:'{ex.Message}'");
        //    }

        //    return urlString;
        //}


        #endregion

        #region List of buckets
        public void ListOfBuckets()
        {
            var client = GetClient();
            var bucketName = "shaban-1";

            var dir = new S3DirectoryInfo(client, bucketName);
            var list = dir.GetFileSystemInfos().ToList();
            double allSizeFiles = 0;
            foreach (IS3FileSystemInfo file in dir.GetFileSystemInfos())
            {
                var fileKey = file.Name;
                allSizeFiles += GetSize(client, fileKey);
                Console.WriteLine(file.Name);
                Console.WriteLine(file.Extension);
                Console.WriteLine(file.LastWriteTime);
            }
            var totalSize = Math.Round((allSizeFiles / 1024) / 1024, 2, MidpointRounding.AwayFromZero);

            var listBuckets = client.ListBuckets();
        }

        public double GetSize(AmazonS3Client client, string fileKey)
        {
            var getObjectMetadataRequest = new GetObjectMetadataRequest() { BucketName = BucketName, Key = fileKey };
            var meta = client.GetObjectMetadata(getObjectMetadataRequest);
            return meta.Headers.ContentLength;
        }
        public List<string> GetBucketFilesName()
        {
            var client = GetClient();
            var dir = new S3DirectoryInfo(client, BucketName);
            var list = dir.GetFileSystemInfos().ToList();
            List<string> fileKeys = new List<string>();
            foreach (IS3FileSystemInfo file in dir.GetFileSystemInfos())
            {
                fileKeys.Add(file.Name);
            }
            return fileKeys;
        }
        public bool CheckFileUploaded(string FileName)
        {
            var client = GetClient();
            var dir = new S3DirectoryInfo(client, BucketName);
            var list = dir.GetFileSystemInfos().ToList();
            List<string> fileKeys = new List<string>();
            foreach (IS3FileSystemInfo file in dir.GetFileSystemInfos())
            {
                fileKeys.Add(file.Name);
            }
            if (fileKeys.Contains(FileName))
                return true;
            return false;
        }
        public bool IsFileUploaded(string fileName)
        {
            try
            {
                var client = GetClient();
                var objectKey = fileName;
                if (!string.IsNullOrEmpty(BucketFolderName))
                    objectKey = (BucketFolderName.EndsWith("/") ? BucketFolderName : BucketFolderName + "/") + objectKey;

                var request = new ListObjectsV2Request
                {
                    BucketName = BucketName,
                    Prefix = objectKey,
                    MaxKeys = 1 // Only check for one file
                };

                var response = client.ListObjectsV2Async(request).GetAwaiter().GetResult();

                return response.S3Objects.Any(); // File exists if it's listed
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error checking file: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Get Ojet
        public void GetObject()
        {
            try
            {
                AmazonS3Client client = GetClient();
                GetObjectRequest request = new GetObjectRequest()
                {
                    BucketName = BucketName,
                    Key = "1"// because we pass 1 as unique key while save 
                             //data at the s3 bucket
                };

                using (GetObjectResponse response = client.GetObject(request))
                {
                    StreamReader reader = new
                 StreamReader(response.ResponseStream);
                    var vccEncryptedData = reader.ReadToEnd();
                }
            }
            catch (AmazonS3Exception)
            {
                throw;
            }
        }
        public string GeneratePresignedURLR(string objectKey, double durationHour, AmazonS3Client client)
        {
            string urlString = string.Empty;
            var bucketName = BucketName;
            var expireDate = DateTime.UtcNow.AddHours(durationHour);

            if (!string.IsNullOrEmpty(BucketFolderName))
                objectKey = (BucketFolderName.TrimEnd('/') + "/") + objectKey;
            string ContentData = "";
            ContentData = "video/mp4";

            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    Expires = expireDate,

                    // Fix: Ensure video plays instead of downloading
                    ResponseHeaderOverrides = new ResponseHeaderOverrides
                    {
                        ContentType = ContentData,   // Ensures correct MIME type
                        ContentDisposition = "inline"  // Forces browser to play instead of downloading
                    }
                };

                urlString = client.GetPreSignedURL(request);
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error generating pre-signed URL: '{ex.Message}'");
            }

            return urlString;
        }

        #endregion
    }
}
