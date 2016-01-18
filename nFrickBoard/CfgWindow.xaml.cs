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
#if false
            testclass[] writeclass = new testclass[ButtonArray.Length];
            foreach (FrickButton btn in ButtonArray)
            {
                writeclass[cnt] = new testclass();
                writeclass[cnt].ID = btn.ID;
                writeclass[cnt].KeyAssign = btn.KeyAssign;
                cnt++;
            }

#else
//                testclass writeclass = new testclass();
            UserSettings.LoadSetting();
            UserSettings.Instance.test += "a";
            UserSettings.Instance.ID = ButtonArray[0].ID;
            UserSettings.Instance.KeyAssign = ButtonArray[0].KeyAssign;
            UserSettings.SaveSetting();
            //シリアル化して書き込む
//                writeclass.save();
            
#endif
            this.Close();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    public class testclass
    {
        //識別番号
        public int ID { get; set; }
        //キー割り当て
        public int[][] KeyAssign { get; set; }

        public testclass()
        {
        }

        public void save()
        {
            string appPath = System.Windows.Forms.Application.StartupPath;
            appPath += @"\test.config";
            StreamWriter sw = new StreamWriter(appPath);
            XmlSerializer xs = new XmlSerializer(typeof(testclass));
            //シリアル化して書き込む
            xs.Serialize(sw, this);
        }
    }
}
