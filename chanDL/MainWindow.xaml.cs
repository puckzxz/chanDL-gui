using HtmlAgilityPack;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace chanDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (Properties.Settings.Default.DownloadPath != string.Empty)
            {
                tbDownloadPath.Text = Properties.Settings.Default.DownloadPath;
            }
            else
            {
                tbDownloadPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }
        }

        private async void btnStartDownload_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(tbThreadURL.Text) & Uri.IsWellFormedUriString(tbThreadURL.Text, UriKind.Absolute))
            {
                var thread = tbThreadURL.Text;
                var path = tbDownloadPath.Text;
                var skipExisting = cbSkipExistingImages.IsChecked.Value;
                lblImage.Dispatcher.Invoke(() => lblImage.Content = string.Empty);
                pbDownloadProgress.Dispatcher.Invoke(() => pbDownloadProgress.Value = 0);
                await Task.Run(() => DownloadThread(thread, path, skipExisting));
            }
            else
            {
                MessageBox.Show("You must enter a valid thread URL!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DownloadThread(string threadURL, string downloadPath, bool skipExistingImages)
        {
            var imageNodes = new HtmlWeb().Load(threadURL).DocumentNode.SelectNodes("//a[contains(@class, 'fileThumb')]");
            string threadID = threadURL.Split(new[] { "thread/" }, StringSplitOptions.None)[1];
            string board = threadURL.Split(new[] { "g/" }, StringSplitOptions.None)[1].Split('/')[0];
            string folderName = $"{board} - {threadID}";
            string downloadFolder = Path.Combine(downloadPath, folderName);
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            List<Uri> ImageURLs = imageNodes.Select(image => new Uri("https:" + image.Attributes["href"].Value)).ToList();

            if (skipExistingImages)
            {
                // Get the file name of each file in the download folder
                List<string> filesInDownloadFolder = Directory.GetFiles(downloadFolder).Select(Path.GetFileName).ToList();

                // Compare the links we have to the files we have downloaded to find out what images we need
                ImageURLs = ImageURLs.Where(x => !filesInDownloadFolder.Contains(Path.GetFileName(x.AbsolutePath))).ToList();
            }

            pbDownloadProgress.Dispatcher.Invoke(() => pbDownloadProgress.Maximum = ImageURLs.Count);

            // Download our images
            Parallel.ForEach(ImageURLs, image =>
            {
                using (WebClient wc = new WebClient())
                {
                    lblImage.Dispatcher.Invoke(() => lblImage.Content = $"Downloading {Path.GetFileName(image.LocalPath)}");
                    wc.DownloadFile(image.AbsoluteUri, Path.Combine(downloadFolder, Path.GetFileName(image.LocalPath)));
                    pbDownloadProgress.Dispatcher.Invoke(() => pbDownloadProgress.Value++);
                }
            });
            lblImage.Dispatcher.Invoke(() => lblImage.Content = $"Downloaded {ImageURLs.Count} images");
        }

        private void btnSetDownloadFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tbDownloadPath.Text = dialog.FileName;
                Properties.Settings.Default.DownloadPath = dialog.FileName;
                Properties.Settings.Default.Save();
            }
        }
    }
}