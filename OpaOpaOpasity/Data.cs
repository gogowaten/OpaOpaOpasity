using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
