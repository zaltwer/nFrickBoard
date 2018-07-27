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
    public partial class FrickButton : UserControl
    {
        //識別番号
        public int ID { get; set; }
        public bool ModFlg{ get; set; }
        //キー割り当て
        public int[][] KeyAssign { get; set; }
        public List<TextBox> PopText { get; set; }
        public object ParentPad { get; set; }

        public FrickButton()
        {
            PopText = new List<TextBox>();
            InitializeComponent();
            ModFlg = false;
            KeyAssign = new int[5][];
//            FrickPop.PlacementTarget = BtnImg;
        }

        private void PopText_Initialized(object sender, EventArgs e)
        {
            PopText.Add((TextBox)sender);
        }
    }
}
