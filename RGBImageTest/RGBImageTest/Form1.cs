﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RGBImageTest
{
    /// <summary>
    /// 色処理判別用
    /// </summary>
    public enum ColorEnum {
        Red,
        Green,
        Blue
    }

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // イベントの登録
            this.button_fileDialog.Click += Button_fileDialog_Click;

            // チャートの初期化
            this.ChartInit(this.chart_R);
            this.ChartInit(this.chart_G);
            this.ChartInit(this.chart_B);
        }

        /// <summary>
        /// ファイルダイアログボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_fileDialog_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog()
                {
                    Title = "画像ファイルを選択してください",
                    FileName = "Image Selection",
                    Filter = "画像ファイル(*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp",
                    ValidateNames = false,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
                })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // 画像パス生成
                    var imagePath = Path.GetFullPath(ofd.FileName);
                    // テキストボックスに貼り付け
                    this.textBox_filePath.Text = imagePath;

                    // RBG画像生成
                    Bitmap src = new Bitmap(imagePath);

                    // 画像貼り付け
                    this.pictureBox_src.Image = src;
                    this.pictureBox_R.Image = this.GetMonoColorImage(src, ColorEnum.Red);
                    this.pictureBox_G.Image = this.GetMonoColorImage(src, ColorEnum.Green);
                    this.pictureBox_B.Image = this.GetMonoColorImage(src, ColorEnum.Blue);

                    // ヒストグラムデータ生成
                    var r_hist = this.GetHistogram(src, ColorEnum.Red);
                    var g_hist = this.GetHistogram(src, ColorEnum.Green);
                    var b_hist = this.GetHistogram(src, ColorEnum.Blue);

                    // チャートの初期化
                    this.ChartInit(this.chart_R);
                    this.ChartInit(this.chart_G);
                    this.ChartInit(this.chart_B);

                    // ヒストグラムデータ挿入
                    this.SetChartData(this.chart_R, ColorEnum.Red, r_hist);
                    this.SetChartData(this.chart_G, ColorEnum.Green, g_hist);
                    this.SetChartData(this.chart_B, ColorEnum.Blue, b_hist);
                }
            }
        }

        /// <summary>
        /// 指定した色の単色画像を生成します
        /// </summary>
        /// <returns></returns>
        private Bitmap GetMonoColorImage(Bitmap src, ColorEnum color) {

            Bitmap bitmap = new Bitmap(src);

            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            byte[] buf = new byte[bitmap.Width * bitmap.Height * 4];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            for (int i = 0; i < buf.Length;)
            {
                if (color == ColorEnum.Red) {
                    //buf[i + 2]        // R
                    buf[i + 1] = 0;     // G
                    buf[i] = 0;         // B
                }
                else if (color == ColorEnum.Green) {
                    buf[i + 2] = 0;     // R
                    //buf[i + 1] = 0;   // G
                    buf[i] = 0;         // B
                }
                else if (color == ColorEnum.Blue) {
                    buf[i + 2] = 0;     // R
                    buf[i + 1] = 0;     // G
                    //buf[i] = 0;       // B
                }
                i = i + 4;
            }
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            bitmap.UnlockBits(data);

            return bitmap;
        }

        /// <summary>
        /// 画像のヒストグラムを取得します
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        private int[] GetHistogram(Bitmap src, ColorEnum color) {
            // 画像色用256配列を用意
            var histogram = new int[256];

            BitmapData data = src.LockBits(
                new Rectangle(0, 0, src.Width, src.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            byte[] buf = new byte[src.Width * src.Height * 4];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            for (int i = 0; i < buf.Length;)
            {
                if (color == ColorEnum.Red)
                {
                    // Rの頻度を数える
                    histogram[buf[i + 2]]++;
                }
                else if (color == ColorEnum.Green)
                {
                    // Gの頻度を数える
                    histogram[buf[i + 1]]++;
                }
                else if (color == ColorEnum.Blue)
                {
                    // Bの頻度を数える
                    histogram[buf[i]]++;
                }
                i = i + 4;
            }
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            src.UnlockBits(data);

            return histogram;
        }

        /// <summary>
        /// チャートの初期化
        /// </summary>
        /// <param name="chart"></param>
        private void ChartInit(System.Windows.Forms.DataVisualization.Charting.Chart chart) {
            // Chartコントロール内のグラフ、凡例、目盛り領域を削除
            chart.Series.Clear();
            chart.Legends.Clear();
            chart.ChartAreas.Clear();

            // 目盛り領域の設定
            var ca = chart.ChartAreas.Add("Histogram");

            // X軸
            ca.AxisX.Title = "Pixel";  // タイトル
            ca.AxisX.Minimum = 0;           // 最小値
            ca.AxisX.Maximum = 256;         // 最大値
            ca.AxisX.Interval = 64;         // 目盛りの間隔
                                            
            ca.AxisY.Title = "Count";       // Y軸
            ca.AxisY.Minimum = 0;
        }

        private void SetChartData(System.Windows.Forms.DataVisualization.Charting.Chart chart, ColorEnum color, int[] data) {
            // グラフの系列を追加
            var series = chart.Series.Add("Histogram");

            // グラフの種類を折れ線に設定する
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

            series.BorderWidth = 2;

            // 輪郭線の色
            if (color == ColorEnum.Red)
            {
                series.BorderColor = Color.Red;
            }
            else if (color == ColorEnum.Green)
            {
                series.BorderColor = Color.Green;
            }
            else if (color == ColorEnum.Blue)
            {
                series.BorderColor = Color.Blue;
            }

            // データ挿入
            for (int i = 0; i < data.Length; i++)
            {
                series.Points.AddXY(i, data[i]);
            }
        }
    }
}
