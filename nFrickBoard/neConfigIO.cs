using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Documents;

namespace nFrickBoard
{
    class neConfigIO
    {

        /// <summary>
        /// ネコペのディレクトリを取得
        /// 初回起動時や存在しない場合入力させる
        /// </summary>
        /// <param name="Dir"></param>
        public string GetNpDir()
        {
            bool ChkFlg = true;
            string  Dir = Properties.Settings.Default.npDir;
            if (Dir == "")
            {
                //初回起動時
                MessageBox.Show("ネコペイント本体の場所を指定してください","起動時設定");
                ChkFlg = false;
            }
            else
            {
                DirectoryInfo dirChk = new DirectoryInfo(Dir);
                if (dirChk.Exists == true)
                {
                    var aaa = dirChk.GetFiles(Constants.NPExeName);
                    if (aaa.Length == 0)
                    {
                        MessageBox.Show("ネコペイント本体が見つかりません。\nネコペイント本体の場所を指定して下さい", "警告");
                        ChkFlg = false;
                    }
                }
                else
                {
                    MessageBox.Show("ネコペイント本体が見つかりません。\nネコペイント本体の場所を指定して下さい", "警告");
                    ChkFlg = false;
                }
            }
            if (ChkFlg == false)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.FileName = "";
                dlg.Filter = "npaint_script.exe|npaint_script.exe";
                dlg.Title = "ネコペイント本体の場所を指定して下さい";

                Nullable<bool> result = dlg.ShowDialog();
                if (result == true)
                {
                    // Open document
                    Dir = Path.GetDirectoryName(dlg.FileName);
                    Properties.Settings.Default.npDir = Dir;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    Dir = "";
                }
            }
            return (Dir);
        }
        
        /// <summary>
        /// アプリケーション設定からショートカットキー全定義取得
        /// データグリッドの一覧作成元になります
        /// </summary>
        /// <param name="KeyConvDef"></param>
        public void GetAllDef(KeyConfigList KL)
        {
            StringCollection src = Properties.Settings.Default.AllKeyDef;
            //ショートカットキー全定義取得
            foreach (string str in src)
            {
                KeyConfigData fd = new KeyConfigData();
                //最初の空白を検索
                int ito = str.IndexOf(" ");
                //最初の空白までをカテゴリとして抽出
                fd.Cat = str.Substring(0, ito);
                int ifrom = ito + 1;
                ito = str.IndexOf(" ", ifrom);
                //次の空白までをIDとして抽出
                fd.ID = Convert.ToInt32(str.Substring(ifrom, ito - ifrom));
                ifrom = ito + 1;
                //残りを機能名として設定
                fd.name = str.Substring(ifrom);
                KL.Add(fd);
            }
        }

