using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Runtime.Serialization;

namespace nFrickBoard
{
    [DataContract]
    public class UserSettings
    {

        //猫ペイント本体ディレクトリ
        [DataMember]
        public string NpDir { get; set; }

        //オブジェクト基本サイズ
        [DataMember]
        public int ObjSize { get; set; }

        //コントロール件数
        [DataMember]
        public int ButtonCnt { get; set; }
        [DataMember]
        public int PadCnt { get; set; }

        //フリック判定範囲
        [DataMember]
        public int FrickRange { get; set; }
        [DataMember]
        public int FrickCancel { get; set; }

        [DataMember]
        public UserButton[] Button { get; set; }

        [DataMember]
        public UserButton[] Pad{ get; set; }

        private static UserSettings _instance;
        public static UserSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UserSettings();
                return _instance;
            }
            set { _instance = value; }
        }

        public static void SaveSetting()
        {
            string appPath = System.Windows.Forms.Application.StartupPath;
            appPath = @Constants.SETTING_FILE_NAME;

            DataContractSerializer ds = new DataContractSerializer(typeof(UserSettings));
            
            //Xmlの改行・インデント設定
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";

            XmlWriter xw = XmlWriter.Create(appPath,settings);
            //シリアル化して書き込む
            ds.WriteObject(xw, Instance);
            xw.Close();
        }
        public static void LoadSetting()
        {
            string appPath = System.Windows.Forms.Application.StartupPath;
            appPath = @Constants.SETTING_FILE_NAME;
            if (System.IO.File.Exists(appPath))
            {
                //ファイルが存在する場合
                DataContractSerializer ds = new DataContractSerializer(typeof(UserSettings));
                XmlReader xr = XmlReader.Create(appPath);
                //XMLファイルから読み込み、逆シリアル化する
                Instance = (UserSettings)ds.ReadObject(xr);
                xr.Close();
            }
        }
    }
    public class UserButton
    {
        //識別番号
        [DataMember]
        public int ID { get; set; }
        //ボタン種類（通常、修飾キー）
        [DataMember]
        public int kind { get; set; }
        //ボタンに表示するテキスト
        [DataMember]
        public string BtnTxt { get; set; }
        //X座標
        [DataMember]
        public int X { get; set; }
        //Y座標
        [DataMember]
        public int Y { get; set; }
        //キー割り当て
        [DataMember]
        public string[] KeyAssign { get; set; }
        [DataMember]
        public string[] KeyText { get; set; }
        public UserButton()
        {
            KeyAssign = new string[Constants.FRICK_WAY+1];
            KeyText = new string[Constants.FRICK_WAY + 1];
        }
    }
}
