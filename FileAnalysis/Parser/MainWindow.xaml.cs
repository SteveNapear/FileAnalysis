using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Parser.Model;

using SNL;

namespace Parser {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public string sParse = string.Empty;
		public string test1 = "10/5+123*237/3*9";
		public string test2 = "10/5+(123*237/3)*9";
		public string test3 = "12345.78900";
		public string test4 = "12.75*0.45-23.67/3+9.25";
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		public MainWindow() {

			InitializeComponent();
		}
		//======================================================================
		private void OnQuit(object sender, RoutedEventArgs e) {
			Close();

		}
		//===================================================================
		private void OnParse(object sender, RoutedEventArgs e) {
			sParse = ParseTBox.Text.Trim();
			sParse = test4;
			bool bOK = CParser.TryParseExp(sParse, out double dv);
			S2.Text = sParse + " ==> " + dv.ToString("f5");
			//			bool bOK = CIntParser.TryParseExp(sParse, out int v);
			AnsTB.Text = (bOK) ? dv.ToString("f5") : "Error";
		}
		//========================================================================
		private void OnLoaded(object sender, RoutedEventArgs e) {
			Size sz = MainGrid.RenderSize;
			Size lsz = ParseLBL.RenderSize;
			ParseTBox.Width = sz.Width - lsz.Width;
			ParseTBox.Text = test2;
			AnsLBL.Width = ParseLBL.Width;
			AnsTB.Width = ParseTBox.Width / 3;
			CalcTB.Width = AnsTB.Width;
			double v = 10 / 5 + 123 * 237 / 3 * 9;
			v = 10 / 5 + 123 * 237 / 3 * 9;
			v = 12 * 45 / 3 * 9;
			v = 12.75*0.45-23.67/3+9.25;
			CalcTB.Text = v.ToString("f5");

			int nc = SBar.Items.Count;
			double wd = sz.Width / nc;
			for (int ix = 0; ix < nc; ix++) {
				TextBlock T = (TextBlock)SBar.Items[ix];
				if (T is null) continue;
				T.Width = wd;
			}
		}

		private void OnKeyUp(object sender, KeyEventArgs e) {
			TextBox B = sender as TextBox;
			if (B is null) return;
			Key key = e.Key;
			if (key != Key.Enter) return;
			KeyboardDevice dev = e.KeyboardDevice;
			ModifierKeys mkeys = dev.Modifiers;
			sParse = B.Text.Trim();
			bool bOK = CParser.TryParseExp(sParse, out double v);
			AnsTB.Text = (bOK) ? v.ToString("f5") : string.Empty;
		}

	}
}
