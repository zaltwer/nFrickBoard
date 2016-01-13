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
        private double dStartPointX;
        private double dStartPointY;

        //ホイールパッド関連
        private double dPadPointX;
        private double dPadPointY;
        private bool flg = false;
        private int[][] PadKeyR;
        private int[][] PadKeyL;

        //フリック判定範囲
        private const int iFrickRange = 20;
        private const int iFrickCancel = 150;

        //フリック時に表示するポップアップ用画像設定
        BitmapImage PopImageC = new BitmapImage(new Uri("Resources/maskb.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageU = new BitmapImage(new Uri("Resources/maskU.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageD = new BitmapImage(new Uri("Resources/maskD.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageL = new BitmapImage(new Uri("Resources/maskL.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageR = new BitmapImage(new Uri("Resources/maskR.png", UriKind.RelativeOrAbsolute));
        BitmapImage PopImageCancel = new BitmapImage(new Uri("Resources/close01.png", UriKind.RelativeOrAbsolute));

        List<Image> fblist = new List<Image>();
        FrickData[] fd = new FrickData[5];
        FrickDataPad fdPad = new FrickDataPad();

        //初期化
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            KeyConfig SetKey = new KeyConfig(); 

            //アクティブにしないおまじない
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);

            //ウィンドウメッセージ取得用
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(new HwndSourceHook(WndProc));

            #region //各ボタンの表示テキスト・キー割り当て設定
            for (int i = 0; i < 5; i++)
            {
                fd[i] = new FrickData();
            }
            SetKey.SetKeyText(fd[0], Properties.Settings.Default.key0_Str);
            SetKey.SetKeyCD(fd[0], Properties.Settings.Default.key0_CD);

            SetKey.SetKeyText(fd[1], Properties.Settings.Default.key1_Str);
            SetKey.SetKeyCD(fd[1], Properties.Settings.Default.key1_CD);

            SetKey.SetKeyText(fd[2], Properties.Settings.Default.key2_Str);
            SetKey.SetKeyCD(fd[2], Properties.Settings.Default.key2_CD);

            SetKey.SetKeyText(fd[3], Properties.Settings.Default.key3_Str);
            SetKey.SetKeyCD(fd[3], Properties.Settings.Default.key3_CD);

            SetKey.SetKeyText(fd[4], Properties.Settings.Default.key4_Str);
            SetKey.SetKeyCD(fd[4], Properties.Settings.Default.key4_CD);

//            SetKey.SetKeyText(fdPad, Properties.Settings.Default.key4_Str);
//            SetKey.SetKeyCD(fdPad, Properties.Settings.Default.key4_CD);
            #endregion
        }

        //偽ボタンコントロール配列化
        private void ButtonFake_Loaded(object sender, RoutedEventArgs e)
        {
            fblist.Add((Image)sender);
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
                    //ボタン1上にカーソルがある場合
                    if (ButtonFake01.IsMouseOver == true)
                    {
                        if (Stylus.CurrentStylusDevice == null)
                        {
                            FirstTouch(ButtonFake01, fd[0]);
                        }
                        //ここでメッセージを止めないとDragMoveに食われる
                        handled = true; // ここでメッセージを止める場合はtrue
                    }
                    //ボタン2上にカーソルがある場合
                    else if (ButtonFake02.IsMouseOver == true)
                    {
                        if (Stylus.CurrentStylusDevice == null)
                        {
                            FirstTouch(ButtonFake02, fd[1]);
                        }
                        //ここでメッセージを止めないとDragMoveに食われる
                        handled = true; // ここでメッセージを止める場合はtrue
                    }
                    //ボタン3上にカーソルがある場合
                    else if (ButtonFake03.IsMouseOver == true)
                    {
                        if (Stylus.CurrentStylusDevice == null)
                        {
                            FirstTouch(ButtonFake03, fd[2]);
                        }
                        //ここでメッセージを止めないとDragMoveに食われる
                        handled = true; // ここでメッセージを止める場合はtrue
                    }
                    //ボタン4上にカーソルがある場合
                    else if (ButtonFake04.IsMouseOver == true)
                    {
                        if (Stylus.CurrentStylusDevice == null)
                        {
                            FirstTouch(ButtonFake04, fd[3]);
                        }
                        //ここでメッセージを止めないとDragMoveに食われる
                        handled = true; // ここでメッセージを止める場合はtrue
                    }
                    //ボタン5上にカーソルがある場合
                    else if (ButtonFake05.IsMouseOver == true)
                    {
                        if (Stylus.CurrentStylusDevice == null)
                        {
                            FirstTouch(ButtonFake05, fd[4]);
                        }
                        //ここでメッセージを止めないとDragMoveに食われる
                        handled = true; // ここでメッセージを止める場合はtrue
                    }
#if false                    //パッドボタン上にカーソルがある場合
                    else if (ButtonFakePad01.IsMouseOver == true)
                    {
                        if (Stylus.CurrentStylusDevice == null)
                        {
                            FirstTouch(ButtonFakePad01, fdPad);
                        }
                        //ここでメッセージを止めないとDragMoveに食われる
                        handled = true; // ここでメッセージを止める場合はtrue
                    }
#endif                    //パッド上にカーソルがある場合
                    else if (Pad01.IsMouseOver == true)
                    {
                        if (Stylus.CurrentStylusDevice == null)
                        {
                            FirstTouchPad(Pad01);
                        }
                        //ここでメッセージを止めないとDragMoveに食われる
                        handled = true; // ここでメッセージを止める場合はtrue
                    }
                    //                   return new IntPtr(MA_NOACTIVATE); // メッセージや処理によって適切な値を返す
                    break;
            }
            return IntPtr.Zero;
        }

        //MouseDownイベントはスタイラスでタップした場合のタイムラグが解消できなかったため不採用
        private void ButtonFake01_MouseDown(object sender, MouseButtonEventArgs e)
        {/*
            Mouse.Capture(ButtonFake01);
            Point pos = e.GetPosition(ButtonFake01);
            dStartPointX = pos.X;
            dStartPointY = pos.Y;
            FrickPopImage.Source = FrickPopImageB;
            FrickPop.PlacementTarget = ButtonFake01;
            FrickPop.IsOpen = true;
        */}

        //空き地ドラッグで移動できるように
        private void nFrick_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        //閉じる
        private void close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            nFrick.Close();
        }
        private void ButtonFake_StylusDown(Image TargetButton, FrickData fd, StylusDownEventArgs e)
        {
            Stylus.Capture(TargetButton);
            Point pos = e.GetPosition(TargetButton);
            dStartPointX = pos.X;
            dStartPointY = pos.Y;
            FrickPopImage.Source = PopImageC;
            PopTextC.Text = fd.PopText[0];
            PopTextU.Text = fd.PopText[1];
            PopTextD.Text = fd.PopText[2];
            PopTextL.Text = fd.PopText[3];
            PopTextR.Text = fd.PopText[4];
            FrickPop.PlacementTarget = TargetButton;
            FrickPop.IsOpen = true;
        }
        private void ButtonFake_StylusDown(object sender, StylusDownEventArgs e)
        {
            Stylus.Capture((Image)sender);
            Point pos = e.GetPosition((Image)sender);
            dStartPointX = pos.X;
            dStartPointY = pos.Y;
            int id = fblist.FindIndex(delegate(Image im) { return sender.Equals(im);});
            FrickPopImage.Source = PopImageC;
            PopTextC.Text = fd[id].PopText[0];
            PopTextU.Text = fd[id].PopText[1];
            PopTextD.Text = fd[id].PopText[2];
            PopTextL.Text = fd[id].PopText[3];
            PopTextR.Text = fd[id].PopText[4];
            FrickPop.PlacementTarget = (Image)sender;
            FrickPop.IsOpen = true;
        }
        private void ButtonFake_MouseMove(Image TargetButton, MouseEventArgs e)
        {
            if (FrickPop.IsOpen == true)
            {
                double difX, difY;
                Point pos = e.GetPosition(TargetButton);
                difX = dStartPointX - pos.X;
                difY = dStartPointY - pos.Y;
                if (System.Math.Abs(difX) < System.Math.Abs(difY))
                {
                    if (System.Math.Abs(difY) < iFrickRange)
                    {
                        FrickPopImage.Source = PopImageC;
                    }
                    else if (System.Math.Abs(difY) > iFrickCancel)
                    {
                        //キャンセル
                        FrickPopImage.Source = PopImageCancel;
                    }
                    else if (difY > 0)
                    {
                        FrickPopImage.Source = PopImageU;
                    }
                    else
                    {
                        FrickPopImage.Source = PopImageD;
                    }
                }
                else
                {
                    if (System.Math.Abs(difX) < iFrickRange)
                    {
                        FrickPopImage.Source = PopImageC;
                    }
                    else if (System.Math.Abs(difX) > iFrickCancel)
                    {
                        //キャンセル
                        FrickPopImage.Source = PopImageCancel;
                    }
                    else if (difX > 0)
                    {
                        FrickPopImage.Source = PopImageL;
                    }
                    else
                    {
                        FrickPopImage.Source = PopImageR;
                    }
                }
            }
        }
        private void ButtonFake_MouseMove(object sender, MouseEventArgs e)
        {
            if (FrickPop.IsOpen == true)
            {
                double difX, difY;
                Point pos = e.GetPosition((Image)sender);
                difX = dStartPointX - pos.X;
                difY = dStartPointY - pos.Y;
                if (System.Math.Abs(difX) < System.Math.Abs(difY))
                {
                    if (System.Math.Abs(difY) < iFrickRange)
                    {
                        FrickPopImage.Source = PopImageC;
                    }
                    else if (System.Math.Abs(difY) > iFrickCancel)
                    {
                        //キャンセル
                        FrickPopImage.Source = PopImageCancel;
                    }
                    else if (difY > 0)
                    {
                        FrickPopImage.Source = PopImageU;
                    }
                    else
                    {
                        FrickPopImage.Source = PopImageD;
                    }
                }
                else
                {
                    if (System.Math.Abs(difX) < iFrickRange)
                    {
                        FrickPopImage.Source = PopImageC;
                    }
                    else if (System.Math.Abs(difX) > iFrickCancel)
                    {
                        //キャンセル
                        FrickPopImage.Source = PopImageCancel;
                    }
                    else if (difX > 0)
                    {
                        FrickPopImage.Source = PopImageL;
                    }
                    else
                    {
                        FrickPopImage.Source = PopImageR;
                    }
                }
            }
        }
        private void ButtonFake_MouseUp(Image TargetButton, FrickData fd, MouseButtonEventArgs e)
        {
            double difX, difY;
            SendKeyCode send = new SendKeyCode();
            Point pos = e.GetPosition(TargetButton);
            difX = dStartPointX - pos.X;
            difY = dStartPointY - pos.Y;

            // アクティブなウィンドウハンドルの取得
            IntPtr hWnd = GetForegroundWindow();
            //IMEハンドルの取得
            IntPtr hIMC = ImmGetDefaultIMEWnd(hWnd);
            //IMEをOFF
            SendMessage(hIMC, WM_IME_CONTROL, IMC_SETOPENSTATUS, 0);

            if (System.Math.Abs(difX) < System.Math.Abs(difY))
            {
                if (System.Math.Abs(difY) < iFrickRange)
                {
                    //フリックなし
                    send.Sendkey(fd.KeyAsign[0]);
                }
                else if (System.Math.Abs(difY) > iFrickCancel)
                {
                    //処理なし
                }
                else if (difY > 0)
                {
                    //上フリック
                    send.Sendkey(fd.KeyAsign[1]);
                }
                else
                {
                    //下フリック
                    send.Sendkey(fd.KeyAsign[2]);
                }
            }
            else
            {
                if (System.Math.Abs(difX) < iFrickRange)
                {
                    //フリックなし
                    send.Sendkey(fd.KeyAsign[0]);
                }
                else if (System.Math.Abs(difX) > iFrickCancel)
                {
                    //処理なし
                }
                else if (difX > 0)
                {
                    //左フリック
                    send.Sendkey(fd.KeyAsign[3]);
                }
                else
                {
                    //右フリック
                    send.Sendkey(fd.KeyAsign[4]);
                }
            }
            Mouse.Capture(null);
            System.Threading.Thread.Sleep(100);
            FrickPop.IsOpen = false;
        }
        private void ButtonFake_MouseUp(object sender, MouseButtonEventArgs e)
        {
            double difX, difY;
            SendKeyCode send = new SendKeyCode();
            Point pos = e.GetPosition((Image)sender);
            difX = dStartPointX - pos.X;
            difY = dStartPointY - pos.Y;
            int id = fblist.FindIndex(delegate(Image im) { return sender.Equals(im); });

            // アクティブなウィンドウハンドルの取得
            IntPtr hWnd = GetForegroundWindow();
            //IMEハンドルの取得
            IntPtr hIMC = ImmGetDefaultIMEWnd(hWnd);
            //IMEをOFF
            SendMessage(hIMC, WM_IME_CONTROL, IMC_SETOPENSTATUS, 0);

            if (System.Math.Abs(difX) < System.Math.Abs(difY))
            {
                if (System.Math.Abs(difY) < iFrickRange)
                {
                    //フリックなし
                    send.Sendkey(fd[id].KeyAsign[0]);
                }
                else if (System.Math.Abs(difY) > iFrickCancel)
                {
                    //処理なし
                }
                else if (difY > 0)
                {
                    //上フリック
                    send.Sendkey(fd[id].KeyAsign[1]);
                }
                else
                {
                    //下フリック
                    send.Sendkey(fd[id].KeyAsign[2]);
                }
            }
            else
            {
                if (System.Math.Abs(difX) < iFrickRange)
                {
                    //フリックなし
                    send.Sendkey(fd[id].KeyAsign[0]);
                }
                else if (System.Math.Abs(difX) > iFrickCancel)
                {
                    //処理なし
                }
                else if (difX > 0)
                {
                    //左フリック
                    send.Sendkey(fd[id].KeyAsign[3]);
                }
                else
                {
                    //右フリック
                    send.Sendkey(fd[id].KeyAsign[4]);
                }
            }
            Mouse.Capture(null);
            System.Threading.Thread.Sleep(100);
            FrickPop.IsOpen = false;
        }
        /// <summary>
        ///アクティブなソフトによってはスタイラスイベントが発生しない場合があるので
        ///ウィンドウメッセージを拾ってこっちを呼び出す
        /// </summary>
        /// <param name="TargetButton">処理対象偽ボタンコントロール</param>
        private void FirstTouch(Image TargetButton, FrickData fd)
        {
            MouseDevice myStylusDevice = Mouse.PrimaryDevice;
            Stylus.Capture(TargetButton);
            Point pos = myStylusDevice.GetPosition(TargetButton);
            dStartPointX = pos.X;
            dStartPointY = pos.Y;
            FrickPopImage.Source = PopImageC;
            PopTextC.Text = fd.PopText[0];
            PopTextU.Text = fd.PopText[1];
            PopTextD.Text = fd.PopText[2];
            PopTextL.Text = fd.PopText[3];
            PopTextR.Text = fd.PopText[4];
            FrickPop.PlacementTarget = TargetButton;
            FrickPop.IsOpen = true;
        }
        private void FirstTouchPad(Image TargetButton)
        {
            MouseDevice myStylusDevice = Mouse.PrimaryDevice;
            Stylus.Capture(TargetButton);
            Point pos = myStylusDevice.GetPosition(TargetButton);
            Stylus.Capture(Pad01);
            dPadPointX = Pad01.RenderSize.Width / 2;
            dPadPointY = Pad01.RenderSize.Height / 2;
            dStartPointX = pos.X - dPadPointX;
            dStartPointY = pos.Y - dPadPointY;
            flg = true;
        }

        #region //各フリックボタンイベント
        private void ButtonFake01_StylusDown(object sender, StylusDownEventArgs e)
        {
            ButtonFake_StylusDown((Image)sender, fd[0], e);
        }
        private void ButtonFake01_MouseMove(object sender, MouseEventArgs e)
        {
            ButtonFake_MouseMove(fd[0], e);
        }
        private void ButtonFake01_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ButtonFake_MouseUp(fd[0], e);
        }

        private void ButtonFake02_MouseMove(object sender, MouseEventArgs e)
        {
            ButtonFake_MouseMove(ButtonFake02, e);
        }
        private void ButtonFake02_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ButtonFake_MouseUp(ButtonFake02, e);
        }
        private void ButtonFake02_StylusDown(object sender, StylusDownEventArgs e)
        {
            ButtonFake_StylusDown(ButtonFake02, e);
        }

        private void ButtonFake03_MouseMove(object sender, MouseEventArgs e)
        {
            ButtonFake_MouseMove(ButtonFake03, e);
        }
        private void ButtonFake03_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ButtonFake_MouseUp(ButtonFake03, e);
        }
        private void ButtonFake03_StylusDown(object sender, StylusDownEventArgs e)
        {
            ButtonFake_StylusDown(ButtonFake03, e);
        }

        private void ButtonFake04_MouseMove(object sender, MouseEventArgs e)
        {
            ButtonFake_MouseMove(ButtonFake04, e);
        }
        private void ButtonFake04_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ButtonFake_MouseUp(ButtonFake04, e);
        }
        private void ButtonFake04_StylusDown(object sender, StylusDownEventArgs e)
        {
            ButtonFake_StylusDown(ButtonFake04, e);
        }

        private void ButtonFake05_MouseMove(object sender, MouseEventArgs e)
        {
            ButtonFake_MouseMove(ButtonFake05, e);
        }
        private void ButtonFake05_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ButtonFake_MouseUp(ButtonFake05, e);
        }
        private void ButtonFake05_StylusDown(object sender, StylusDownEventArgs e)
        {
            ButtonFake_StylusDown(ButtonFake05, e);
        }
        #endregion
