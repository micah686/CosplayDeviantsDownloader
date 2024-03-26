using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CosplayDeviantsDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Stopwatch sw = new Stopwatch();
        private readonly string _downloadDirectory = $@"{Directory.GetCurrentDirectory()}\Downloads\";
        private int totalArchives = 0;
        private int currentDownloaded = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(AlbumIdTextbox.Text, out int albumId))
            {
                if (albumId > 0 && !string.IsNullOrEmpty(CosplayDeviantCookie.Text))
                {
                    SetupAndCheck();
                }
                else
                {
                    MessageContent.Content = "Not a valid album ID or cookie";
                    Thread.Sleep(300);
                    MessageContent.Content = string.Empty;
                }
            }
            else
            {
                MessageContent.Content = "Not a valid album ID";
                Thread.Sleep(300);
                MessageContent.Content = string.Empty;
            }
            
        }

        private void SetupAndCheck()
        {
            string downloadPath = $@"{Directory.GetCurrentDirectory()}\Downloads\";
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory($@"{Directory.GetCurrentDirectory()}\Downloads\");
            }
            DirectoryInfo directoryInfo = new DirectoryInfo($@"{Directory.GetCurrentDirectory()}\Downloads\");
            //TODO: replace with regex
            int existingArchives = directoryInfo.GetFiles("CosplayDeviants-*.zip")
                .Where(file => file.Name.StartsWith("CosplayDeviants-")).Where(file => file.Name.EndsWith(".zip"))
                .Select(file => file.Name).Count();
             totalArchives = 2720 + (Convert.ToInt32(AlbumIdTextbox.Text) - 26604) - existingArchives;
            DownloadFiles();
        }

        private async void DownloadFiles()
        {
            CookieAwareWebClient client = new CookieAwareWebClient();
            client.DownloadProgressChanged += DownloadProgressChanged;
            client.DownloadFileCompleted += DownloadFileCompleted;
            client.CookieContainer.Add(new Cookie("CosplayDeviants", CosplayDeviantCookie.Text) { Domain = ".cosplaydeviants.com" }); //old method
            client.CookieContainer.Add(new Cookie("threads", CosplayDeviantCookie.Text) { Domain = ".cosplaydeviants.com" });
            sw.Start();
            try
            {
                LabelFilesDownloaded.Content = $"{currentDownloaded} / {totalArchives}";
                for (int albumId = Convert.ToInt32(AlbumIdTextbox.Text); albumId >= 26604; albumId--)
                {
                    if (File.Exists($@"{_downloadDirectory}CosplayDeviants-{albumId}.zip")) continue;
                    sw.Start();
                    LabelCurrentFile.Content = $@"{_downloadDirectory}CosplayDeviants-{albumId}.zip";
                    await client.DownloadFileTaskAsync(new Uri($"https://www.cosplaydeviants.com/downloadSet/{albumId}"), $@"{_downloadDirectory}CosplayDeviants-{albumId}.zip");
                }

                for (int albumId = 2720; albumId >= 1; albumId--)
                {
                    if (File.Exists($@"{_downloadDirectory}CosplayDeviants-{albumId}.zip")) continue;
                    sw.Start();
                    LabelCurrentFile.Content = $@"{_downloadDirectory}CosplayDeviants-{albumId}.zip";
                    await client.DownloadFileTaskAsync(new Uri($"https://www.cosplaydeviants.com/downloadSet/{albumId}"), $@"{_downloadDirectory}CosplayDeviants-{albumId}.zip");
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Console.WriteLine(e.Message);
                MessageContent.Content = e.Message;
                Thread.Sleep(300);
                MessageContent.Content = string.Empty;                                
                //throw;
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            LabelSpeed.Content = $"{e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds:0.00} kb/s";
            LabelDownloaded.Content = $"{e.BytesReceived / 1024d:0.00} Kb";
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageContent.Content = "The download has been cancelled";
                Thread.Sleep(300);
                MessageContent.Content = string.Empty;
                return;
            }

            if (e.Error != null) // We have an error! Retry a few times, then abort.
            {
                MessageContent.Content = "An error ocurred while trying to download file";
                Thread.Sleep(300);
                MessageContent.Content = string.Empty;
                return;
            }

            MessageContent.Content = "File succesfully downloaded";
            sw.Reset();
            currentDownloaded++;
            LabelFilesDownloaded.Content = $"{currentDownloaded} / {totalArchives}";
            Thread.Sleep(300);
            MessageContent.Content = string.Empty;

        }
    }

    public class CookieAwareWebClient : WebClient
    {
        public CookieContainer CookieContainer { get; set; }
        public Uri Uri { get; set; }

        public CookieAwareWebClient()
            : this(new CookieContainer())
        {
        }

        public CookieAwareWebClient(CookieContainer cookies)
        {
            this.CookieContainer = cookies;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).CookieContainer = this.CookieContainer;
            }
            HttpWebRequest httpRequest = (HttpWebRequest)request;
            httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return httpRequest;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            String setCookieHeader = response.Headers[HttpResponseHeader.SetCookie];

            if (setCookieHeader != null)
            {
                //do something if needed to parse out the cookie.
                if (setCookieHeader != null)
                {
                    Cookie cookie = new Cookie(); //create cookie
                    this.CookieContainer.Add(cookie);
                }
            }
            return response;
        }
    }
}
