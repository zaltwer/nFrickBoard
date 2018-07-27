using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Serialization;
using System.IO;

namespace nFrickBoard
{
    /// <summary>
    /// ButtonSetting.xaml の相互作用ロジック
    /// </summary>
    public partial class ButtonSetting : Window
    {
        //識別番号
        public FrickButton[] ButtonArray { get; set; }
        public FrickPad[] PadArray { get; set; }

        public ButtonSetting()
        {
            InitializeComponent();
        }
        //初期化
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            ButtonArray[0].BtnTxt.Text = "test";
#if false //設定反映お試しコード
            int PadCnt = Properties.Settings.Default.PadCnt;
                //メインウインドウ再起動
                if (PadCnt == 1)
                {
                    PadCnt = 2;
                }
                else
                {
                    PadCnt = 1;
                }
                Properties.Settings.Default.PadCnt = PadCnt;
                Properties.Settings.Default.Save();
                System.Windows.Forms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
#endif

            UserSettings.SaveSetting();
            this.Close();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
