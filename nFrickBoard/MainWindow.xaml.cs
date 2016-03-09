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
using System.IO;

namespace nFrickBoard
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>

    public partial class MainWindow : Window
    {
        //修飾キーコード
        const byte VK_SHIFT = 0x10;
        const byte VK_CONTROL = 0x11;
        const byte VK_MENU = 0x12;      //ALT

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

        //タッチ開始座標
        private double StartPointX;
        private double StartPointY;

        //ホイールパッド関連
        private double PadPointX;
        private double PadPointY;
        private bool PadActivate = false;

        //フリック時に表示するポップアップ用画像設定
        BitmapImage PopImageC = new BitmapImage(new Uri("Resources/maskb.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageU = new BitmapImage(new Uri("Resources/maskU.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageD = new BitmapImage(new Uri("Resources/maskD.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageL = new BitmapImage(new Uri("Resources/maskL.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageR = new BitmapImage(new Uri("Resources/maskR.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageCancel = new BitmapImage(new Uri("Resources/maskCancel.png", UriKind.RelativeOrAbsolute));

        //色変更用ブラシ
        SolidColorBrush BrsW = new SolidColorBrush(Colors.White);
        SolidColorBrush BrsR = new SolidColorBrush(Colors.Red);
        SolidColorBrush BrsY = new SolidColorBrush(Colors.Yellow);
        SolidColorBrush BrsBK = new SolidColorBrush(Colors.Black);

        //コントロール配列
        FrickButton[] ButtonArray;
        FrickPad[] PadArray;
        //修飾キー用ボタンは別枠で作成
//        FrickButton ModButton;
        BitmapImage ModBmp = new BitmapImage(new Uri("Resources/PadBase.bmp", UriKind.RelativeOrAbsolute));

        //設定中フラグ
        bool SettingFlg = false;

        //ウィンドウ幅(仮)
        int WinW = 1000;
        int WinH = 1000;

        //初期化
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
#if false //アップグレードチェック
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
#endif
            //ブラシ設定てすとーーーーーーーーーーーーーーーーーーーー
//            var brsprm = Properties.Settings.Default.brsPrm;
//            Clipboard.SetDataObject(brsprm);

            //ユーザー設定ファイルロード
            UserSettings.LoadSetting();

            this.Width = WinW;
            this.Height = WinH;

            KeyConfig SetKey = new KeyConfig(); 

            //ネコペイント関連IO系クラス
            neConfigIO necIO = new neConfigIO();

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

            #region 起動時チェックなど
            //二重起動確認
            var name = this.GetType().Assembly.GetName().Name;
            if (System.Diagnostics.Process.GetProcessesByName(name).Length > 1)
            {
                //二重起動禁止
                MessageBox.Show("二重起動はできません", "エラー");
                Close();
                return;
            }
            #endregion
            #region 各種ユーザー設定取得
            //ネコペイント本体のディレクトリ取得
            GetNpDir();
            if (UserSettings.Instance.NpDir == "")
            {
                MessageBox.Show("ネコペイント本体が見つかりません。NekoFrickを終了します", "エラー");
                Close();
                return;
            }
            //フリック範囲取得
            if (UserSettings.Instance.FrickRange == 0)
            {
                //0の場合デフォルト値を取得
                UserSettings.Instance.FrickRange = Properties.Settings.Default.FrickRange;
            }
            if (UserSettings.Instance.FrickCancel == 0)
            {
                //0の場合デフォルト値を取得
                UserSettings.Instance.FrickCancel = Properties.Settings.Default.FrickCancel;
            }
            if (UserSettings.Instance.ObjSize == 0)
            {
                //0の場合デフォルト値を取得
                UserSettings.Instance.ObjSize = Properties.Settings.Default.ObjSize;
            }
            //ボタン数・ホイールパッド数取得
            if (UserSettings.Instance.ButtonCnt == 0 && UserSettings.Instance.PadCnt == 0)
            {
                //ボタン数、ホイール数ともに0の場合のみデフォルト値を取得
                //ボタンは修飾キー用に＋1しておく
                UserSettings.Instance.ButtonCnt = Properties.Settings.Default.ButtonCnt + 1;
                UserSettings.Instance.PadCnt = Properties.Settings.Default.PadCnt;

                #region ボタン設定のデフォルト値を取得
                //ボタン数分ユーザー設定に配列確保
                UserSettings.Instance.Button = new UserButton[UserSettings.Instance.ButtonCnt];
                int X = 0;
                int Y = 0;
                for (int i = 0; i < UserSettings.Instance.ButtonCnt; i++)
                {
                    //とりあえず仮で横方向ボタン三個で折り返し
                    if (i != 0 && i % 3 == 0)
                    {
                        X = 0;
                        Y++;
                    }
                    UserSettings.Instance.Button[i] = new UserButton();
                    UserSettings.Instance.Button[i].BtnTxt = Properties.Settings.Default.BtnTXT[i];
                    UserSettings.Instance.Button[i].ID = i;
                    for (int j = 0; j < (Constants.FRICK_WAY + 1); j++)
                    {
                        UserSettings.Instance.Button[i].KeyText[j] = Properties.Settings.Default.KeyStr[i * (Constants.FRICK_WAY + 1) + j];
                        UserSettings.Instance.Button[i].KeyAssign[j] = Properties.Settings.Default.KeyCDNP[i * (Constants.FRICK_WAY + 1) + j];
                    }
                    //最後の一件は修飾キー用ボタン
                    if (i == UserSettings.Instance.ButtonCnt - 1)
                    {
                        UserSettings.Instance.Button[i].kind = Constants.BTN_MOD;  //ボタン種類を修飾キーに設定
                    }
                    else
                    {
                        UserSettings.Instance.Button[i].kind = Constants.BTN_NORMAL;  //ボタン種類を通常に設定
                    }
                    //ボタン座標を設定
                    UserSettings.Instance.Button[i].X = X * UserSettings.Instance.ObjSize * 2;
                    UserSettings.Instance.Button[i].Y = Y * UserSettings.Instance.ObjSize * 2;
                    X++;
                }
                #endregion
                #region ホイール設定のデフォルト値を取得
                //ホイール数分ユーザー設定に配列確保
                UserSettings.Instance.Pad = new UserButton[UserSettings.Instance.PadCnt];
                for (int i = 0; i < UserSettings.Instance.PadCnt; i++)
                {
                    UserSettings.Instance.Pad[i] = new UserButton();
                    UserSettings.Instance.Pad[i].BtnTxt = Properties.Settings.Default.BtnTXT[i];
                    UserSettings.Instance.Pad[i].ID = i;
                    for (int j = 0; j < (Constants.FRICK_WAY + 1); j++)
                    {
                        UserSettings.Instance.Pad[i].KeyText[j] = Properties.Settings.Default.padStr[i * (Constants.FRICK_WAY + 1) + j];
//                        UserSettings.Instance.Pad[i].KeyAssign[j] = Properties.Settings.Default.padCDNP[i * (Constants.FRICK_WAY + 1) + j];
                    }
                    //ボタン座標を設定
                    UserSettings.Instance.Pad[i].X = X * UserSettings.Instance.ObjSize * 2;
                    UserSettings.Instance.Pad[i].Y = Y * UserSettings.Instance.ObjSize * 2;
                    X++;
                }
                #endregion
            }
            #endregion

            #region ネコペイント各種設定ファイル取得
            //ネコペkey.txt取得（ショートカット割り当て済みの機能のみ）
            necIO.ReadKey_txt(UserSettings.Instance.NpDir, Key_txt);
            //スクリプトリスト取得
            necIO.ReadScriptList(UserSettings.Instance.NpDir, Constants.NP_USER_LIST, Key_txt, List_txt);
            necIO.ReadScriptList(UserSettings.Instance.NpDir, Constants.NP_SCRIPT_LIST, Key_txt, List_txt);
            #endregion

            #region 各ボタンの表示テキスト・キー割り当て設定
            //ボタン配列作成
            ButtonArray = new FrickButton[UserSettings.Instance.ButtonCnt];
            for (int i = 0; i < UserSettings.Instance.ButtonCnt; i++)
            {
                //ボタン作成
                ButtonArray[i] = new FrickButton();
                FrickGrid.Children.Add(ButtonArray[i]);
                ButtonArray[i].Margin = new Thickness(UserSettings.Instance.Button[i].X, UserSettings.Instance.Button[i].Y, 0, 0);
                //                ButtonArray[i].BtnGrid.Height = 32;
                //                ButtonArray[i].Height = 128;
                //サイズ設定(縦横ともに基本サイズの2倍)
#if false
                ButtonArray[i].BtnImg.Height = UserSettings.Instance.ObjSize * 2;
                ButtonArray[i].BtnImg.Width = UserSettings.Instance.ObjSize * 2;
                ButtonArray[i].BtnTxt.Height = UserSettings.Instance.ObjSize * 2;
                ButtonArray[i].BtnTxt.Width = UserSettings.Instance.ObjSize * 2;
#endif
                ButtonArray[i].BtnGrid.Resources["ObjSize"] = (double)32;
                //イベント設定
                ButtonArray[i].StylusDown += FrickButton_StylusDown;
                ButtonArray[i].MouseMove += FrickButton_MouseMove;
                //通常ボタンと修飾キー用ボタンで分岐
                if (UserSettings.Instance.Button[i].kind == Constants.BTN_NORMAL)
                {
                    //通常ボタン
                    ButtonArray[i].MouseUp += FrickButton_MouseUp;
                }
                else
                {
                    //修飾キー用
                    ButtonArray[i].MouseUp += FrickButtonMod_MouseUp;
                    ButtonArray[i].ModFlg = true;
                    //見た目を修飾キー用に変更
                    ButtonArray[i].BtnImg.Source = ModBmp;
                    for (int j = 0; j <= Constants.FRICK_WAY; j++)
                    {
                        //修飾キーに未設定はないためポップアップは無条件で白背景に
                        ButtonArray[i].PopText[j].Background = BrsW;
                    }
                }
                ButtonArray[i].BtnTxt.Text = UserSettings.Instance.Button[i].BtnTxt;
            }
            //作成したボタンにショートカットを設定
            SetKey.SetUserKey(ButtonArray, UserSettings.Instance, Key_txt);
#if false
            //修飾キー用ボタン作成
            ModButton = new FrickButton();
            FrickGrid.Children.Add(ModButton);
            ModButton.Margin = new Thickness(64 * X, Y * 64, 0, 0);
            ModButton.BtnTxt.Text = "修飾キー";
            ModButton.BtnImg.Source = ModBmp;
            ModButton.StylusDown += FrickButton_StylusDown;
            ModButton.MouseMove += FrickButton_MouseMove;
            //マウスアップイベントは修飾キー用を設定
            ModButton.MouseUp += FrickButtonMod_MouseUp;
            ModButton.ModFlg = true;
            for (int i = 0; i < Constants.FRICK_WAY + 1; i++)
            {
                //修飾キーに未設定はないためポップアップは無条件で白背景に
                ModButton.PopText[i].Background = BrsW;
            }

            SetKey.SetKeyText(ModButton, Properties.Settings.Default.ModStr);
            SetKey.SetModKeyCD(ModButton.KeyAssign, Properties.Settings.Default.ModCD);
#endif
            //ホイールパッド配列作成
            PadArray = new FrickPad[UserSettings.Instance.PadCnt];
            for (int i = 0; i < UserSettings.Instance.PadCnt; i++)
            {
                PadArray[i] = new FrickPad();
                FrickGrid.Children.Add(PadArray[i]);
                PadArray[i].Margin = new Thickness(192 + 128 * i, 0, 0, 0);

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
            SetKey.SetKeyCD(PadArray, Properties.Settings.Default.padCDNP, UserSettings.Instance.PadCnt, Key_txt, List_txt);
            #endregion
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
#if false
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
#endif
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


        #region 各種イベント
        //MouseDownイベントはスタイラスでタップした場合のタイムラグが解消できなかったため不採用

        /// <summary>
        /// フリック処理開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrickButton_StylusDown(object sender, StylusDownEventArgs e)
        {
            var TargetButton = (FrickButton)sender;
            Stylus.Capture(TargetButton);
            Point pos = e.GetPosition(TargetButton);
            StartPointX = pos.X;
            StartPointY = pos.Y;
            TargetButton.FrickPopImage.Source = PopImageC;
//            target.FrickPop.PlacementTarget = target;
            if (TargetButton.ModFlg)
            {
                //修飾キーの場合現在の修飾キーの押下状態をポップアップに反映する
                for (int i = 1; i <= Constants.FRICK_WAY; i++)
                {
                    //先頭はキャンセル用のためループは1から開始
                    switch (TargetButton.KeyAssign[i][0])
                    {
                        case VK_CONTROL:
                            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                            {
                                TargetButton.PopText[i].Background = BrsY;
                            }
                            else
                            {
                                TargetButton.PopText[i].Background = BrsW;
                            }
                            break;
                        case VK_SHIFT:
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0)
                            {
                                TargetButton.PopText[i].Background = BrsY;
                            }
                            else
                            {
                                TargetButton.PopText[i].Background = BrsW;
                            }
                            break;
                        case VK_MENU:
                            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0)
                            {
                                TargetButton.PopText[i].Background = BrsY;
                            }
                            else
                            {
                                TargetButton.PopText[i].Background = BrsW;
                            }
                            break;
                        default: //修飾キー以外はスペースとみなす
                            if (Keyboard.IsKeyDown(Key.Space))
                            {
                                TargetButton.PopText[i].Background = BrsY;
                            }
                            else
                            {
                                TargetButton.PopText[i].Background = BrsW;
                            }
                            break;
                    }
                }
            }
            TargetButton.FrickPop.IsOpen = true;
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
            if (TargetButton.ModFlg)
            {
                //修飾キーの場合現在の修飾キーの押下状態をポップアップに反映する
                for (int i = 1; i <= Constants.FRICK_WAY; i++)
                {
                    //先頭はキャンセル用のためループは1から開始
                    switch (TargetButton.KeyAssign[i][0])
                    {
                        case VK_CONTROL:
                            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                            {
                                TargetButton.PopText[i].Background = BrsY;
                            }
                            else
                            {
                                TargetButton.PopText[i].Background = BrsW;
                            }
                            break;
                        case VK_SHIFT:
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0)
                            {
                                TargetButton.PopText[i].Background = BrsY;
                            }
                            else
                            {
                                TargetButton.PopText[i].Background = BrsW;
                            }
                            break;
                        case VK_MENU:
                            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0)
                            {
                                TargetButton.PopText[i].Background = BrsY;
                            }
                            else
                            {
                                TargetButton.PopText[i].Background = BrsW;
                            }
                            break;
                        default: //修飾キー以外はスペースとみなす
                            if (Keyboard.IsKeyDown(Key.Space))
                            {
                                TargetButton.PopText[i].Background = BrsY;
                            }
                            else
                            {
                                TargetButton.PopText[i].Background = BrsW;
                            }
                            break;
                    }
                }
            }
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
                    if (System.Math.Abs(difY) < UserSettings.Instance.FrickRange)
                    {
                        target.FrickPopImage.Source = PopImageC;
                    }
                    else if (System.Math.Abs(difY) > UserSettings.Instance.FrickCancel)
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
                    if (System.Math.Abs(difX) < UserSettings.Instance.FrickRange)
                    {
                        target.FrickPopImage.Source = PopImageC;
                    }
                    else if (System.Math.Abs(difX) > UserSettings.Instance.FrickCancel)
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
        /// 押下中の修飾キーを離してコマンド実行後元に戻す
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
                if (System.Math.Abs(difY) < UserSettings.Instance.FrickRange)
                {
                    //フリックなし
                    send.Sendkey(target.KeyAssign[0]);
                }
                else if (System.Math.Abs(difY) > UserSettings.Instance.FrickCancel)
                {
                    //処理なし
                }
                else if (difY > 0)
                {
                    //上フリック
                    send.Sendkey(target.KeyAssign[1]);
                }
                else
                {
                    //下フリック
                    send.Sendkey(target.KeyAssign[2]);
                }
            }
            else
            {
                if (System.Math.Abs(difX) < UserSettings.Instance.FrickRange)
                {
                    //フリックなし
                    send.Sendkey(target.KeyAssign[0]);
                }
                else if (System.Math.Abs(difX) > UserSettings.Instance.FrickCancel)
                {
                    //処理なし
                }
                else if (difX > 0)
                {
                    //左フリック
                    send.Sendkey(target.KeyAssign[3]);
                }
                else
                {
                    //右フリック
                    send.Sendkey(target.KeyAssign[4]);
                }
            }
            Mouse.Capture(null);
            //コマンド送信後ワンテンポ遅らせてポップアップを消す
            System.Threading.Thread.Sleep(100);
            target.FrickPop.IsOpen = false;
        }
        /// <summary>
        /// フリック確定（修飾キー用）
        /// 押下中の修飾キーをキャンセルしないメソッドを呼び出す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrickButtonMod_MouseUp(object sender, MouseButtonEventArgs e)
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
                if (System.Math.Abs(difY) < UserSettings.Instance.FrickRange)
                {
                    //フリックなし
                    send.SendModkey(target.KeyAssign[0]);
                }
                else if (System.Math.Abs(difY) > UserSettings.Instance.FrickCancel)
                {
                    //処理なし
                }
                else if (difY > 0)
                {
                    //上フリック
                    send.SendModkey(target.KeyAssign[1]);
                }
                else
                {
                    //下フリック
                    send.SendModkey(target.KeyAssign[2]);
                }
            }
            else
            {
                if (System.Math.Abs(difX) < UserSettings.Instance.FrickRange)
                {
                    //フリックなし
                    send.SendModkey(target.KeyAssign[0]);
                }
                else if (System.Math.Abs(difX) > UserSettings.Instance.FrickCancel)
                {
                    //処理なし
                }
                else if (difX > 0)
                {
                    //左フリック
                    send.SendModkey(target.KeyAssign[3]);
                }
                else
                {
                    //右フリック
                    send.SendModkey(target.KeyAssign[4]);
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
                if (System.Math.Abs(difY) < UserSettings.Instance.FrickRange)
                {
                    //フリックなし
                    Pad.FuncID = 0;
                }
                else if (System.Math.Abs(difY) > UserSettings.Instance.FrickCancel)
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
                if (System.Math.Abs(difX) < UserSettings.Instance.FrickRange)
                {
                    //フリックなし
                    Pad.FuncID = 0;
                }
                else if (System.Math.Abs(difX) > UserSettings.Instance.FrickCancel)
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
                    else if (target.PadBtn.KeyAssign[target.FuncID][0] == Constants.WHEEL_ASSIGN)
                    {
                        mouse.Sendwheel(40);
                    }
                    else
                    {
                        send.Sendkey(target.PadBtn.KeyAssign[target.FuncID]);
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
                    else if (target.KeyAsignR[target.FuncID][0] == Constants.WHEEL_ASSIGN)
                    {
                        mouse.Sendwheel(-40);
                    }
                    else
                    {
                        send.Sendkey(target.KeyAsignR[target.FuncID]);
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

        /// <summary>
        /// ネコペのディレクトリを取得
        /// 初回起動時や存在しない場合入力させる
        /// </summary>
        /// <param name="Dir"></param>
        public void GetNpDir()
        {
            bool ChkFlg = true;
            string Dir = UserSettings.Instance.NpDir;
            if (Dir == null || Dir == "")
            {
                //初回起動時
                MessageBox.Show("ネコペイント本体の場所を指定してください", "起動時設定");
                ChkFlg = false;
            }
            else
            {
                DirectoryInfo dirChk = new DirectoryInfo(Dir);
                if (dirChk.Exists == true)
                {
                    var aaa = dirChk.GetFiles(Constants.NP_EXE_NAME);
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
                    UserSettings.Instance.NpDir = System.IO.Path.GetDirectoryName(dlg.FileName);
                    UserSettings.SaveSetting();
                }
                else
                {
                    Dir = "";
                }
            }
            return;
        }
    }

    //ボタン設定処理いろいろ
    class KeyConfig
    {
        //キー定義
        private NameValueCollection NekoKeyDef = new NameValueCollection();

        public KeyConfig()
        {
            //アプリ設定からネコペのキー表記とキーコードの対応表を取得
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

        //ユーザー設定から各ボタンのポップアップ、キー割り当てを設定
        public void SetUserKey(FrickButton[] TargetButton, UserSettings UserKey, Dictionary<int, string> AllAssign)
        {
            for (int i = 0; i < UserKey.ButtonCnt; i++) //ボタン数ループ
            {
                //ボタン種別の判定
                if (UserKey.Button[i].kind == Constants.BTN_NORMAL)
                {
                    //通常ボタンの場合
                    for (int j = 0; j <= (Constants.FRICK_WAY); j++) //フリック数ループ
                    {
                        //ポップアップ用テキスト設定
                        TargetButton[i].PopText[j].Text = UserKey.Button[i].KeyText[j];
                        //ネコペのショートカットIDから送信するキーコードを設定
                        string[] Split;
                        int[] KeyArray;
                        int KeyCnt = 0;
                        int ID;
                        //キー割り当てが数値変換出来るか判定
                        if (int.TryParse(UserKey.Button[i].KeyAssign[j], out ID))
                        {
                            //数値変換出来る場合はショートカットID指定とみなす
                            //ネコペの割り当て済みショートカット一覧から該当するショートカットを検索
                            if (!AllAssign.ContainsKey(ID))
                            {
                                //ショートカットが設定されていない場合テキストを赤表示にして次のキーに
                                TargetButton[i].KeyAssign[j] = new int[0];
                                SolidColorBrush brs = new SolidColorBrush(Colors.Red);
                                TargetButton[i].PopText[j].Foreground = brs;
                                continue;
                            }
                            TargetButton[i].PopText[j].Background = new SolidColorBrush(Colors.White);
                            //割り当て済みショートカット一覧から該当するショートカットキーを取得
                            string conv = AllAssign[ID];
                            //入力を空白文字で分割
                            Split = conv.Split();
                            KeyArray = new int[Split.Length];
                            //分割した文字列からショートカットキーの配列を生成
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
                                            KeyArray[KeyCnt] = Convert.ToByte(str[0]);
                                        }
                                        catch
                                        {
                                            //2byteとか特殊文字対策
                                            //キー定義から検索して設定
                                            KeyArray[KeyCnt] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                                        }
                                    }
                                }
                                else
                                {
                                    //キー定義から検索して設定
                                    KeyArray[KeyCnt] = Convert.ToByte(NekoKeyDef.Get(str), 16);
                                }
                                KeyCnt++;
                            }
                            //ショートカットキーをボタンに登録
                            TargetButton[i].KeyAssign[j] = new int[KeyCnt * 2];
                            for (int k = 0; k < KeyCnt; k++)
                            {
                                TargetButton[i].KeyAssign[j][k] = KeyArray[k];
                                //キーアップは逆順にマイナス値を設定
                                TargetButton[i].KeyAssign[j][k + KeyCnt] = -KeyArray[KeyCnt - k - 1];
                            }
                        }
                    }
                }
                else
                {
                    //修飾キー用ボタンの場合
                    //ポップアップ用テキスト設定
                    TargetButton[i].PopText[0].Text = UserKey.Button[i].KeyText[0];
                    //センターの領域確保
                    TargetButton[i].KeyAssign[0] = new int[Constants.FRICK_WAY];
                    int cnt = 0;
                    //0件目（センター）はキャンセル専用とするため1～4件目を処理する
                    for (int j = 1; j <= Constants.FRICK_WAY; j++)
                    {
                        //ポップアップ用テキスト設定
                        TargetButton[i].PopText[j].Text = UserKey.Button[i].KeyText[j];
                        //修飾キーは単一のためループ無しで配列も1固定
                        TargetButton[i].KeyAssign[j] = new int[1];
                        TargetButton[i].KeyAssign[j][0] = Convert.ToByte(NekoKeyDef.Get(UserKey.Button[i].KeyAssign[j]), 16);
                        //設定したキーのキーアップをセンターに設定
                        TargetButton[i].KeyAssign[0][cnt] = -TargetButton[i].KeyAssign[j][0];
                        cnt++;
                    }
                }
            }
        }

        //ポップアップに表示するテキスト設定
        public void SetKeyText(FrickButton[] TargetButton, StringCollection key_str, int Cnt)
        {
            for(int i = 0;i < Cnt;i++)
            {
                for (int j = 0; j < (Constants.FRICK_WAY + 1); j++)
                {
                    TargetButton[i].PopText[j].Text = key_str[i * (Constants.FRICK_WAY + 1) + j];
                }
            }
        }
        public void SetKeyText(FrickButton TargetButton, StringCollection key_str)
        {
            for (int i = 0; i < (Constants.FRICK_WAY + 1); i++)
            {
                TargetButton.PopText[i].Text = key_str[i];
            }
        }

        //各フリックに送信するキーコード設定
        public void SetKeyCD(int[][] KeyAsign, StringCollection key_str)
        {
            for (int i = 0; i < (Constants.FRICK_WAY + 1); i++)
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
                for (int i = 0; i < (Constants.FRICK_WAY + 1); i++)
                {
                    string[] Split;
                    int[] KeyArray;
                    int j = 0;

                    //入力を空白文字で分割
                    Split = key_str[i + m * (Constants.FRICK_WAY + 1)].Split();
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
                for (int i = 0; i < (Constants.FRICK_WAY + 1); i++)
                {
                    string[] Split;
                    int[] KeyArray;
                    int j = 0;
                    int ID = Convert.ToInt16(key_str[i + m * (Constants.FRICK_WAY + 1)]);
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
            //パッド数ループ
            for (int m = 0; m < Cnt; m++)
            {
                //フリック方向数ループ
                for (int i = 0; i < (Constants.FRICK_WAY + 1); i++)
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
                        TargetButton[m].PadBtn.KeyAssign[i][0] = Constants.WHEEL_ASSIGN;
                        TargetButton[m].KeyAsignR[i][0] = Constants.WHEEL_ASSIGN;
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
            //センターの領域確保
            KeyAsign[0] = new int[Constants.FRICK_WAY];
            int cnt = 0;
            //0件目（センター）はキャンセル専用とするため1～4件目を処理する
            for (int i = 1; i <= Constants.FRICK_WAY; i++)
            {
                //修飾キーは単一のためループ無しで配列も1固定
                KeyAsign[i] = new int[1];
                KeyAsign[i][0] = Convert.ToByte(NekoKeyDef.Get(key_str[i]), 16);
                //設定したキーのキーアップをセンターに設定
                KeyAsign[0][cnt] = -KeyAsign[i][0];
                cnt++;
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
        public void Sendkey(int[] Code)
        {
            bool ctrl = false, alt = false, shift = false;
            //修飾キー以外の場合押下中の修飾キーを解除し、最後に再押下する
            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
            {
                ctrl = true;
                keybd_event(VK_CONTROL, 0, 2, (UIntPtr)0);
            }
            if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0)
            {
                shift = true;
                keybd_event(VK_SHIFT, 0, 2, (UIntPtr)0);
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0)
            {
                alt = true;
                keybd_event(VK_MENU, 0, 2, (UIntPtr)0);
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
        //修飾キー用
        public void SendModkey(int[] Code)
        {
            foreach (int cd in Code)
            {
                int sendcd = 0;
                if (cd > 0)
                {
                    //プラス値が入力された場合、個別の修飾キーとみなす
                    switch (cd)
                    {
                        //各キーの押下状態を反転するようにコード値を設定
                        case VK_CONTROL:
                            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
                            {
                                sendcd = -cd;
                            }
                            else
                            {
                                sendcd = cd;
                            }
                            break;
                        case VK_SHIFT:
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0)
                            {
                                sendcd = -cd;
                            }
                            else
                            {
                                sendcd = cd;
                            }
                            break;
                        case VK_MENU:
                            if ((Keyboard.Modifiers & ModifierKeys.Alt) > 0)
                            {
                                sendcd = -cd;
                            }
                            else
                            {
                                sendcd = cd;
                            }
                            break;
                        default: //修飾キー以外はスペースとみなす
                            if (Keyboard.IsKeyDown(Key.Space))
                            {
                                sendcd = -cd;
                            }
                            else
                            {
                                sendcd = cd;
                            }
                            break;
                    }
                }
                else
                {
                    //マイナス値が入力された場合リセットとみなす
                    sendcd = cd;
                }
                if (sendcd > 0)
                {
                    //正ならキーダウン
                    keybd_event((byte)sendcd, 0, 0, (UIntPtr)0);
                }
                else
                {
                    //負ならキーアップ
                    keybd_event((byte)-sendcd, 0, 2, (UIntPtr)0);
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
