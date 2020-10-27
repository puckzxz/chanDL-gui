using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Windows.Controls;

namespace chanDL
{
    public partial class MainWindow : Window
    {
        private static readonly string BASE_API_URL = "https://a.4cdn.org";
        private static readonly string BASE_IMAGE_URL = "https://i.4cdn.org";
        private readonly static HttpClient client = new HttpClient();
        private static string board = "";
        private static string threadID = "";
        private static string thread = "";

        public MainWindow()
        {
            InitializeComponent();
            if (Properties.Settings.Default.DownloadPath != string.Empty)
            {
                lblDownloadPath.Content = Properties.Settings.Default.DownloadPath;
            }
            else
            {
                lblDownloadPath.Content = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                Properties.Settings.Default.DownloadPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                Properties.Settings.Default.Save();
            }
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (board == string.Empty | thread == string.Empty)
            {
                return;
            }
            pbDownloadProgress.Dispatcher.Invoke(() => pbDownloadProgress.Value = 0);
            Task.Run(() => DownloadThread());
        }

        private async void DownloadThread()
        {
            var downloadFolder = Path.Combine(Properties.Settings.Default.DownloadPath, $"{board}-{threadID}");

            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            var resp = await client.GetAsync($"{BASE_API_URL}/{board}/thread/{threadID}.json");

            if (!resp.IsSuccessStatusCode)
            {
                MessageBox.Show(
                    $"Failed to fetch data from 4chan thread\nStatus: {resp.StatusCode}\nError: {resp.ReasonPhrase}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var thread = JsonSerializer.Deserialize<Thread>(await resp.Content.ReadAsStringAsync());

            var posts = thread.Posts.Where(p => p.Filename != 0);

            var skipExisting = cbSkipExistingImages.Dispatcher.Invoke(() => cbSkipExistingImages.IsChecked.Value);

            if (skipExisting)
            {
                var existingFiles = Directory.GetFiles(downloadFolder).Select(Path.GetFileName).ToList();
                posts = posts.Where(p => !existingFiles.Contains(Path.GetFileName($"{p.Filename}{p.Extenstion}"))).ToList();
            }

            pbDownloadProgress.Dispatcher.Invoke(() => pbDownloadProgress.Maximum = posts.Count());

            var downloadedImages = 0;

            lblDownloadAmount.Dispatcher.Invoke(() => lblDownloadAmount.Content = $"Downloading {downloadedImages} of {posts.Count()}");

            Parallel.ForEach(posts, post =>
            {
                using var wc = new WebClient();
                wc.DownloadFile($"{BASE_IMAGE_URL}/{board}/{post.Filename}{post.Extenstion}", Path.Combine(downloadFolder, $"{post.Filename}{post.Extenstion}"));
                downloadedImages++;
                pbDownloadProgress.Dispatcher.Invoke(() => pbDownloadProgress.Value++);
                lblDownloadAmount.Dispatcher.Invoke(() => lblDownloadAmount.Content = $"Downloading {downloadedImages} of {posts.Count()}");
            });

            lblDownloadAmount.Dispatcher.Invoke(() => lblDownloadAmount.Content = $"Downloaded {posts.Count()} images");
        }

        private void btnSetDownloadPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = Properties.Settings.Default.DownloadPath
            };
            var res = dialog.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                lblDownloadPath.Content = dialog.SelectedPath;
                Properties.Settings.Default.DownloadPath = dialog.SelectedPath;
                Properties.Settings.Default.Save();
            }
        }

        private void tbThreadURL_TextChanged(object sender, TextChangedEventArgs e)
        {
            thread = tbThreadURL.Text;
            if (thread.Split(".org/").Length < 2 | thread.Split("thread/").Length < 2)
            {
                lblDownloadPath.Content = $"{Properties.Settings.Default.DownloadPath}";
                return;
            }
            board = thread.Split(".org/")[1].Split("/")[0];
            threadID = thread.Split("thread/")[1];
            if (threadID.EndsWith("/"))
            {
                threadID = threadID.Split("/")[0];
            }
            lblDownloadPath.Content = $"{Properties.Settings.Default.DownloadPath}\\{board}-{threadID}";
        }
    }
}
