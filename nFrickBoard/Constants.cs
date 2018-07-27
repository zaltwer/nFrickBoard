using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nFrickBoard
{
    public class Constants
    {
        public static readonly string NP_EXE_NAME = "npaint_script.exe";    //ネコペ実行体名
        public static readonly string NP_KEY_PATH = "\\config\\key.txt";    //ネコペショートカット設定ファイル名
        public static readonly string SETTING_FILE_NAME = "test2.config";   //このソフトの設定ファイル名
        public static readonly string NP_SCRIPT_LIST = "list.txt";          //ネコペのスクリプトリスト名
        public static readonly string NP_USER_LIST = "user_list.txt";       //ネコペのユーザースクリプトリスト名
        public static readonly int FRICK_WAY = 4;                           //フリック方向
        public static readonly string WHEEL_DOWN = "WHEEL_D";               //設定ファイルでのホイールダウン指定用
        public static readonly string WHEEL_UP = "WHEEL_U";                 //設定ファイルでのホイールアップ指定用
        public static readonly int WHEEL_ASSIGN = 999;                      //ホイール用キーアサイン
        public static readonly int ButtonSize = 64;                         //ボタンサイズ
        public static readonly int BTN_NORMAL = 0;                          //通常ボタン
        public static readonly int BTN_MOD = 1;                             //修飾キー用ボタン
        public static readonly string MOD_TXT = "修飾キー";                 //修飾キー用ボタン表示用文字列
        public static readonly string MOD_RELEASE = "RELEASE";              //修飾キーすべて離す
    }
}
