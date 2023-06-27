using System;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        public string MyDirectory { get; private set; } = "";
        public MainWindow()
        {
            InitializeComponent();
            Title = MY_APP_NAME + GetAppVersion();
            SetMyImageGridBackground();
            MyButtonConvertAll.FontSize = FontSize * 1.5;
            MyButtonConvertSelected.FontSize = FontSize * 1.5;

            DataContext = this;
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

        private void SelectedExe(MyOpe ope)
        {
            List<string> files = new();
            foreach (var item in MyListBox.SelectedItems)
            {
                string str = (string)item;
                files.Add(System.IO.Path.Combine(MyDirectory, str));
            }
            MyExe(files, ope);
        }

        private void MyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyListBox.SelectedItem is string fileName)
            {
                string path = System.IO.Path.Combine(MyDirectory, fileName);
                var bmp = GetBitmap(path);
                MyImage.Source = bmp;
            }
        }

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
                MyDirectory = dir;
                MyTextBlockDir.Text = dir;
                MyListBox.ItemsSource = GetFileNames(dir);
                SetMyListBoxContextMenu();
            }
        }

        /// <summary>
        /// 指定フォルダの中のpngファイルだけのリストを返す
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>

        private List<string> GetFileNames(string dir)
        {
            List<string> names = new();
            foreach (var item in Directory.GetFiles(dir, "*.png"))
            {
                names.Add(System.IO.Path.GetFileName(item));
            }
            return names;
        }




        /// <summary>
        /// 実行
        /// 対象フォルダにopaフォルダ作成、そこにAlpha値を変換した画像を
        /// png形式で保存
        /// </summary>
        private void MyExe(IEnumerable<string> files, MyOpe ope)
        {
            try
            {
                if (!string.IsNullOrEmpty(MyDirectory))
                {
                    string dir = System.IO.Path.Combine(MyDirectory, MY_FOlDER_NAME);// + "\\opa";
                    _ = Directory.CreateDirectory(dir);
                    foreach (var file in files)
                    {
                        SaveExe(file,
                            dir + "\\" + System.IO.Path.GetFileName(file),
                            (byte)MySlider.Value,
                            ope);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 画像ファイルを指定Alphaに変換して保存
        /// </summary>
        /// <param name="bmpPath"></param>
        /// <param name="savePath"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        private bool SaveExe(string bmpPath, string savePath, byte alpha, MyOpe ope)
        {
            if (GetBitmap(bmpPath) is BitmapSource bmp)
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
                SaveBitmapToPng(savePath, bmp);
                return true;
            }
            return false;
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



        private BitmapSource? GetBitmap(string path)
        {
            if (!File.Exists(path)) return null;
            PngBitmapDecoder decoder = new(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            BitmapSource bmp = decoder.Frames[0];
            return ConverterBitmapFromatBgra32(bmp);
        }

        private static BitmapSource ConverterBitmapFromatBgra32(BitmapSource bmp)
        {
            if (bmp.Format != PixelFormats.Bgra32)
            {
                FormatConvertedBitmap fc = new(bmp, PixelFormats.Bgra32, null, 0.0);
                return fc;
            }
            return bmp;
        }

        #region メインウィンドウボタンクリック

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MySlider.Value = 0.0;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MySlider.Value = 128;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MySlider.Value = 255;
        }


        private void ButtonAllReplace_Click(object sender, RoutedEventArgs e)
        {
            MyExe(Directory.GetFiles(MyDirectory, "*.png"), MyOpe.Replace);
        }

        private void ButtonAllAdd_Click(object sender, RoutedEventArgs e)
        {
            MyExe(Directory.GetFiles(MyDirectory, "*.png"), MyOpe.Add);
        }

        private void ButtonAllSubtract_Click(object sender, RoutedEventArgs e)
        {
            MyExe(Directory.GetFiles(MyDirectory, "*.png"), MyOpe.Subtract);
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

    }

    public class MyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double dd = (double)value;
            return dd / 255;
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
