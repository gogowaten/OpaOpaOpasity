﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

//半透明のpng画像を不透明に変換保存するアプリ、OpaOpaOpasityできた - 午後わてんのブログ
//https://gogowaten.hatenablog.com/entry/2023/06/26/200756

namespace OpaOpaOpasity
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string MY_APP_NAME = "OpaOpaOpacity(おぱおぱおぱしてぃ)";
        private const string MY_FOlDER_NAME = "opaopaopasity";

        public Data MyData { get; private set; } = new();
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            this.Left = 100; this.Top = 100;
#endif

            DataContext = MyData;

            Title = MY_APP_NAME + GetAppVersion();
            SetMyImageGridBackground();
            MyButtonConvertAll.FontSize = FontSize * 1.5;
            MyButtonConvertSelected.FontSize = FontSize * 1.5;


            Drop += MainWindow_Drop;
            MyListBox.SelectionChanged += MyListBox_SelectionChanged;


        }

        #region 初期化

        /// <summary>
        /// アプリのバージョン取得、できなかかったときはstring.Emptyを返す
        /// </summary>
        /// <returns></returns>
        private static string GetAppVersion()
        {
            //実行ファイルのバージョン取得
            string[] cl = Environment.GetCommandLineArgs();

            //System.Diagnostics.FileVersionInfo
            if (FileVersionInfo.GetVersionInfo(cl[0]).FileVersion is string ver)
            {
                return ver;
            }
            else { return string.Empty; }
        }

        private void SetMyImageGridBackground()
        {
            var bg = MakeCheckeredPattern(20, Color.FromRgb(230, 230, 230));
            var br = MakeTileBrush(bg);
            MyImageGrid.Background = br;
        }
        #endregion 初期化

        #region 右クリックメニュー
        private void SetMyListBoxContextMenu()
        {
            ContextMenu MyListBoxContext = new();
            MyListBox.ContextMenu = MyListBoxContext;

            MenuItem item;
            item = new() { Header = "Alpha値を置き換える" };
            item.Click += ItemReplace_Click;
            MyListBoxContext.Items.Add(item);
            item = new() { Header = "足し算" };
            item.Click += ItemAdd_Click;
            MyListBoxContext.Items.Add(item);
            item = new() { Header = "引き算" };
            item.Click += ItemSubtract_Click;
            MyListBoxContext.Items.Add(item);

        }


        #endregion 右クリックメニュー

        #region 市松模様画像とブラシ作成

        /// <summary>
        /// 市松模様画像作成
        /// </summary>
        /// <param name="cellSize">タイル1辺のサイズ</param>
        /// <param name="gray">白じゃない方の色指定</param>
        /// <returns></returns>
        private WriteableBitmap MakeCheckeredPattern(int cellSize, Color gray)
        {
            int width = cellSize * 2;
            int height = cellSize * 2;
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            int stride = wb.Format.BitsPerPixel / 8 * width;
            byte[] pixels = new byte[stride * height];
            int p = 0;
            Color iro;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if ((y < cellSize & x < cellSize) | (y >= cellSize & x >= cellSize))
                    {
                        iro = Colors.White;
                    }
                    else { iro = gray; }

                    p = y * stride + x * 3;
                    pixels[p] = iro.R;
                    pixels[p + 1] = iro.G;
                    pixels[p + 2] = iro.B;
                }
            }
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return wb;
        }

        //        方法: TileBrush のタイル サイズを設定する | Microsoft Docs
        //https://docs.microsoft.com/ja-jp/dotnet/framework/wpf/graphics-multimedia/how-to-set-the-tile-size-for-a-tilebrush
        /// <summary>
        /// BitmapからImageBrush作成
        /// 引き伸ばし無しでタイル状に敷き詰め
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private ImageBrush MakeTileBrush(BitmapSource bitmap)
        {
            var imgBrush = new ImageBrush(bitmap);
            imgBrush.Stretch = Stretch.Uniform;//これは必要ないかも
            //タイルモード、タイル
            imgBrush.TileMode = TileMode.Tile;
            //タイルサイズは元画像のサイズ
            imgBrush.Viewport = new Rect(0, 0, bitmap.Width, bitmap.Height);
            //タイルサイズ指定方法は絶対値、これで引き伸ばされない
            imgBrush.ViewportUnits = BrushMappingMode.Absolute;
            return imgBrush;
        }
        #endregion 市松模様画像とブラシ作成

        //選択項目だけ変換
        private void SelectedExe(MyOpe ope)
        {
            Collection<string> files = new();
            foreach (var item in MyListBox.SelectedItems)
            {
                string str = (string)item;
                files.Add(System.IO.Path.Combine(MyData.Dir, str));
            }
            //SavePngImage(files, ope);
            SavePngImage(MyData.Dir, files, ope, MyData.Alpha);
        }

        //選択項目変更で画像表示更新
        private void MyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyListBox.SelectedItem is string fileName)
            {
                string path = System.IO.Path.Combine(MyData.Dir, fileName);
                if (GetBitmapFromFilePath(path) is (BitmapSource bmp, byte[]))
                {
                    MyData.Bitmap = bmp;
                }
            }
        }

        //ファイルドロップ時
        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            string[] ff = (string[])e.Data.GetData(DataFormats.FileDrop);
            string dir = string.Empty;
            if (ff != null && ff.Length > 0)
            {
                if (File.GetAttributes(ff[0]).HasFlag(FileAttributes.Directory))
                {
                    dir = ff[0];
                }
                else
                {
                    var dirr = System.IO.Path.GetDirectoryName(ff[0]);
                    if (Directory.Exists(dirr)) { dir = dirr; }
                }
                MyData.Dir = dir;
                MyData.FileList = new ObservableCollection<string>(GetImageFileNames(dir));
                GetImageFileNames(dir);
                SetMyListBoxContextMenu();
            }
        }

        /// <summary>
        /// 指定フォルダの中の画像ファイルリストを返す、判定は拡張子
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private List<string> GetImageFileNames(string dir)
        {
            List<string> paths = new();
            paths.AddRange(Directory.GetFiles(dir, "*.png"));
            paths.AddRange(Directory.GetFiles(dir, "*.jpg"));
            paths.AddRange(Directory.GetFiles(dir, "*.jpeg"));
            paths.AddRange(Directory.GetFiles(dir, "*.bmp"));
            paths.AddRange(Directory.GetFiles(dir, "*.gif"));
            paths.AddRange(Directory.GetFiles(dir, "*.tiff"));
            paths.AddRange(Directory.GetFiles(dir, "*.wdp"));
            paths.Sort();

            List<string> names = new();
            foreach (string path in paths)
            {
                names.Add(System.IO.Path.GetFileName(path));
            }

            return names;
        }



        /// <summary>
        /// 対象フォルダにopaopaopasityフォルダ作成、そこにAlpha値を変換した画像をpng形式で保存
        /// </summary>
        /// <param name="files">対象画像ファイルリスト</param>
        /// <param name="ope">アルファ値の計算方法</param>
        private void SavePngImage(string dir, Collection<string> files, MyOpe ope, byte alpha)
        {
            try
            {
                if (!string.IsNullOrEmpty(dir))
                {
                    string filePath = System.IO.Path.Combine(dir, MY_FOlDER_NAME);
                    _ = Directory.CreateDirectory(filePath);
                    Parallel.For(0, files.Count, async ii =>
                    {
                        await SaveExe(files[ii], filePath + "\\" + System.IO.Path.GetFileName(files[ii]),
                            alpha,
                              ope);
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //private void SavePngImage(List<string> files, MyOpe ope)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(MyData.Dir))
        //        {
        //            string dir = System.IO.Path.Combine(MyData.Dir, MY_FOlDER_NAME);
        //            _ = Directory.CreateDirectory(dir);
        //            Parallel.For(0, files.Count, async ii =>
        //            {
        //                await SaveExe(files[ii], dir + "\\" + System.IO.Path.GetFileName(files[ii]),
        //                    MyData.Alpha,
        //                      ope);
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //}





        /// <summary>
        /// 画像ファイルを指定Alphaと計算して保存
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <param name="savePath">保存パス</param>
        /// <param name="alpha">アルファ値指定</param>
        /// <param name="ope">アルファ値の計算方法指定</param>
        /// <returns></returns>
        private async Task<bool> SaveExe(string filePath, string savePath, byte alpha, MyOpe ope)
        {
            if (GetBitmapFromFilePath(filePath) is (BitmapSource bmp, byte[]))
            {
                switch (ope)
                {
                    case MyOpe.Replace:
                        bmp = AlphaReplace(bmp, alpha);
                        break;
                    case MyOpe.Add:
                        bmp = AlphaAdd(bmp, alpha);
                        break;
                    case MyOpe.Subtract:
                        bmp = AlphaSubtract(bmp, alpha);
                        break;
                    default:
                        break;
                }
                await Task.Run(() =>
                {
                    SaveBitmapToPng(savePath, bmp);
                });
                return true;
            }
            return false;
            //if (GetBitmap(bmpPath) is BitmapSource bmp)
            //{
            //    switch (ope)
            //    {
            //        case MyOpe.Replace:
            //            bmp = AlphaReplace(bmp, alpha);
            //            break;
            //        case MyOpe.Add:
            //            bmp = AlphaAdd(bmp, alpha);
            //            break;
            //        case MyOpe.Subtract:
            //            bmp = AlphaSubtract(bmp, alpha);
            //            break;
            //        default:
            //            break;
            //    }
            //    await Task.Run(() =>
            //    {
            //        SaveBitmapToPng(savePath, bmp);
            //    });
            //    return true;
            //}
            //return false;

        }

        private void SaveBitmapToPng(string filename, BitmapSource bmp)
        {
            BitmapMetadata metadata = new("png");
            metadata.SetQuery("/tEXt/Software", MY_APP_NAME);
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bmp, null, metadata, null));
            using FileStream stream = new(filename, FileMode.Create, FileAccess.Write);
            encoder.Save(stream);
        }

        #region アルファ値変換



        /// <summary>
        /// すべてのピクセルを指定Alphaに変換。元のAlpha値が0だった場合は変換しない
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        private BitmapSource AlphaReplace(BitmapSource bmp, byte alpha)
        {
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;
            int stride = w * 4;
            byte[] pixels = new byte[stride * h];
            bmp.CopyPixels(pixels, stride, 0);
            for (int i = 3; i < pixels.Length; i += 4)
            {
                //完全透明は変換しない
                if (pixels[i] != 0)
                {
                    pixels[i] = alpha;
                }
            }
            return BitmapSource.Create(w, h, bmp.DpiX, bmp.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        }

        //足し算、255で飽和
        private BitmapSource AlphaAdd(BitmapSource bmp, byte alpha)
        {
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;
            int stride = w * 4;
            byte[] pixels = new byte[stride * h];
            bmp.CopyPixels(pixels, stride, 0);
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] != 0)
                {
                    if (pixels[i] + alpha > 255)
                    {
                        pixels[i] = 255;
                    }
                    else
                    {
                        pixels[i] += alpha;
                    }
                }
            }
            return BitmapSource.Create(w, h, bmp.DpiX, bmp.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        }

        //引き算、0で飽和
        private BitmapSource AlphaSubtract(BitmapSource bmp, byte alpha)
        {
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;
            int stride = w * 4;
            byte[] pixels = new byte[stride * h];
            bmp.CopyPixels(pixels, stride, 0);
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] != 0)
                {
                    if (pixels[i] - alpha < 0)
                    {
                        pixels[i] = 0;
                    }
                    else
                    {
                        pixels[i] -= alpha;
                    }
                }
            }
            return BitmapSource.Create(w, h, bmp.DpiX, bmp.DpiY, PixelFormats.Bgra32, null, pixels, stride);
        }
        #endregion アルファ値変換


        #region 画像を開く

        /// <summary>
        /// 画像ファイルとして開いて返す、エラーの場合はnullを返す
        /// dpiは96に変換する、このときのピクセルフォーマットはbgra32
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private (BitmapSource?, byte[]?) GetBitmapFromFilePath(string path)
        {
            using FileStream stream = File.OpenRead(path);
            BitmapSource bmp;
            try
            {
                bmp = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                //BitmapSource bbb = ConverterBitmapFromatBgra32(bmp);
                return ConverterBitmapDipWithPixels(ConverterBitmapFromatBgra32(bmp));
                //return ConverterBitmapDpi96AndPixFormatBgra32(bmp);
            }
            catch (Exception)
            {
                return (null, null);
            }
        }


        /// <summary>
        /// BitmapSourceのピクセルフォーマットをBgra32に変換するだけ
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        private static BitmapSource ConverterBitmapFromatBgra32(BitmapSource bmp)
        {
            if (bmp.Format != PixelFormats.Bgra32)
            {
                FormatConvertedBitmap fc = new(bmp, PixelFormats.Bgra32, null, 0.0);
                return fc;
            }
            return bmp;
        }

        /// <summary>
        /// BitmapSourceのdpiを96に変換＋pixelsも返す、PixelFormatsBgra32専用
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        private (BitmapSource, byte[]) ConverterBitmapDipWithPixels(BitmapSource bmp)
        {
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;
            int stride = w * 4;
            byte[] pixels = new byte[stride * h];
            bmp.CopyPixels(pixels, stride, 0);
            //png画像はdpi95.98とかの場合もあるけど、
            //これは問題ないので変換しない
            if (bmp.DpiX < 95.0 || 96.0 < bmp.DpiX)
            {
                bmp = BitmapSource.Create(w, h, 96.0, 96.0, bmp.Format, null, pixels, stride);
            }
            return (bmp, pixels);
        }

        //private BitmapSource? GetBitmap(string path)
        //{
        //    if (!File.Exists(path)) return null;
        //    PngBitmapDecoder decoder = new(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        //    BitmapSource bmp = decoder.Frames[0];
        //    return ConverterBitmapFromatBgra32(bmp);
        //}
        #endregion 画像を開く


        #region メインウィンドウボタンクリック

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MyData.Alpha = 0;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MyData.Alpha = 127;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MyData.Alpha = 255;
        }


        private void ButtonAllReplace_Click(object sender, RoutedEventArgs e)
        {
            SavePngImage(MyData.Dir, MyData.FileList, MyOpe.Replace, MyData.Alpha);
            //SavePngImage(Directory.GetFiles(MyData.Dir, "*.png").ToList(), MyOpe.Replace);
        }

        private void ButtonAllAdd_Click(object sender, RoutedEventArgs e)
        {
            SavePngImage(MyData.Dir, MyData.FileList, MyOpe.Add, MyData.Alpha);
            //SavePngImage(Directory.GetFiles(MyData.Dir, "*.png").ToList(), MyOpe.Add);
        }

        private void ButtonAllSubtract_Click(object sender, RoutedEventArgs e)
        {
            SavePngImage(MyData.Dir, MyData.FileList, MyOpe.Subtract, MyData.Alpha);
            //SavePngImage(Directory.GetFiles(MyData.Dir, "*.png").ToList(), MyOpe.Subtract);
        }

        private void ButtonSelectedReplace_Click(object sender, RoutedEventArgs e)
        {
            SelectedExe(MyOpe.Replace);
        }

        private void ButtonSelectedAdd_Click(object sender, RoutedEventArgs e)
        {
            SelectedExe(MyOpe.Add);
        }

        private void ButtonSelectedSubtract_Click_4(object sender, RoutedEventArgs e)
        {
            SelectedExe(MyOpe.Subtract);
        }
        #endregion メインウィンドウボタンクリック

        #region 右クリックメニュークリック

        private void ItemReplace_Click(object sender, RoutedEventArgs e)
        {
            SelectedExe(MyOpe.Replace);
        }

        private void ItemSubtract_Click(object sender, RoutedEventArgs e)
        {
            SelectedExe(MyOpe.Subtract);
        }

        private void ItemAdd_Click(object sender, RoutedEventArgs e)
        {
            SelectedExe(MyOpe.Add);
        }

        #endregion 右クリックメニュークリック

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ddd = MyData;
            var bbb = BindingOperations.GetBinding(TTT, TextBlock.TextProperty);
        }
    }

    public class MyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte dd = (byte)value;
            return dd / 255.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public enum MyOpe
    {
        Replace = 0, Add, Subtract
    }
}
