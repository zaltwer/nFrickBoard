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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace nFrickBoard
{
    /// <summary>
    /// FrickButton.xaml の相互作用ロジック
    /// </summary>
    public partial class FrickPad : UserControl
    {
        //キー割り当て（逆回転用）
        public int[][] KeyAsignR { get; set; }
        public int FuncID { get; set; }
        //ホイール感度
        public double[] sens { get; set; }

        public FrickPad()
        {
            InitializeComponent();
            KeyAsignR = new int[5][];
            sens = new double[5] { 0.05, 0.05, 0.05, 0.05, 0.05 };
            PadBtn.ParentPad = this;
            FuncID = 0;
        }
    }
}