        /// <summary>
        /// 猫ペイントのkey.txtを取得
        /// 対象はショートカット設定済みの機能のみ
        /// </summary>
        public void ReadKey_txt(string NpDir, Dictionary<int, string> Key_txt)
        {
            try
            {
                // シフトJISのファイルの読み込み
                string[] lines1 = File.ReadAllLines(NpDir + Constants.NPKeyPath,
                    System.Text.Encoding.GetEncoding("Shift_JIS"));
                foreach (string line in lines1)
                {
                    int ID;
                    string KEY;

                    //最初の空白を検索
                    int ito = line.IndexOf(" ");
                    //最初の空白までをIDとして抽出
                    ID = Convert.ToInt32(line.Substring(0, ito));
                    //最初の{を検索
                    int ifrom = ito + 1;
                    ito = line.IndexOf("{", ifrom);
                    if (ito > 0)
                    {
                        //{までを機能名として設定
                        ifrom = ito;
                        //最初の}を検索
                        ito = line.IndexOf("}");
                        if (ito > ifrom)
                        {
                            //{から}までを取得。
                            KEY = line.Substring(ifrom + 1, ito - ifrom - 1);
                            //割り当て済みリストに追加
                            Key_txt.Add(ID, KEY);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        /// <summary>
        /// 猫ペイントのスクリプトリストを取得
        /// 対象はショートカット設定済みの機能のみ。重複した場合は最初の一件のみ
        /// </summary>
        public void ReadScriptList(string NpDir, string FileName, Dictionary<int, string> Key_txt, Dictionary<string, int> List_txt)
        {
            int ScriptID = 4352;
            try
            {
                // シフトJISのファイルの読み込み
                string[] lines1 = File.ReadAllLines(NpDir + "/script/menu/" + FileName,
                    System.Text.Encoding.GetEncoding("Shift_JIS"));
                foreach (string line in lines1)
                {
                    string FuncName;
                    string tmpDiv;
                    int ifrom, ito;

                    //割り当て済みショートカット一覧からIDを検索
                    if (Key_txt.ContainsKey(ScriptID) == false)
                    {
                        //割り当てがないレコードは無視
                        ScriptID++;
                        continue;
                    }
                    //最初の#を検索
                    ito = line.IndexOf("#");
                    if (ito < 0)
                    {
                        //#がない場合全部抽出
                        tmpDiv = line;
                    }
                    else
                    {
                        //#より前の部分の処理
                        if (ito == 0)
                        {
                            //#が先頭の場合無視
                            ScriptID++;
                            continue;
                        }
                        else
                        {
                            //最初の#までを抽出
                            tmpDiv = line.Substring(0, ito);
                        }
                    }
                    //最初の空白を検索
                    ito = tmpDiv.IndexOf(" ");
                    if (ito < 0)
                    {
                        //空白がない場合無視
                        ScriptID++;
                        continue;
                    }
                    else
                    {
                        //最初の空白までをスクリプトファイル名として抽出
                        FuncName = tmpDiv.Substring(0, ito);
                        ifrom = ito + 1;
                        //2番目の空白を検索
                        ito = tmpDiv.IndexOf(" ", ifrom);
                        if (ito < 0)
                        {
                            //空白がない場合全てスクリプト名に結合
                            FuncName += " " + tmpDiv.Substring(ifrom);
                        }
                        else
                        {
                            //2番目の空白までをスクリプトファイル名に結合
                            //2番めの空白以降は無視
                            FuncName += " " + tmpDiv.Substring(ifrom, ito - ifrom);
                        }
                    }
                    //作成済みのレコードから同一機能を検索
                    if (List_txt.ContainsKey(FuncName) == false)
                    {
                        //未登録なら追加
                        List_txt.Add(FuncName, ScriptID);
                    }
                    ScriptID++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 猫ペイントのkey.txtを書き込み
        /// </summary>
        /// <param name="KL"></param>
        public void WriteNekoKeytxt(string NpDir,KeyConfigList KL)
        {
            try
            {
                //ファイルにテキストを書き出し
                using (StreamWriter w = new StreamWriter(NpDir + Constants.NPKeyPath
                    , false, System.Text.Encoding.GetEncoding("Shift_JIS")))
                {
                    foreach (var tmp in KL)
                    {
                        string str = Convert.ToString(tmp.ID) + " " + tmp.name + tmp.assign;
                        w.WriteLine(str);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 猫ペイントのスクリプトリストを書き込み
        /// 全スクリプトをuser_list.txtに書き込み、list.txtは空にする
        /// </summary>
        /// <param name="KL"></param>
        public void WriteNekoListtxt(string NpDir, List<string> tmpList)
        {
            try
            {
                //ファイルにテキストを書き出し
                using (StreamWriter w = new StreamWriter(NpDir + "/script/menu/user_list.txt"
                    , false, System.Text.Encoding.GetEncoding("Shift_JIS")))
                {
                    foreach (var tmp in tmpList)
                    {
                        w.WriteLine(tmp);
                    }
                }
                //ファイルにテキストを書き出し
                using (StreamWriter w2 = new StreamWriter(NpDir + "/script/menu/list.txt"
                    , false, System.Text.Encoding.GetEncoding("Shift_JIS")))
                {
                    w2.Write("");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 設定取得汎用
        /// </summary>
        /// <param name="dest"></param>
        public void GetDef(Dictionary<string, string> dest, StringCollection src)
        {
            //キー表記変換定義取得
            int div = 0;
            string key, val;
            foreach (string str in src)
            {
                div = str.IndexOf(' ');
                if (div < 0)
                {
                    continue;
                }
                key = str.Substring(0, div);
                val = str.Substring(div + 1);
                dest.Add(key, val);
            }
        }
        public void GetDef(Dictionary<string, int> dest, StringCollection src)
        {
            //キー表記変換定義取得
            int div = 0;
            string key, val;
            foreach (string str in src)
            {
                div = str.IndexOf(' ');
                if (div < 0)
                {
                    continue;
                }
                key = str.Substring(0, div);
                val = str.Substring(div + 1);
                dest.Add(key, Convert.ToInt32(val));
            }
        }
        public void GetDef(Dictionary<int, string> dest, StringCollection src)
        {
            //キー表記変換定義取得
            int div = 0;
            string key, val;
            foreach (string str in src)
            {
                div = str.IndexOf(' ');
                if (div < 0)
                {
                    continue;
                }
                key = str.Substring(0, div);
                val = str.Substring(div + 1);
                dest.Add(Convert.ToInt32(key), val);
            }
        }

        /// <summary>
        /// nasファイルを読み込み使用可能メソッド一覧を作成
        /// </summary>
        /// <param name="FileName"></param>
        public ObservableCollection<string> ReadScriptFile(string NpDir, string FileName)
        {
            ObservableCollection<string> Ret = new ObservableCollection<string>();
            try
            {
                // シフトJISのファイルの読み込み
                string[] lines1 = File.ReadAllLines(NpDir + "/script/menu/" + FileName,
                    System.Text.Encoding.GetEncoding("Shift_JIS"));
                foreach (string line in lines1)
                {
                    //void *() に一致する行以外無視
                    string tmpDiv;
                    string Name;
                    int idx;

                    //コメントを除外
                    idx = line.IndexOf("//");
                    if (idx < 0)
                    {
                        //#がない場合全部抽出
                        tmpDiv = line;
                    }
                    else
                    {
                        //コメント部以外を抽出
                        tmpDiv = line.Substring(0, idx);
                    }
                    tmpDiv = tmpDiv.Trim();

                    //先頭が"void"でなければ無視
                    idx = tmpDiv.IndexOf("void", StringComparison.CurrentCultureIgnoreCase);
                    if (idx != 0) continue;

                    //空白がなければ無視
                    idx = tmpDiv.IndexOf(" ");
                    if (idx < 0) continue;

                    tmpDiv = tmpDiv.Substring(idx);

                    //最初の()を検索
                    idx = tmpDiv.IndexOf("()");
                    if (idx < 0) continue;

                    Name = tmpDiv.Substring(1, idx - 1);

                    Ret.Add(Name);
                }
                return Ret;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