#if true
        private void ButtonFakePad01_MouseMove(object sender, MouseEventArgs e)
        {
            ButtonFake_MouseMove(ButtonFakePad01, e);
        }
        private void ButtonFakePad01_MouseUp(object sender, MouseButtonEventArgs e)
        {
            double difX, difY;
            Point pos = e.GetPosition(ButtonFakePad01);
            difX = dStartPointX - pos.X;
            difY = dStartPointY - pos.Y;

            if (System.Math.Abs(difX) < System.Math.Abs(difY))
            {
                if (System.Math.Abs(difY) < iFrickRange)
                {
                    //フリックなし
                    ButtonFakePad01.FuncID = 0;
                }
                else if (difY > 0)
                {
                    //上フリック
                    ButtonFakePad01.FuncID = 1;
                }
                else
                {
                    //下フリック
                    ButtonFakePad01.FuncID = 2;
                }
            }
            else
            {
                if (System.Math.Abs(difX) < iFrickRange)
                {
                    //フリックなし
                    ButtonFakePad01.FuncID = 0;
                }
                else if (difX > 0)
                {
                    //左フリック
                    ButtonFakePad01.FuncID = 3;
                }
                else
                {
                    //右フリック
                    ButtonFakePad01.FuncID = 4;
                }
            }
            Mouse.Capture(null);
            FrickPop.IsOpen = false;
        }
        private void ButtonFakePad01_StylusDown(object sender, StylusDownEventArgs e)
        {
            ButtonFake_StylusDown(ButtonFakePad01, e);
        }
