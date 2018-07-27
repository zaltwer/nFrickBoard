using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace nFrickBoard
{
    //キーコンフィグデータ定義
    public class KeyConfigData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        //識別番号
        public int ID { get; set; }
        //機能名
        public string name { get; set; }
        //キー割り当て
        //これだけは変更があるためOnPropertyChangedを実装
        private string _assign;
        public string assign
        {
            get { return _assign; }
            set
            {
                _assign = value;
                OnPropertyChanged("assign");
            }
        }
        //カテゴリ
        public string Cat { get; set; }

        public KeyConfigData()
        {
            ID = 0;
            name = "";
            assign = "";
            Cat = "";
        }
        /// <summary>
        /// 1機能に複数ショートカット登録対策の分割処理
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dist"></param>
        public void DivAssign(List<string> dist)
        {
            int ifrom = 0;
            int ito = 0;
            dist.Clear();
            try
            {
                while (ito < assign.Length)
                {
                    //最初の{を検索
                    ifrom = assign.IndexOf("{", ito);
                    if (ifrom >= 0)
                    {
                        //{が発見できた場合}を検索
                        ito = assign.IndexOf("}", ifrom);
                        if (ito >= 0)
                        {
                            //{から}までをショートカットとして設定
                            dist.Add(assign.Substring(ifrom + 1, ito - ifrom - 1));
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 1機能に複数ショートカット登録対策の結合処理
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dist"></param>
        public void CatAssign(List<string> src)
        {
            assign = "";
            try
            {
                foreach (string tmp in src)
                {
                    //1レコードごとに{}で囲んで結合前後にスペース
                    assign += " {" + tmp + "} ";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void CatAssign(ItemCollection src)
        {
            assign = "";
            try
            {
                foreach (string tmp in src)
                {
                    //1レコードごとに{}で囲んで結合前後にスペース
                    assign += " {" + tmp + "} ";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 指定されたショートカットを削除
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dist"></param>
        public void DelAssign(string src)
        {
            List<string> aaa = new List<string>();
            DivAssign(aaa);
            aaa.Remove(src);
            CatAssign(aaa);
        }
        /// <summary>
        /// 指定されたショートカットを追加
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dist"></param>
        public void AddAssign(string src)
        {
            //1レコードごとに{}で囲んで結合前後にスペース
            assign += " {" + src + "} ";
        }
    }

    /// <summary>
    /// データグリッドバインド用のリスト
    /// </summary>
    public class KeyConfigList:ObservableCollection<KeyConfigData>
    {

    }

    //assign分割／結合用コンバータ
    [ValueConversion(typeof(KeyConfigData), typeof(ObservableCollection<string>))]
    public class AssignConverter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ObservableCollection<string> aaa = (ObservableCollection<string>)value;
            KeyConfigData KD = new KeyConfigData();
            foreach (string tmp in aaa)
            {
                //1レコードごとに{}で囲んで結合前後にスペース
                KD.assign += " {" + tmp + "} ";
            }
            return KD;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int ifrom = 0;
            int ito = 0;
            KeyConfigData KD = (KeyConfigData)value;
            ObservableCollection<string> dist = new ObservableCollection<string>();
            if (value == null) return dist;
            while (ito < KD.assign.Length)
            {
                //最初の{を検索
                ifrom = KD.assign.IndexOf("{", ito);
                if (ifrom >= 0)
                {
                    //{が発見できた場合}を検索
                    ito = KD.assign.IndexOf("}", ifrom);
                    if (ito >= 0)
                    {
                        //{から}までをショートカットとして設定
                        dist.Add(KD.assign.Substring(ifrom + 1, ito - ifrom - 1));
                    }
                }
                else
                {
                    break;
                }
            }
            return dist;
        }
    }
    //assign分割／結合用コンバータ2
    [ValueConversion(typeof(string), typeof(ObservableCollection<string>))]
    public class AssignConverter2 : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ObservableCollection<string> aaa = (ObservableCollection<string>)value;
            string assign = "";
            foreach (string tmp in aaa)
            {
                //1レコードごとに{}で囲んで結合前後にスペース
                assign += " {" + tmp + "} ";
            }
            return (assign);
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int ifrom = 0;
            int ito = 0;
            string aaa = (string)value;
            //            string aaa = bbb.assign;
            ObservableCollection<string> dist = new ObservableCollection<string>();
            while (ito < aaa.Length)
            {
                //最初の{を検索
                ifrom = aaa.IndexOf("{", ito);
                if (ifrom >= 0)
                {
                    //{が発見できた場合}を検索
                    ito = aaa.IndexOf("}", ifrom);
                    if (ito >= 0)
                    {
                        //{から}までをショートカットとして設定
                        dist.Add(aaa.Substring(ifrom + 1, ito - ifrom - 1));
                    }
                }
                else
                {
                    break;
                }
            }
            return dist;
        }
    }
}
