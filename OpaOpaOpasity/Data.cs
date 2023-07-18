using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OpaOpaOpasity
{
    public class Data : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        //対象フォルダ
        private string dir = "";
        public string Dir { get => dir; set => SetProperty(ref dir, value); }

        //対象ファイル名リスト
        private ObservableCollection<string> fileList = new();
        public ObservableCollection<string> FileList { get => fileList; set => SetProperty(ref fileList, value); }

        //元画像
        private BitmapSource? bitmap;
        public BitmapSource? Bitmap { get => bitmap; set => SetProperty(ref bitmap, value); }

        //アルファ値
        private byte alpha = 255;
        public byte Alpha { get => alpha; set => SetProperty(ref alpha, value); }


    }
}