#endif

        private void Pad01_MouseMove(object sender, MouseEventArgs e)
        {
            double ang;
            double x, y;
            SendKeyCode send = new SendKeyCode();
            SendMouseCode mouse = new SendMouseCode();

            if (flg == true)
            {
                Point pos = e.GetPosition(Pad01);
                x = pos.X - dPadPointX;
                y = pos.Y - dPadPointY;
                ang = Math.Atan2(
                    dStartPointX * y - dStartPointY * x,
                    dStartPointX * x + dStartPointY * y
                    );
                if (ang > 0.1)
                {
                    dStartPointX = x;
                    dStartPointY = y;
//                    send.Sendkey(0x31);
                    mouse.Sendwheel(-40);
                }
                if (ang < -0.1)
                {
                    dStartPointX = x;
                    dStartPointY = y;
//                    send.Sendkey(0x32);
                    mouse.Sendwheel(40);
                }
            }
        }
        private void Pad01_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            flg = false;
        }
        private void Pad01_StylusDown(object sender, StylusDownEventArgs e)
        {
            Stylus.Capture(Pad01);
            Point pos = e.GetPosition(Pad01);
            dPadPointX = Pad01.RenderSize.Width / 2;
            dPadPointY = Pad01.RenderSize.Height / 2;
            dStartPointX = pos.X - dPadPointX;
            dStartPointY = pos.Y - dPadPointY;
            flg = true;
        }
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
        public void SetKeyText(FrickData TargetButton, StringCollection key_str)
        {
            TargetButton.PopText = new string[5];
            for (int i = 0; i < 5; i++)
            {
                TargetButton.PopText[i] = key_str[i];
            }
        }

        //各フリックに送信するキーコード設定
        public void SetKeyCD(FrickData TargetButton, StringCollection key_str)
        {
            TargetButton.KeyAsign = new int[5][];

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
                TargetButton.KeyAsign[i] = new int[j * 2];
                for (int k = 0; k < j; k++)
                {
                    TargetButton.KeyAsign[i][k] = KeyArray[k];
                    //キーアップは逆順にマイナス値を設定
                    TargetButton.KeyAsign[i][k + j] = -KeyArray[j - k -1];
                }
            }
        }
        public void SetKeyCD(int[][] KeyAsign, StringCollection key_str)
        {
//            KeyAsign = new int[5][];

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

    //フリックボタンデータ定義
    class FrickData
    {
        //識別番号
        public int ID { get; set; }
        //キー割り当て
        public int[][] KeyAsign { get; set; }
        //ポップアップテキスト
        public String[] PopText { get; set; }
    }
    class FrickDataPad : FrickData
    {
        //タッチホイール機能ID
        public int FuncID { get; set; }
        //逆回転フラグ
        public bool RevFlg { get; set; }
        //逆回転用キー割り当て
        public int[][] KeyAsignR { get; set; }
    }
}
