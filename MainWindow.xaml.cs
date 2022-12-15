using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
//using Forms =System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Color = System.Windows.Media.Color;

namespace OpenImage
{
    public partial class MainWindow : System.Windows.Window
    {
        private string _SourceFolderPath;
        private string _SaveFolderPath;
        private double _PicWidth;
        private double _PicHeight;
        private bool? _FillCanvas;
        BackgroundWorker bgWorker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SourceFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog sourceFolder = new System.Windows.Forms.FolderBrowserDialog();
            sourceFolder.ShowNewFolderButton = true;
            var result = sourceFolder.ShowDialog();
           if (result == System.Windows.Forms.DialogResult.OK)
            {
                SourceFolderPath.Text = sourceFolder.SelectedPath;
            }
        }

        private void RestPicWidthHeight_Click(object sender, RoutedEventArgs e)
        {
            PicWidth.Value = 512;
            PicHeight.Value = 512;
        }

        private void SaveFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog saveFolder = new System.Windows.Forms.FolderBrowserDialog();
            saveFolder.ShowNewFolderButton = true;
            var result = saveFolder.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK)
            {
                SaveFolderPath.Text = saveFolder.SelectedPath;
            }
        }

        private void PicMultiConvert_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.Value = 0;

            
            if (!Directory.Exists(SourceFolderPath.Text))
            {
                System.Windows.MessageBox.Show("Source folder does not exist！");
                return;
            }
            if (!Directory.Exists(SaveFolderPath.Text))
            {
                System.Windows.MessageBox.Show("Target folder does not exist！");
                return;
            }

            if(SourceFolderPath.Text == SaveFolderPath.Text)
            {
                System.Windows.MessageBox.Show("The source folder and target folder cannot be the same！");
                return;
            }

            
            this._SourceFolderPath = SourceFolderPath.Text;
            this._SaveFolderPath = SaveFolderPath.Text;
            this._PicWidth = PicWidth.Value;
            this._PicHeight = PicHeight.Value;
            this._FillCanvas = FillCanvas.IsChecked;

            this.bgWorker = new BackgroundWorker();
            bgWorker.WorkerReportsProgress = true;
            bgWorker.WorkerSupportsCancellation = true;
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.ProgressChanged += BgWorker_ProgressChanged;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
            if (!bgWorker.IsBusy)
            {
                PicMultiConvert.Background = new SolidColorBrush(Color.FromRgb(218, 51, 64));
                PicMultiConvert.Content = "Stop Conversion";
                bgWorker.RunWorkerAsync();
            }
            else
            {
                bgWorker.CancelAsync();
                bgWorker.Dispose();
                PicMultiConvert.Background = new SolidColorBrush(Color.FromRgb(45, 184, 77));
                PicMultiConvert.Content = "Start Conversion";
            }
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
            if (ProgressBar.Value == 100)
            {
                PicMultiConvert.Background = new SolidColorBrush(Color.FromRgb(45, 184, 77));
                PicMultiConvert.Content = "Start Conversion";
                System.Windows.MessageBox.Show("Done");
            }
        }

        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            DirectoryInfo TheFolder = new DirectoryInfo(this._SourceFolderPath);
            Collection<string> ImgsList = new Collection<string>();
            Console.WriteLine(TheFolder.GetFiles());
            foreach (FileInfo NextFile in TheFolder.GetFiles()){
                Console.WriteLine(NextFile.Extension);
                var ext = NextFile.Extension.ToLower();
                if(ext == ".png" || ext == ".jpg")
                {
                    ImgsList.Add(NextFile.FullName);
                }
            }
            if(ImgsList.Count == 0)
            {
                System.Windows.MessageBox.Show("Can not find any images with extension (png|jpg)");
                return;
            }
            for(int i = 0; i < ImgsList.Count; i++)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                else
                {
                    FileStream fs = new FileStream(ImgsList[i], FileMode.Open, FileAccess.Read); 
                    BinaryReader br = new BinaryReader(fs);
                    byte[] buffer = br.ReadBytes((int)fs.Length); 

                    MemoryStream ms = new MemoryStream(buffer);
                    System.Drawing.Image image = System.Drawing.Image.FromStream(ms);

                    Bitmap srcImg = new Bitmap(image);
                    var sWidth = srcImg.Width;
                    var sHeight = srcImg.Height;
                    
                    Bitmap bmNew = new Bitmap((int)this._PicWidth, (int)this._PicHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(bmNew))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        if (this._FillCanvas == true)
                        {
                            g.DrawImage(srcImg, 0, 0, (int)this._PicWidth, (int)this._PicHeight);
                        }
                        else
                        {
                            var sAspectRatio = (double)sWidth / (double)sHeight;
                            var pAspectRatio = this._PicWidth / this._PicHeight;
                            if(sAspectRatio == pAspectRatio)
                            {
                                g.DrawImage(srcImg, 0, 0, (int)this._PicWidth, (int)this._PicHeight);
                            }
                            if (sAspectRatio > pAspectRatio)
                            {
                                var dWidth = (int)this._PicWidth;
                                var dHeightDouble = (double)sHeight * ((double)dWidth / sWidth);
                                var marginYDouble = (this._PicHeight - dHeightDouble) /2;

                                var dHeight = Convert.ToInt32(Math.Round(dHeightDouble,2));
                                var marginY = Convert.ToInt32(Math.Round(marginYDouble,2));

                                g.DrawImage(srcImg, 0, marginY, dWidth, dHeight);
                            }
                            else
                            {
                                var dHeight = (int)this._PicHeight;
                                var dWidthDouble = (double)sWidth * ((double)dHeight / sHeight);
                                var marginXDouble = (this._PicWidth - dWidthDouble) /2;

                                var dWidth = Convert.ToInt32(Math.Round(dWidthDouble,2));
                                var marginX = Convert.ToInt32(Math.Round(marginXDouble,2));

                                g.DrawImage(srcImg, marginX, 0, dWidth, dHeight);
                            }

                        }
                        
                        bmNew.Save(this._SaveFolderPath + @"\"+i+".png");
                        bmNew.Dispose();
                        g.Dispose();
                        ms.Dispose();
                        fs.Dispose();
                    }

                    var percent = (double)(i + 1) / (double)ImgsList.Count;
                    worker.ReportProgress(Convert.ToInt32(Math.Round(percent,2)*100));
                    Thread.Sleep(100);
                }
            }
        }
    }
}
