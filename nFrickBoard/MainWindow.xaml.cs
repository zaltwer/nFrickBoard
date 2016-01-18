using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;

namespace nFrickBoard
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>

    public partial class MainWindow : Window
    {

        //アクティブにしないおまじない用
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        //IME制御用
        private const int WM_IME_CONTROL = 0x283;
        private const int IMC_SETOPENSTATUS = 0x6;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern Int32 SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("Imm32.dll")]
        public static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

        //キー設定取得用
        private NameValueCollection NekoKeyDef = new NameValueCollection();

        //タッチ開始座標
        private double StartPointX;
        private double StartPointY;

        //ホイールパッド関連
        private double PadPointX;
        private double PadPointY;
        private bool PadActivate = false;

        //フリック判定範囲
        private int FrickRange;
        private int FrickCancel;

        //フリック時に表示するポップアップ用画像設定
        BitmapImage PopImageC = new BitmapImage(new Uri("Resources/maskb.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageU = new BitmapImage(new Uri("Resources/maskU.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageD = new BitmapImage(new Uri("Resources/maskD.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageL = new BitmapImage(new Uri("Resources/maskL.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageR = new BitmapImage(new Uri("Resources/maskR.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageCancel = new BitmapImage(new Uri("Resources/maskCancel.png", UriKind.RelativeOrAbsolute));

        //コントロール件数
        int ButtonCnt;
        int PadCnt;
        //コントロール配列
        FrickButton[] ButtonArray;
        FrickPad[] PadArray;
        //修飾キー用ボタンは別枠で作成
        FrickButton ModButton;
        BitmapImage ModBmp = new BitmapImage(new Uri("Resources/PadBase.bmp", UriKind.RelativeOrAbsolute));

        //設定中フラグ
        bool SettingFlg = false;

        //ウィンドウ幅(仮)
        int WinW = 352;

        //初期化
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 前バージョンからのUpgradeを実行していないときは、Upgradeを実施する
            if (Properties.Settings.Default.IsUpgrade == false)
            {
                // Upgradeを実行する
                Properties.Settings.Default.Upgrade();

                // 「Upgradeを実行した」という情報を設定する
                Properties.Settings.Default.IsUpgrade = true;

                // 現行バージョンの設定を保存する
                Properties.Settings.Default.Save();
            }
            //ブラシ設定てすとーーーーーーーーーーーーーーーーーーーー
//            var brsprm = Properties.Settings.Default.brsPrm;
//            Clipboard.SetDataObject(brsprm);

            KeyConfig SetKey = new KeyConfig(); 

            //IO系クラス
            neConfigIO necIO = new neConfigIO();
            //猫ペイント本体ディレクトリ
            string NpDir = "";
            //割り当て済みショートカット管理リスト
            //KEY：ID　VAL：ショートカット
            Dictionary<int, string> Key_txt = new Dictionary<int, string>();
            //割り当て済みスクリプト管理リスト
            //KEY：スクリプト名　VAL：ショートカットID
            Dictionary<string, int> List_txt = new Dictionary<string, int>();

            //アクティブにしないおまじない
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);

            //ウィンドウメッセージ取得用
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(new HwndSourceHook(WndProc));

            //フリック範囲取得
            FrickRange = Properties.Settings.Default.FrickRange;
            FrickCancel = Properties.Settings.Default.CancelRange;
            #region 起動時チェックなど
            //二重起動確認
            var name = this.GetType().Assembly.GetName().Name;
            if (System.Diagnostics.Process.GetProcessesByName(name).Length > 1)
            {
                //二重起動禁止
                MessageBox.Show("二重起動はできません", "エラー");
                Close();
                return;
            }            //起動ディレクトリ設定
#if true
            NpDir = necIO.GetNpDir();
            if (NpDir == "")
            {
                MessageBox.Show("ネコペイント本体が見つかりません。NekoFrickを終了します", "エラー");
                Close();
                return;
            }
#endif
            #endregion
            //ネコペkey.txt取得
            necIO.ReadKey_txt(NpDir, Key_txt);
            //スクリプトリスト取得
            necIO.ReadScriptList(NpDir, "user_list.txt", Key_txt, List_txt);
            necIO.ReadScriptList(NpDir, "list.txt", Key_txt, List_txt);

            #region //各ボタンの表示テキスト・キー割り当て設定
            //ボタン数・ホイールパッド数取得
            ButtonCnt = Properties.Settings.Default.ButtonCnt;
            PadCnt = Properties.Settings.Default.PadCnt;
            //通常ボタン配列作成
            ButtonArray = new FrickButton[ButtonCnt];
            int X = 0;
            int Y = 0;
            for (int i = 0; i < ButtonCnt; i++)
            {
                //とりあえず仮で横方向ボタン三個で折り返し
                if (i != 0 && i % 3 == 0)
                {
                    X = 0;
                    Y++;
                }
                ButtonArray[i] = new FrickButton();
                FrickGrid.Children.Add(ButtonArray[i]);
                ButtonArray[i].Margin = new Thickness(64 * X, Y * 64, 0, 0);

                ButtonArray[i].StylusDown += FrickButton_StylusDown;
                ButtonArray[i].MouseMove += FrickButton_MouseMove;
                ButtonArray[i].MouseUp += FrickButton_MouseUp;
                ButtonArray[i].BtnTxt.Text = Properties.Settings.Default.BtnTXT[i];
                X++;
            }
            SetKey.SetKeyText(ButtonArray, Properties.Settings.Default.KeyStr, ButtonCnt);
//            SetKey.SetKeyCD(ButtonArray, Properties.Settings.Default.KeyCD, ButtonCnt);
            SetKey.SetKeyCD(ButtonArray, Properties.Settings.Default.KeyCDNP, ButtonCnt, Key_txt);

            //修飾キー用ボタン作成
            ModButton = new FrickButton();
            FrickGrid.Children.Add(ModButton);
            ModButton.Margin = new Thickness(64 * X, Y * 64, 0, 0);
            ModButton.BtnTxt.Text = "修飾キー";
            ModButton.BtnImg.Source = ModBmp;
            ModButton.StylusDown += FrickButton_StylusDown;
            ModButton.MouseMove += FrickButton_MouseMove;
            ModButton.MouseUp += FrickButton_MouseUp;
            ModButton.ModFlg = true;
            for (int i = 0; i < 5; i++)
            {
                //修飾キーに未設定はないためポップアップは無条件で白背景に
                ModButton.PopText[i].Background = new SolidColorBrush(Colors.White);
            }

            SetKey.SetKeyText(ModButton, Properties.Settings.Default.ModStr);
            SetKey.SetModKeyCD(ModButton.KeyAssign, Properties.Settings.Default.ModCD);
            //ホイールパッド配列作成
            PadArray = new FrickPad[PadCnt];
            for (int i = 0; i < PadCnt; i++)
            {
                PadArray[i] = new FrickPad();
                FrickGrid.Children.Add(PadArray[i]);
                PadArray[i].HorizontalAlignment = HorizontalAlignment.Right;
                PadArray[i].Margin = new Thickness(0, 0, 128 * i, 0);

                PadArray[i].PadBtn.StylusDown += FrickButton_StylusDown;
                PadArray[i].PadBtn.MouseMove += FrickButton_MouseMove;
                PadArray[i].PadBtn.MouseUp += FrickButtonP_MouseUp;

                PadArray[i].StylusDown += Pad_StylusDown;
                PadArray[i].MouseMove += Pad_MouseMove;
                PadArray[i].MouseUp += Pad_MouseUp;

                SetKey.SetKeyText(PadArray[i].PadBtn, Properties.Settings.Default.padStr);
//                SetKey.SetKeyCD(PadArray[i].PadBtn.KeyAsign, Properties.Settings.Default.padCD);
//                SetKey.SetKeyCD(PadArray[i].KeyAsignR, Properties.Settings.Default.padCDR);
                PadArray[i].PadBtn.BtnTxt.Text = PadArray[i].PadBtn.PopTextC.Text;
            }
            SetKey.SetKeyCD(PadArray, Properties.Settings.Default.padCDNP, PadCnt, Key_txt, List_txt);
            #endregion
#if true
            //猫ペイント本体ディレクトリ
            UserSettings.Instance.NpDir = NpDir;
            //割り当て済みショートカット管理リスト
            //KEY：ID　VAL：ショートカット
            UserSettings.Instance.Key_txt = Key_txt;
            //割り当て済みスクリプト管理リスト
            //KEY：スクリプト名　VAL：ショートカットID
            UserSettings.Instance.List_txt = List_txt;
#endif
        }
        //ウインドウメッセージ処理
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int WM_LBUTTONDOWN = 0x0201;
            //const int MA_NOACTIVATE = 3;
            switch (msg)
            {
                case WM_MOUSEACTIVATE: // 処理するメッセージ
                    //handled = true; // ここでメッセージを止める場合はtrue
                    //return new IntPtr(MA_NOACTIVATE); // メッセージや処理によって適切な値を返す
                    break;
                case WM_LBUTTONDOWN: // 処理するメッセージ
                    //ボタン上にカーソルがある場合
                    foreach (var target in ButtonArray)
                    {
                        if (target.IsMouseOver == true)
                        {
                            if (Stylus.CurrentStylusDevice == null)
                            {
                                FrickButton_FirstTouch(target);
                            }
                            //ここでメッセージを止めないとDragMoveに食われる
                            handled = true; // ここでメッセージを止める場合はtrue
                            break;
                        }
                    }
                    //修飾キー用ボタン上にカーソルがある場合
                    if (ModButton.IsMouseOver == true)
                    {
                        if (Stylus.CurrentStylusDevice == null)
                        {
                            FrickButton_FirstTouch(ModButton);
                        }
                        //ここでメッセージを止めないとDragMoveに食われる
                        handled = true; // ここでメッセージを止める場合はtrue
                        break;
                    }
                    //パッド上にカーソルがある場合
                    foreach (var target in PadArray)
                    {
                        if (target.PadBtn.IsMouseOver == true)
                        {
                            if (Stylus.CurrentStylusDevice == null)
                            {
                                FrickButton_FirstTouch(target.PadBtn);
                            }
                            //ここでメッセージを止めないとDragMoveに食われる
                            handled = true; // ここでメッセージを止める場合はtrue
                            break;
                        }
                        if (target.IsMouseOver == true)
                        {
                            if (Stylus.CurrentStylusDevice == null)
                            {
                                Pad_FirstTouch(target);
                            }
                            //ここでメッセージを止めないとDragMoveに食われる
                            handled = true; // ここでメッセージを止める場合はtrue
                            break;
                        }
                    }
                    //                   return new IntPtr(MA_NOACTIVATE); // メッセージや処理によって適切な値を返す
                    break;
            }
            return IntPtr.Zero;
        }

        //空き地ドラッグで移動できるようにするおまじない
        private void nFrick_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        //閉じるボタン
        private void close_Click(object sender, RoutedEventArgs e)
        {
            nFrick.Close();
        }
        //設定ボタン
        private void setting_Click(object sender, RoutedEventArgs e)
        {
            //未実装
            if (!SettingFlg)
            {
                //設定モードに移行
                setting.Content = "Resources/settingON.bmp";
                SettingFlg = true;
                //最小化中の場合は元に戻す
                if (FrickGrid.Visibility == System.Windows.Visibility.Hidden)
                {
                    FrickGrid.Visibility = System.Windows.Visibility.Visible;
                    min.Content = "Resources/min.bmp";
                    this.Width = WinW;
                }
                //最小化ボタンを無効化
                min.IsEnabled = false;
                ButtonSetting testset = new ButtonSetting();
                testset.ButtonArray = ButtonArray;
                testset.ShowDialog();
            }
            else
            {
                //通常モードに移行
                setting.Content = "Resources/setting.bmp";
                SettingFlg = false;
                //最小化ボタンを有効化
                min.IsEnabled = true;
#if false //設定反映お試しコード
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
            }
        }
        //最小化・元に戻すボタン
        private void min_Click(object sender, RoutedEventArgs e)
        {
            if (FrickGrid.Visibility == System.Windows.Visibility.Hidden)
            {
                FrickGrid.Visibility = System.Windows.Visibility.Visible;
                min.Content = "Resources/min.bmp";
                this.Width = WinW;
            }
            else
            {
                FrickGrid.Visibility = System.Windows.Visibility.Hidden;
                min.Content = "Resources/max.bmp";
                this.Width = 32;
            }
        }
        #region 各種イベントユーザーコントロール化
        //MouseDownイベントはスタイラスでタップした場合のタイムラグが解消できなかったため不採用

        /// <summary>
        /// フリック処理開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrickButton_StylusDown(object sender, StylusDownEventArgs e)
        {
            var target = (FrickButton)sender;
            Stylus.Capture(target);
            Point pos = e.GetPosition(target);
            StartPointX = pos.X;
            StartPointY = pos.Y;
            target.FrickPopImage.Source = PopImageC;
//            target.FrickPop.PlacementTarget = target;
            target.FrickPop.IsOpen = true;
            e.Handled = true;
        }
        /// <summary>
        /// スタイラスダウンの代わり
        /// アクティブなソフトによってはスタイラスイベントが発生しない場合があるので
        /// その場合に限りウィンドウメッセージを拾ってこっちを呼び出す
        /// </summary>
        /// <param name="TargetButton">処理対象偽ボタンコントロール</param>
        private void FrickButton_FirstTouch(FrickButton TargetButton)
        {
            MouseDevice myStylusDevice = Mouse.PrimaryDevice;
            Stylus.Capture(TargetButton);
            Point pos = myStylusDevice.GetPosition(TargetButton);
            StartPointX = pos.X;
            StartPointY = pos.Y;
            TargetButton.FrickPopImage.Source = PopImageC;

            TargetButton.FrickPop.IsOpen = true;
        }
        
        /// <summary>
        /// フリック中のポップアップ表示処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrickButton_MouseMove(object sender, MouseEventArgs e)
        {
            var target = (FrickButton)sender;
            if (target.FrickPop.IsOpen == true)
            {
                double difX, difY;
                Point pos = e.GetPosition(target);
                difX = StartPointX - pos.X;
                difY = StartPointY - pos.Y;
                if (System.Math.Abs(difX) < System.Math.Abs(difY))
                {
                    if (System.Math.Abs(difY) < FrickRange)
                    {
                        target.FrickPopImage.Source = PopImageC;
                    }
                    else if (System.Math.Abs(difY) > FrickCancel)
                    {
                        //キャンセル
                        target.FrickPopImage.Source = PopImageCancel;
                    }
                    else if (difY > 0)
                    {
                        target.FrickPopImage.Source = PopImageU;
                    }
                    else
                    {
                        target.FrickPopImage.Source = PopImageD;
                    }
                }
                else
                {
                    if (System.Math.Abs(difX) < FrickRange)
                    {
                        target.FrickPopImage.Source = PopImageC;
                    }
                    else if (System.Math.Abs(difX) > FrickCancel)
                    {
                        //キャンセル
                        target.FrickPopImage.Source = PopImageCancel;
                    }
                    else if (difX > 0)
                    {
                        target.FrickPopImage.Source = PopImageL;
                    }
                    else
                    {
                        target.FrickPopImage.Source = PopImageR;
                    }
                }
            }
        }

        /// <summary>
        /// フリック確定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrickButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var target = (FrickButton)sender;
            double difX, difY;
            SendKeyCode send = new SendKeyCode();
            Point pos = e.GetPosition(target);
            difX = StartPointX - pos.X;
            difY = StartPointY - pos.Y;

            // アクティブなウィンドウハンドルの取得
            IntPtr hWnd = GetForegroundWindow();
            //IMEハンドルの取得
            IntPtr hIMC = ImmGetDefaultIMEWnd(hWnd);
            //IMEをOFF
            SendMessage(hIMC, WM_IME_CONTROL, IMC_SETOPENSTATUS, 0);

            if (System.Math.Abs(difX) < System.Math.Abs(difY))
            {
                if (System.Math.Abs(difY) < FrickRange)
                {
                    //フリックなし
                    send.Sendkey(target.KeyAssign[0],target.ModFlg);
                }
                else if (System.Math.Abs(difY) > FrickCancel)
                {
                    //処理なし
                }
                else if (difY > 0)
                {
                    //上フリック
                    send.Sendkey(target.KeyAssign[1], target.ModFlg);
                }
                else
                {
                    //下フリック
                    send.Sendkey(target.KeyAssign[2], target.ModFlg);
                }
            }
            else
            {
                if (System.Math.Abs(difX) < FrickRange)
                {
                    //フリックなし
                    send.Sendkey(target.KeyAssign[0], target.ModFlg);
                }
                else if (System.Math.Abs(difX) > FrickCancel)
                {
                    //処理なし
                }
                else if (difX > 0)
                {
                    //左フリック
                    send.Sendkey(target.KeyAssign[3], target.ModFlg);
                }
                else
                {
                    //右フリック
                    send.Sendkey(target.KeyAssign[4], target.ModFlg);
                }
            }
            Mouse.Capture(null);
            System.Threading.Thread.Sleep(100);
            target.FrickPop.IsOpen = false;
        }
        /// <summary>
        /// ホイールパッド切り替えボタン用イベント
        /// フリック開始とフリック中は通常ボタンと同様
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrickButtonP_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var target = (FrickButton)sender;
            var Pad = (FrickPad)target.ParentPad;
            
            double difX, difY;
            Point pos = e.GetPosition(target);
            difX = StartPointX - pos.X;
            difY = StartPointY - pos.Y;

            if (System.Math.Abs(difX) < System.Math.Abs(difY))
            {
                if (System.Math.Abs(difY) < FrickRange)
                {
                    //フリックなし
                    Pad.FuncID = 0;
                }
                else if (System.Math.Abs(difY) > FrickCancel)
                {
                    //処理なし
                }
                else if (difY > 0)
                {
                    //上フリック
                    Pad.FuncID = 1;
                }
                else
                {
                    //下フリック
                    Pad.FuncID = 2;
                }
            }
            else
            {
                if (System.Math.Abs(difX) < FrickRange)
                {
                    //フリックなし
                    Pad.FuncID = 0;
                }
                else if (System.Math.Abs(difX) > FrickCancel)
                {
                    //処理なし
                }
                else if (difX > 0)
                {
                    //左フリック
                    Pad.FuncID = 3;
                }
                else
                {
                    //右フリック
                    Pad.FuncID = 4;
                }
            }
            target.BtnTxt.Text = target.PopText[Pad.FuncID].Text;
            Mouse.Capture(null);
            target.FrickPop.IsOpen = false;
        }

        /// <summary>
        /// ホイールパッド開始イベント
        /// 開始座標とパッド中心座標の設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pad_StylusDown(object sender, StylusDownEventArgs e)
        {
            var target = (FrickPad)sender;
            Stylus.Capture(target);
            Point pos = e.GetPosition(target);
            PadPointX = target.RenderSize.Width / 2;
            PadPointY = target.RenderSize.Height / 2;
            StartPointX = pos.X - PadPointX;
            StartPointY = pos.Y - PadPointY;
            target.PadImg.Opacity = 0.8;
            PadActivate = true;
        }

        /// <summary>
        /// ホイールパッド用スタイラスダウンの代わり
        /// </summary>
        /// <param name="TargetButton"></param>
        private void Pad_FirstTouch(FrickPad target)
        {
            MouseDevice myStylusDevice = Mouse.PrimaryDevice;
            Stylus.Capture(target);
            Point pos = myStylusDevice.GetPosition(target);
            Stylus.Capture(target);
            PadPointX = target.RenderSize.Width / 2;
            PadPointY = target.RenderSize.Height / 2;
            StartPointX = pos.X - PadPointX;
            StartPointY = pos.Y - PadPointY;
            target.PadImg.Opacity = 0.8;
            PadActivate = true;
        }

        /// <summary>
        /// ホイールパッド回転イベント
        /// 一定角度ごとにイベントを送信して開始位置をリセット
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pad_MouseMove(object sender, MouseEventArgs e)
        {
            //パッド使用中のみ処理
            if (PadActivate == true)
            {
                var target = (FrickPad)sender;
                double ang;
                double x, y;
                SendKeyCode send = new SendKeyCode();
                SendMouseCode mouse = new SendMouseCode();

                Point pos = e.GetPosition(target);
                x = pos.X - PadPointX;
                y = pos.Y - PadPointY;
                ang = Math.Atan2(
                    StartPointX * y - StartPointY * x,
                    StartPointX * x + StartPointY * y
                    );
                if (ang > target.sens[target.FuncID])
                {
                    StartPointX = x;
                    StartPointY = y;
                    //キー設定がない場合は無視
                    if (target.PadBtn.KeyAssign[target.FuncID].Count() == 0)
                    {
                    }
                    else if (target.PadBtn.KeyAssign[target.FuncID][0] == 999)
                    {
                        mouse.Sendwheel(40);
                    }
                    else
                    {
                        send.Sendkey(target.PadBtn.KeyAssign[target.FuncID],false);
                    }
                }
                if (ang < -target.sens[target.FuncID])
                {
                    StartPointX = x;
                    StartPointY = y;
                    //キー設定がない場合は無視
                    if (target.KeyAsignR[target.FuncID].Count() == 0)
                    {
                    }
                    else if (target.KeyAsignR[target.FuncID][0] == 999)
                    {
                        mouse.Sendwheel(-40);
                    }
                    else
                    {
                        send.Sendkey(target.KeyAsignR[target.FuncID], false);
                    }
                }
            }
        }

        /// <summary>
        /// ホイールパッド終了イベント
        /// キャプチャの開放のみ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pad_MouseUp(object sender, MouseButtonEventArgs e)
        {
            FrickPad tmp = sender as FrickPad;
            tmp.PadImg.Opacity = 0.5;
            Mouse.Capture(null);
            PadActivate = false;
        }

        #endregion
    }

    //設定ファイルから色々取得
    class KeyConfig
    {
        //キー定義
        private NameValueCollection NekoKeyDef = new NameValueCollection();

        public KeyConfig()
        {
            //キー定義取得
            int div = 0;
            string key, val;
            foreach (string str in Properties.Settings.Default.KeyDef)
            {
                div = str.IndexOf(' ');
                if (div < 0)
                {
                    continue;
                }
                key = str.Substring(0, div);
                val = str.Substring(div + 1);
                NekoKeyDef.Add(key, val);
            }
        }

        //ポップアップに表示するテキスト設定
        public void SetKeyText(FrickButton[] TargetButton, StringCollection key_str, int Cnt)
        {
            for(int i = 0;i < Cnt;i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    TargetButton[i].PopText[j].Text = key_str[i * 5 + j];
                }
            }
        }
        public void SetKeyText(FrickButton TargetButton, StringCollection key_str)
        {
            for (int i = 0; i < 5; i++)
            {
                TargetButton.PopText[i].Text = key_str[i];
            }
        }

        //各フリックに送信するキーコード設定
        public void SetKeyCD(int[][] KeyAsign, StringCollection key_str)
        {
            for (int i = 0; i < 5; i++)
            {
                string[] Split;
                int[] KeyArray;
                int j = 0;

                //入力を空白文字で分割
                Split = key_str[i].Split();
                KeyArray = new int[Split.Length];
                foreach (string str in Split)
                {
                    if (str.Length == 0)
                    {
                        continue;
                    }
                    else if (str.Length == 1)
                    {
                        if (str == "+")
                        {
                            //単一の+は無視
                            continue;
                        }
                        else
                        {
                            try
                            {
                                //キー設定
                                KeyArray[j] = Convert.ToByte(str[0]);
                            }
                            catch
                            {
                                //2byteとか特殊文字対策
                                //キー定義から検索して設定
                                KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                            }
                        }
                    }
                    else
                    {
                        //キー定義から検索して設定
                        KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                    }
                    j++;
                }
                KeyAsign[i] = new int[j * 2];
                for (int k = 0; k < j; k++)
                {
                    KeyAsign[i][k] = KeyArray[k];
                    //キーアップは逆順にマイナス値を設定
                    KeyAsign[i][k + j] = -KeyArray[j - k - 1];
                }
            }
        }
        public void SetKeyCD(FrickButton[] TargetButton, StringCollection key_str, int Cnt)
        {
            for (int m = 0; m < Cnt; m++)
            {
                for (int i = 0; i < 5; i++)
                {
                    string[] Split;
                    int[] KeyArray;
                    int j = 0;

                    //入力を空白文字で分割
                    Split = key_str[i + m * 5].Split();
                    KeyArray = new int[Split.Length];
                    foreach (string str in Split)
                    {
                        if (str.Length == 0)
                        {
                            continue;
                        }
                        else if (str.Length == 1)
                        {
                            if (str == "+")
                            {
                                //単一の+は無視
                                continue;
                            }
                            else
                            {
                                try
                                {
                                    //キー設定
                                    KeyArray[j] = Convert.ToByte(str[0]);
                                }
                                catch
                                {
                                    //2byteとか特殊文字対策
                                    //キー定義から検索して設定
                                    KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                                }
                            }
                        }
                        else
                        {
                            //キー定義から検索して設定
                            KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                        }
                        j++;
                    }
                    TargetButton[m].KeyAssign[i] = new int[j * 2];
                    for (int k = 0; k < j; k++)
                    {
                        TargetButton[m].KeyAssign[i][k] = KeyArray[k];
                        //キーアップは逆順にマイナス値を設定
                        TargetButton[m].KeyAssign[i][k + j] = -KeyArray[j - k - 1];
                    }
                }
            }
        }
        public void SetKeyCD(FrickButton[] TargetButton, StringCollection key_str, int Cnt, Dictionary<int, string> AllAssign)
        {
            for (int m = 0; m < Cnt; m++)
            {
                for (int i = 0; i < 5; i++)
                {
                    string[] Split;
                    int[] KeyArray;
                    int j = 0;
                    int ID = Convert.ToInt16(key_str[i + m * 5]);
                    if (!AllAssign.ContainsKey(ID))
                    {
                        //ショートカットが設定されていない場合
                        TargetButton[m].KeyAssign[i] = new int[0];
                        SolidColorBrush brs = new SolidColorBrush(Colors.Red);
                        TargetButton[m].PopText[i].Foreground = brs;

                        continue;
                    }
                    TargetButton[m].PopText[i].Background = new SolidColorBrush(Colors.White);
                    string conv = AllAssign[ID];
                    //入力を空白文字で分割
                    Split = conv.Split();
                    KeyArray = new int[Split.Length];
                    foreach (string str in Split)
                    {
                        if (str.Length == 0)
                        {
                            continue;
                        }
                        else if (str.Length == 1)
                        {
                            if (str == "+")
                            {
                                //単一の+は無視
                                continue;
                            }
                            else
                            {
                                try
                                {
                                    //キー設定
                                    KeyArray[j] = Convert.ToByte(str[0]);
                                }
                                catch
                                {
                                    //2byteとか特殊文字対策
                                    //キー定義から検索して設定
                                    KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                                }
                            }
                        }
                        else
                        {
                            //キー定義から検索して設定
                            KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                        }
                        j++;
                    }
                    TargetButton[m].KeyAssign[i] = new int[j * 2];
                    for (int k = 0; k < j; k++)
                    {
                        TargetButton[m].KeyAssign[i][k] = KeyArray[k];
                        //キーアップは逆順にマイナス値を設定
                        TargetButton[m].KeyAssign[i][k + j] = -KeyArray[j - k - 1];
                    }
                }
            }
        }
        //ホイール用（マクロ専用）
        public void SetKeyCD(FrickPad[] TargetButton, StringCollection key_str, int Cnt, Dictionary<int, string> Key_txt, Dictionary<string, int> List_txt)
        {
            for (int m = 0; m < Cnt; m++)
            {
                for (int i = 0; i < 5; i++)
                {
                    string[] Split, SplitR;
                    int[] KeyArray, KeyArrayR;
                    int j = 0;
                    if (i * 2 + m * 10 >= key_str.Count
                        || key_str[i * 2 + m * 10] == ""
                        || key_str[i * 2 + 1 + m * 10] == "")
                    {
                        //設定が足りない場合または
                        //順回転、逆回転どちらかでも未入力ならホイールとみなす
                        TargetButton[m].PadBtn.KeyAssign[i] = new int[1];
                        TargetButton[m].KeyAsignR[i] = new int[1];
                        TargetButton[m].PadBtn.KeyAssign[i][0] = 999;
                        TargetButton[m].KeyAsignR[i][0] = 999;
                        //ポップアップ背景を割り当てなし→通常に変更
                        TargetButton[m].PadBtn.PopText[i].Background = new SolidColorBrush(Colors.White);
                        continue;
                    }
                    if (!List_txt.ContainsKey(key_str[i * 2 + m * 10]) 
                        || !List_txt.ContainsKey(key_str[i * 2 + 1 + m * 10]))
                    {
                        //順回転、逆回転どちらかでもスクリプトが登録されていない場合
                        TargetButton[m].PadBtn.KeyAssign[i] = new int[0];
                        TargetButton[m].KeyAsignR[i] = new int[0];
                        SolidColorBrush brs = new SolidColorBrush(Colors.Red);
                        //brs.Opacity = 0.5;
                        TargetButton[m].PadBtn.PopText[i].Foreground = brs;
                        continue;
                    }
                    int ID = List_txt[key_str[i * 2 + m * 10]];
                    int IDR = List_txt[key_str[i * 2 + 1 + m * 10]];
                    if (!Key_txt.ContainsKey(ID) || !Key_txt.ContainsKey(IDR))
                    {
                        //順回転、逆回転どちらかでもショートカットが設定されていない場合
                        TargetButton[m].PadBtn.KeyAssign[i] = new int[0];
                        TargetButton[m].KeyAsignR[i] = new int[0];
                        SolidColorBrush brs = new SolidColorBrush(Colors.Red);
                        //brs.Opacity = 0.5;
                        TargetButton[m].PadBtn.PopText[i].Foreground = brs;
                        continue;
                    }
                    //ポップアップ背景を割り当てなし→通常に変更
                    TargetButton[m].PadBtn.PopText[i].Background = new SolidColorBrush(Colors.White);
                    //入力を空白文字で分割
                    Split = Key_txt[ID].Split();
                    KeyArray = new int[Split.Length];
                    foreach (string str in Split)
                    {
                        if (str.Length == 0)
                        {
                            continue;
                        }
                        else if (str.Length == 1)
                        {
                            if (str == "+")
                            {
                                //単一の+は無視
                                continue;
                            }
                            else
                            {
                                try
                                {
                                    //キー設定
                                    KeyArray[j] = Convert.ToByte(str[0]);
                                }
                                catch
                                {
                                    //2byteとか特殊文字対策
                                    //キー定義から検索して設定
                                    KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                                }
                            }
                        }
                        else
                        {
                            //キー定義から検索して設定
                            KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                        }
                        j++;
                    }
                    TargetButton[m].PadBtn.KeyAssign[i] = new int[j * 2];
                    for (int k = 0; k < j; k++)
                    {
                        TargetButton[m].PadBtn.KeyAssign[i][k] = KeyArray[k];
                        //キーアップは逆順にマイナス値を設定
                        TargetButton[m].PadBtn.KeyAssign[i][k + j] = -KeyArray[j - k - 1];
                    }
                    j=0;
                    SplitR = Key_txt[IDR].Split();
                    KeyArrayR = new int[SplitR.Length];
                    foreach (string str in SplitR)
                    {
                        if (str.Length == 0)
                        {
                            continue;
                        }
                        else if (str.Length == 1)
                        {
                            if (str == "+")
                            {
                                //単一の+は無視
                                continue;
                            }
                            else
                            {
                                try
                                {
                                    //キー設定
                                    KeyArrayR[j] = Convert.ToByte(str[0]);
                                }
                                catch
                                {
                                    //2byteとか特殊文字対策
                                    //キー定義から検索して設定
                                    KeyArrayR[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                                }
                            }
                        }
                        else
                        {
                            //キー定義から検索して設定
                            KeyArrayR[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                        }
                        j++;
                    }
                    TargetButton[m].KeyAsignR[i] = new int[j * 2];
                    for (int k = 0; k < j; k++)
                    {
                        TargetButton[m].KeyAsignR[i][k] = KeyArrayR[k];
                        //キーアップは逆順にマイナス値を設定
                        TargetButton[m].KeyAsignR[i][k + j] = -KeyArrayR[j - k - 1];
                    }
                }
            }
        }

        //修飾キー用
        public void SetModKeyCD(int[][] KeyAsign, StringCollection key_str)
        {
            //合計キー件数
            int KeyCnt = 0;
            //0件目（センター）はキャンセル専用とするため1～4件目を処理する
            for (int i = 1; i < 5; i++)
            {
                string[] Split;
                int[] KeyArray;
                int j = 0;

                //入力を空白文字で分割
                Split = key_str[i].Split();
                KeyArray = new int[Split.Length];
                foreach (string str in Split)
                {
                    if (str.Length == 0)
                    {
                        continue;
                    }
                    else if (str.Length == 1)
                    {
                        if (str == "+")
                        {
                            //単一の+は無視
                            continue;
                        }
                        else
                        {
                            try
                            {
                                //キー設定
                                KeyArray[j] = Convert.ToByte(str[0]);
                            }
                            catch
                            {
                                //2byteとか特殊文字対策
                                //キー定義から検索して設定
                                KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                            }
                        }
                    }
                    else
                    {
                        //キー定義から検索して設定
                        KeyArray[j] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                    }
                    j++;
                    KeyCnt++;
                }
                KeyAsign[i] = new int[j];
                for (int k = 0; k < j; k++)
                {
                    KeyAsign[i][k] = KeyArray[k];
                }
            }
            //センターの領域確保
            KeyAsign[0] = new int[KeyCnt];
            int cnt = 0;
            //設定済みのキーのキーアップをセンターに設定
            for (int i = 1; i < 5; i++)
            {
                foreach (int CD in KeyAsign[i])
                {
                    KeyAsign[0][cnt] = -CD;
                    cnt++;
                }
            }
        }
    }

    //キーボードイベント送信
    class SendKeyCode
    {
        [DllImport("user32.dll")]
        public static extern uint keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        //修飾キーコード
        const byte VK_SHIFT = 0x10;
        const byte VK_CONTROL = 0x11;
        const byte VK_MENU = 0x12;      //ALT
        const byte VK_LWIN = 0x5B;
        const byte VK_RWIN = 0x5C;
        const byte VK_APPS = 0x5D;
        
        //単純クリック用
        public void Sendkey(byte Code)
        {
            keybd_event(Code, 0, 0, (UIntPtr)0);
            keybd_event(Code, 0, 2, (UIntPtr)0);
        }
        //連続入力
        public void Sendkey(int[] Code,bool ModFlg)
        {
            bool ctrl = false, alt = false, shift = false;
            //修飾キー以外の場合押下中の修飾キーを解除し、最後に再押下する
            if (ModFlg == false)
            {
                if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Control)
                {
                    ctrl = true;
                    keybd_event(VK_CONTROL, 0, 2, (UIntPtr)0);
                }
                if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    shift = true;
                    keybd_event(VK_SHIFT, 0, 2, (UIntPtr)0);
                }
                if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Alt)
                {
                    alt = true;
                    keybd_event(VK_MENU, 0, 2, (UIntPtr)0);
                }
            }
            foreach (int cd in Code)
            {
                if(cd > 0){
                    //正ならキーダウン
                    keybd_event((byte)cd, 0, 0, (UIntPtr)0);
                }
                else{
                    //負ならキーアップ
                    keybd_event((byte)-cd, 0, 2, (UIntPtr)0);
                }
            }
            if (ModFlg == false)
            {
                if (ctrl == true)
                {
                    keybd_event(VK_CONTROL, 0, 0, (UIntPtr)0);
                }
                if (shift == true)
                {
                    keybd_event(VK_SHIFT, 0, 0, (UIntPtr)0);
                }
                if (alt == true)
                {
                    keybd_event(VK_MENU, 0, 0, (UIntPtr)0);
                }
            }
        }
        //キーダウン、アップ独立用
        public void Sendkey(byte Code, byte UD)
        {
            keybd_event(Code, 0, UD, (UIntPtr)0);
        }
    }

    //マウスイベント送信
    class SendMouseCode
    {
        [DllImport("user32.dll")]
        public static extern void mouse_event(
          int dwFlags,         // 移動とクリックのオプション
          int dx,              // 水平位置または移動量
          int dy,              // 垂直位置または移動量
          int dwData,          // ホイールの移動
          UIntPtr dwExtraInfo  // アプリケーション定義の情報
        );

        //マウスイベントコード
        const int MOUSEEVENTF_MOVED = 0x0001; // 移動
        const int MOUSEEVENTF_LEFTDOWN = 0x0002; // 左ボタン Down
        const int MOUSEEVENTF_LEFTUP = 0x0004; // 左ボタン Up
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008; // 右ボタン Down
        const int MOUSEEVENTF_RIGHTUP = 0x0010; // 右ボタン Up
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020; // 中ボタン Down
        const int MOUSEEVENTF_MIDDLEUP = 0x0040; // 中ボタン Up
        const int MOUSEEVENTF_WHEEL = 0x0800; // ホイール動作
        const int MOUSEEVENTF_XDOWN = 0x0100;
        const int MOUSEEVENTF_XUP = 0x0200;
        const int MOUSEEVENTF_ABSOLUTE = 0x8000; // 絶対座標

        //ホイール回転用
        public void Sendwheel(int move)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, move, (UIntPtr)0);
        }
    }

}
