using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace VtsVideoPlayer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 


        [STAThread]
        static void Main(string[] args)
        {
           

        Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string baseUrl, lessonId, type , Execution , Student,  NumOfMinutes,  IsVisitor;


            string arg = args[0];

            // ✅ إزالة prefix vtsplayer:
            if (arg.StartsWith("vtsplayer:", StringComparison.OrdinalIgnoreCase))
            {
                arg = arg.Substring("vtsplayer:".Length);
            }

            // ✅ لو جاي URL Encoded (مثلاً vtsplayer:http://xx|id|Youtube يبقى http://xx%7Cid%7CYoutube)
            arg = HttpUtility.UrlDecode(arg);

            // ✅ لو في Double Quotes
            arg = arg.Trim('"');

          

            var parts = arg.Split('|');
            

            if (parts.Length == 7)
            {
                baseUrl = parts[0];
                Execution = parts[1];
                lessonId = parts[2];
                NumOfMinutes = parts[3];
                Student = parts[5];
                IsVisitor = parts[4];
                type = parts[6];
               
            }
            else
            {
                MessageBox.Show("❌ صيغة الباراميترات غير صحيحة.\n\nالوارد: " + arg +
                                "\n\n✅ الصيغة الصحيحة:\n" +
                                "vtsplayer:BaseUrl|LessonId|Type");
                return;
            }

            bool isVisitorBool = IsVisitor.Equals("true", StringComparison.OrdinalIgnoreCase);
            string apiUrl = $"{baseUrl}/LessonURL/GetLessonPlayerUrl?Lessonid={lessonId}";
         
            try
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        HttpResponseMessage response =  client.GetAsync(apiUrl).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            string finalUrl = response.Content.ReadAsStringAsync().Result.Trim();


                            if (type.Equals("Youtube", StringComparison.OrdinalIgnoreCase))
                            {
                                Application.Run(new frmshowyoutube(finalUrl, Execution, Student, NumOfMinutes, isVisitorBool, lessonId, baseUrl));
                            }
                            else
                            {
                                Application.Run(new frmvideoplayer(finalUrl, "",Execution, Student, NumOfMinutes, isVisitorBool, lessonId,baseUrl, "", 0));
                            }
                        }
                        else
                        {
                            // ❌ هنا هتظهر رسالة مفصلة عن الخطأ
                            string errorContent =  response.Content.ReadAsStringAsync().Result;
                            MessageBox.Show("❌ فشل الاتصال بالـ API\n\n" +
                                            "🔗 URL: " + apiUrl + "\n" +
                                            "📌 Status Code: " + (int)response.StatusCode + " - " + response.StatusCode + "\n" +
                                            "📩 Response Body: " + errorContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("⚠️ خطأ أثناء محاولة الاتصال بالـ API:\n" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠️ خطأ أثناء الاتصال: " + ex.Message);
            }
        }




        //// 




        //[STAThread]
        //static void Main(string[] args)
        //{
        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);

        //    if (args.Length > 0)
        //    {
        //        string fullArg = args[0];

        //        MessageBox.Show("Received raw argument: " + fullArg);

        //        if (fullArg.StartsWith("vtsplayer:", StringComparison.OrdinalIgnoreCase))
        //            fullArg = fullArg.Substring("vtsplayer:".Length);

        //        fullArg = Uri.UnescapeDataString(fullArg);

        //        MessageBox.Show("Processed argument: " + fullArg);

        //        string[] parts = fullArg.Split('|');

        //        if (parts.Length >= 4)
        //        {
        //            string content = parts[0];
        //            string bucket = parts[1];
        //            string foldername = parts[2];
        //            string servertype = parts[3];

        //            MessageBox.Show($"Launching player with:\nContent: {content}\nBucket: {bucket}\nFolder: {foldername}\nServerType: {servertype}");

        //            Application.Run(new frmvideoplayer(content, bucket, foldername, Convert.ToInt32(servertype)));

        //        }
        //        else
        //        {
        //            MessageBox.Show("Argument parts count less than 4");
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show("No arguments received");
        //    }

        //    // إذا لم يدخل شرط فتح الفيديو، هنا تقدر تفتح الفورم الافتراضي أو تغلق البرنامج
        //    // Application.Run(new frmvideoplayer());
        //}


        //[STAThread]
        //static void Main(string[] args)
        //{

        //    string appName = "VeduPlayerApp2";
        //    string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);

        //    if (!Directory.Exists(targetDir))
        //        Directory.CreateDirectory(targetDir);

        //    string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        //    foreach (string resource in resources)
        //    {
        //        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
        //        {
        //            string fileName = resource.Replace("InstallerNamespace.Resources.", ""); // Update with your actual namespace!
        //            string outputPath = Path.Combine(targetDir, fileName);

        //            using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        //            {
        //                stream.CopyTo(fileStream);
        //            }
        //        }
        //    }


        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    if (args.Length > 0)
        //    {
        //        string fullArg = args[0];

        //        // إزالة البروتوكول
        //        if (fullArg.StartsWith("vtsplayer:", StringComparison.OrdinalIgnoreCase))
        //            fullArg = fullArg.Substring("vtsplayer:".Length);

        //        // فك ترميز الرابط
        //        fullArg = Uri.UnescapeDataString(fullArg);  // هذه هي الإضافة المهمة

        //        string[] parts = fullArg.Split('|');

        //        if (parts.Length >= 4)
        //        {
        //            string content = parts[0];
        //            string bucket = parts[1];
        //            string foldername = parts[2].Trim();
        //            string servertype = parts[3];
        //            Application.Run(new frmvideoplayer(content, bucket, foldername, Convert.ToInt32(servertype)));
        //            return;
        //        }
        //        else
        //        {
        //            string content = parts[0];
        //            Application.Run(new frmshowyoutube(content));
        //            return;
        //        }
        //    }



        //    //string content = args[0];
        //    //string bucket = args[1];

        //    //string foldername = args[2];

        //    //string servertype = args[3];

        //    //    frmvideoplayer playerForm = new frmvideoplayer(content, bucket, foldername, Convert.ToInt32(servertype));
        //    //    Application.Run(playerForm);


        //    //frmvideoplayer playerForm = new frmvideoplayer(content, bucket, foldername, Convert.ToInt32(servertype));
        //    //MessageBox.Show("222جاري التحميل");
        //    // Application.Run(playerForm);
        //    // MessageBox.Show("تم فتح البرنامج");
        //    // Application.Run(new frmvideoplayer());
        //}
    }
}
