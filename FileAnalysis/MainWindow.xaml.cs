using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

using Microsoft.Win32;

using SNDump;

using SNL;

namespace FileAnalysis {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
#if DEBUG
		public bool bDebug = true;
#else
        public bool bDebug = false;
#endif
		private const string userRoot = "HKEY_CURRENT_USER";
		private const string subkey = "DUMP";
		public const string sTitle = "File Content Analyzer";
		private string keyName = userRoot + "\\" + subkey;
		private int vSlider = 0;
		//file information
		private FileInfo fi = null;

		private CSN_Asm info = null;
		public CSN_Security Security = null;
		public CFileDump dmp = null;
		public string sHex = "0123456789ABCDEF";
		public bool bByteVis = true;
		const int nByteRow = 17;
		const int nByteCol = 16;
		const int nWordRow = 17;
		const int nWordCol = 8;
		const int szFont = 18;
		const int nAcol = 16, nArow = 17;
		const int nIcol = 4, nIrow = 17;
		const int nFcol = 4, nFrow = 17;
		const int nLcol = 2, nLrow = 17;
		const int nDcol = 2, nDrow = 17;
		const int nUcol = 16, nUrow = 1 + 128 / nUcol;

		private static Color AColor = Colors.White;
		private static Color PColor = Colors.LightGray;
		private static Color BtnBackColor = Colors.Navy;
		private static Color BtnBackhitColor = Colors.DarkGoldenrod;
		private SolidColorBrush SCBWhite = new SolidColorBrush(Colors.White);
		private SolidColorBrush SCBLGray = new SolidColorBrush(Colors.LightGray);
		private SolidColorBrush SCBFore = new SolidColorBrush(Colors.White);
		private SolidColorBrush SCBack = new SolidColorBrush(Colors.DarkGreen);
		private SolidColorBrush ABrush = new SolidColorBrush(AColor);
		private SolidColorBrush PBrush = new SolidColorBrush(PColor);
		private SolidColorBrush BtnBack = new SolidColorBrush(BtnBackColor);
		private SolidColorBrush BtnHit = new SolidColorBrush(BtnBackhitColor);
		private SolidColorBrush AHit = new SolidColorBrush(Colors.Yellow);
		private SolidColorBrush ANot = new SolidColorBrush(Colors.White);
		private bool bRGB4 = false;

		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		public MainWindow() {
			string fns = null, st;
			Info = new CSN_Asm(Assembly.GetExecutingAssembly());
			Security = new CSN_Security();

			bool bBE = Properties.Settings.Default.BigEndian;
			bool bSigned = Properties.Settings.Default.IsSigned;
			bool bHex = Properties.Settings.Default.IsHex;
			string spath = Properties.Settings.Default.dmpPath;

			string[] sa = Environment.GetCommandLineArgs();
			if (sa.Length > 1) fns = sa[1]; //When user drags a file onto the Icon shortcut
			else fns = spath;

			if (fns != null && File.Exists(fns)) {
				fi = new FileInfo(fns);
				dmp = new CFileDump(fns);
			} else dmp = null;

			InitializeComponent();

			if (dmp != null) {  //remember last settings
				dmp.isSigned = false;
				dmp.isBigEndian = bBE;
				dmp.isSigned = bSigned;
				dmp.isHex = bHex;
			}

			st = Info.sTitle
				+ " version " + Info.sVersion
				+ ", Tier " + (RenderCapability.Tier >> 16).ToString("d")
				+ ", " + Info.sCopyright
				+ ", " + (Info.Is64 ? "-64-" : "-32-")
				+ " for " + Security.sDomainUser
				+ (bDebug ? " ***DEBUG***" : " ");
			this.Title = st;

		}
		//================================================================
		private void Display() {
			Path1.Text = (dmp == null ? "No file opened." : dmp.sPath);
			DOC2.Text = (dmp == null ? "" : dmp.FI.CreationTime.ToShortDateString());
			DOLU3.Text = (dmp == null ? "" : dmp.FI.LastWriteTime.ToShortDateString());
			Attrs4.Text = (dmp == null ? "" : dmp.FI.Attributes.ToString());
			NBYTES5.Text = (dmp == null ? "" : dmp.FI.Length.ToString("n0") + " bytes.");
			OffsetTB.Text = (dmp == null ? "" : dmp.ByteOffset.ToString("d"));
			OffTB.Text = " ";
			BlockTB.Text = (dmp == null ? "" : dmp.BlockNumber.ToString("n0"));
			if (dmp != null) SignBtn.Content = dmp.isSigned ? "Signed" : "Unsigned";
			if (dmp != null) HexBtn.Content = dmp.isHex ? "Hex" : "Dec";
			if (dmp != null) EndianBtn.Content = dmp.isBigEndian ? "Big Endian" : "Little Endian";

			if (bByteVis) {
				DC1.Visibility = Visibility.Hidden;
				ByteGrid.Visibility = Visibility.Visible;
			} else {
				ByteGrid.Visibility = Visibility.Hidden;
				DC1.Visibility = Visibility.Visible;
			}

			OffGrid.Visibility = Visibility.Collapsed;
			AbsGrid.Visibility = Visibility.Collapsed;
			AlphaGrid.Visibility = Visibility.Collapsed;
			UniGrid.Visibility = Visibility.Collapsed;

			ByteGrid.Visibility = Visibility.Collapsed;
			WordGrid.Visibility = Visibility.Collapsed;
			IntGrid.Visibility = Visibility.Collapsed;
			FloatGrid.Visibility = Visibility.Collapsed;
			LongGrid.Visibility = Visibility.Collapsed;
			DoubleGrid.Visibility = Visibility.Collapsed;
			ColorGrid.Visibility = Visibility.Collapsed;

			if (dmp == null) return;

			AlphaGrid.Visibility = Visibility.Visible;
			OffGrid.Visibility = Visibility.Visible;
			AbsGrid.Visibility = Visibility.Visible;
			UniGrid.Visibility = Visibility.Visible;
			FillAbsGrid();
			FillOffGrid();

			ResetButtons();
			switch (dmp.wtype) {
				default: break;
				case CDump.WD.Byte: ByteBtn.Background = BtnHit; FillByteGrid(); ByteGrid.Visibility = Visibility.Visible; break;
				case CDump.WD.Word: WordBtn.Background = BtnHit; FillWordGrid(); WordGrid.Visibility = Visibility.Visible; break;
				case CDump.WD.Int: IntBtn.Background = BtnHit; FIllIntGrid(); IntGrid.Visibility = Visibility.Visible; break;
				case CDump.WD.Float: FloatBtn.Background = BtnHit; FIllFloatGrid(); FloatGrid.Visibility = Visibility.Visible; break;
				case CDump.WD.Long: LongBtn.Background = BtnHit; FillLongGrid(); LongGrid.Visibility = Visibility.Visible; break;
				case CDump.WD.Double: DoubleBtn.Background = BtnHit; FillDoubleGrid(); DoubleGrid.Visibility = Visibility.Visible; break;
				case CDump.WD.Color:
				Color4Btn.Background = (bRGB4) ? BtnHit : BtnBack;
				Color3Btn.Background = (bRGB4) ? BtnBack : BtnHit;
				FillColorGrid();
				ColorGrid.Visibility = Visibility.Visible;
				break;
			}
			FillAlphaGrid();
			FillUniGrid();
		}
		//================================================================
		private void OnQuitBtn(object sender, RoutedEventArgs e) {
			if (dmp != null) {
				Properties.Settings.Default.BigEndian = dmp.isBigEndian;
				Properties.Settings.Default.IsHex = dmp.isHex;
				Properties.Settings.Default.IsSigned = dmp.isSigned;
				Properties.Settings.Default.dmpPath = dmp.sPath;
				Properties.Settings.Default.Save();
			}
			Close();
		}
		//================================================================
		private void OnLoaded(object sender, RoutedEventArgs e) {
			Size szG = MainGrid.RenderSize;

			PrintersCB.Width = szG.Width / 10;
			PrintersCB.Background = SCBLGray;
			SetupPrinterList();
			SetupPrintForm();
			MainSL.Value = vSlider;
			bool b = dmp == null;
			SetupAbsOffGrid();
			SetupByteGrid();
			SetupWordGrid();
			SetupIntGrid();
			SetupColorGrid();
			SetupFLoatGrid();
			SetupAlphaGrid();
			SetupLongGrid();
			SetupDoubleGrid();
			SetupUniGrid();

			Size szS = StatusSP.RenderSize;
			int nc = StatusSP.Children.Count;
			//setup widths
			double wd = szS.Width / nc;
			foreach (Object O in StatusSP.Children) {
				TextBlock B = O as TextBlock;
				if (B is null) continue;
				B.Width = wd;
			}

			szS = SP11.RenderSize;
			wd = szS.Width;
			Path1.Width = wd - PathLabel.RenderSize.Width;
			Display();
		}
		//=================================================================
		private void OnKeyUp(object sender, KeyEventArgs e) {
			ModifierKeys mkey = Keyboard.Modifiers;
			if (mkey != ModifierKeys.Control) return;
			switch (e.Key) {
				case Key.Home: dmp.ReadBlock(0); SetSlider(); return;
				case Key.End: dmp.ReadBlock(dmp.LastBlock); SetSlider(); return;
				case Key.OemMinus: Decrease(); SetSlider(); return;
				case Key.OemPlus: Increase(); SetSlider(); return;
				case Key.P: OnPrintPreview(null, null); return;
				case Key.Q: OnQuitBtn(null, null); return;
				case Key.O: OnOpenBtn(null, null); return;
			}
		}
		//================================================================
		private void SetupUniGrid() {
			int ix, rw, cl;
			double wd = UniGrid.Width / nUcol;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			GridLength wgl = new GridLength(3, GridUnitType.Star);
			UniGrid.ColumnDefinitions.Clear();
			UniGrid.RowDefinitions.Clear();

			for (ix = 0; ix < nUcol; ix++) {
				ColumnDefinition col = new ColumnDefinition();
				col.Width = gl;
				UniGrid.ColumnDefinitions.Add(col);
			}
			for (ix = 0; ix < nUrow; ix++) {
				RowDefinition row = new RowDefinition();
				row.Height = gl;
				UniGrid.RowDefinitions.Add(row);
			}
			for (rw = 0; rw < nUrow; rw++) {
				for (cl = 0; cl < nUcol; cl++) {
					TextBlock TB = new TextBlock();
					if (rw == 0) TB.Name = "H" + sHex.Substring(cl, 1);
					else TB.Name = "U" + sHex.Substring(rw - 1, 1) + sHex.Substring(cl, 1);
					Grid.SetRow(TB, rw);
					SetTB(TB, cl, rw);
					Grid.SetColumn(TB, cl);
					if (rw == 0) TB.FontSize = 20;
					TB.TextAlignment = TextAlignment.Center;
					TB.Text = TB.Name;
					TB.MouseEnter += OnUnicode_TB_MouseEnter;
					TB.MouseLeave += OnUnicode_TB_MouseLeave;
					UniGrid.Children.Add(TB);
				}
			}
		}
		//===============================================================
		private void SetupDoubleGrid() {
			int ix, rw, cl; string st, sn;
			double wd = DoubleGrid.Width / nDcol;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			GridLength wgl = new GridLength(3, GridUnitType.Star);
			DoubleGrid.ColumnDefinitions.Clear();
			DoubleGrid.RowDefinitions.Clear();

			for (ix = 0; ix < nDcol; ix++) {
				ColumnDefinition col = new ColumnDefinition();
				col.Width = gl;
				DoubleGrid.ColumnDefinitions.Add(col);
			}
			for (ix = 0; ix < nDrow; ix++) {
				RowDefinition row = new RowDefinition();
				row.Height = gl;
				DoubleGrid.RowDefinitions.Add(row);
			}
			//fill children
			for (rw = 0; rw < nDrow; rw++) {
				for (cl = 0; cl < nDcol; cl++) {
					TextBlock TB = new TextBlock();
					st = (rw == 0 ? "Hdr" : "D");
					if (rw == 0) sn = sHex.Substring(cl, 1);
					else sn = sHex.Substring(rw - 1, 1) + sHex.Substring(cl, 1);
					TB.Name = st + sn;
					Grid.SetRow(TB, rw);
					SetTB(TB, cl, rw);
					Grid.SetColumn(TB, cl);
					if (rw == 0) TB.FontSize = 20;
					TB.TextAlignment = TextAlignment.Center;
					//					TB.MouseEnter += OnByteGrid_MouseEnter;
					DoubleGrid.Children.Add(TB);
				}
			}
		}
		//===============================================================
		private void SetupAbsOffGrid() {
			int ix, nac, noc;
			double wd = AbsGrid.Width;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			AbsGrid.ColumnDefinitions.Clear();
			AbsGrid.RowDefinitions.Clear();
			OffGrid.ColumnDefinitions.Clear();
			OffGrid.RowDefinitions.Clear();
			for (ix = 0; ix < nByteRow; ix++) {
				RowDefinition rd = new RowDefinition();
				rd.Height = gl;
				AbsGrid.RowDefinitions.Add(rd);
				rd = new RowDefinition();
				rd.Height = gl;
				OffGrid.RowDefinitions.Add(rd);
			}
			for (ix = 0; ix < nByteRow; ix++) {
				//do absolute address textblocks
				TextBlock TB = new TextBlock();
				Grid.SetRow(TB, ix);
				Grid.SetColumn(TB, 0);
				SetTB(TB, 0, ix);
				if (ix == 0) {
					TB.Foreground = SCBFore;
					TB.Background = SCBack;
					TB.FontStyle = FontStyles.Normal;
					TB.TextAlignment = TextAlignment.Center;
				} else {
					TB.TextAlignment = TextAlignment.Right;
					TB.FontStyle = FontStyles.Italic;
				}
				TB.FontWeight = FontWeights.Bold;
				TB.FontSize = szFont * 1.25;
				AbsGrid.Children.Add(TB);
				//do relative address textblocks
				TB = new TextBlock();
				Grid.SetRow(TB, ix);
				Grid.SetColumn(TB, 0);
				SetTB(TB, 0, ix);
				if (ix == 0) {
					TB.Foreground = SCBFore;
					TB.Background = SCBack;
					TB.FontStyle = FontStyles.Normal;
					TB.TextAlignment = TextAlignment.Center;
				} else {
					TB.FontStyle = FontStyles.Italic;
					TB.TextAlignment = TextAlignment.Right;
				}
				TB.FontWeight = FontWeights.Bold;
				TB.FontSize = szFont * 1.25;
				OffGrid.Children.Add(TB);
			}
			nac = AbsGrid.Children.Count;
			noc = OffGrid.Children.Count;
		}
		//===============================================================
		private void SetupAlphaGrid()   //single char array 16 +1 rows x 16 columns
		{
			int ix, rw, cl;
			double wd = ByteGrid.Width / nAcol;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			GridLength wgl = new GridLength(3, GridUnitType.Star);
			AlphaGrid.ColumnDefinitions.Clear();
			AlphaGrid.RowDefinitions.Clear();
			for (ix = 0; ix < nAcol; ix++) {
				ColumnDefinition col = new ColumnDefinition();
				col.Width = gl;
				AlphaGrid.ColumnDefinitions.Add(col);
			}
			for (ix = 0; ix < nArow; ix++) {
				RowDefinition row = new RowDefinition();
				row.Height = gl;
				AlphaGrid.RowDefinitions.Add(row);
			}
			for (rw = 0; rw < nArow; rw++) {
				for (cl = 0; cl < nAcol; cl++) {
					TextBlock TB = new TextBlock();
					if (rw == 0) TB.Name = "H" + sHex.Substring(cl, 1);
					else TB.Name = "A" + sHex.Substring(rw - 1, 1) + sHex.Substring(cl, 1);
					Grid.SetRow(TB, rw);
					Grid.SetColumn(TB, cl);
					SetTB(TB, cl, rw);
					TB.FontStyle = (rw == 0) ? FontStyles.Italic : FontStyles.Normal;
					TB.FontWeight = FontWeights.Bold;
					TB.Background = ANot;
					TB.TextAlignment = TextAlignment.Center;
					TB.FontSize = szFont;
					if (rw > 0) TB.MouseEnter += OnAlphaGrid_MouseEnter;
					if (rw > 0) TB.MouseLeave += OnAlphaGrid_MouseLeave;
					AlphaGrid.Children.Add(TB);
				}
			}
			int nc = AlphaGrid.Children.Count;
		}
		//=================================================================
		private void SetupByteGrid() {
			int ix, rw, cl; string st, sn;
			double wd = ByteGrid.Width / nByteCol; TextBlock tb = null;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			GridLength wgl = new GridLength(3, GridUnitType.Star);
			ByteGrid.ColumnDefinitions.Clear();
			ByteGrid.RowDefinitions.Clear();

			for (ix = 0; ix < nByteCol; ix++) {
				ColumnDefinition col = new ColumnDefinition();
				col.Width = gl;
				ByteGrid.ColumnDefinitions.Add(col);
			}
			for (ix = 0; ix < nByteRow; ix++) {
				RowDefinition row = new RowDefinition();
				row.Height = gl;
				ByteGrid.RowDefinitions.Add(row);
			}
			//fill children
			for (rw = 0; rw < nByteRow; rw++) {
				for (cl = 0; cl < nByteCol; cl++) {
					TextBlock TB = new TextBlock();
					st = (rw == 0 ? "Hdr" : "B");
					if (rw == 0) sn = sHex.Substring(cl, 1);
					else sn = sHex.Substring(rw - 1, 1) + sHex.Substring(cl, 1);
					TB.Name = st + sn;
					Grid.SetRow(TB, rw);
					SetTB(TB, cl, rw);
					Grid.SetColumn(TB, cl);
					if (rw == 0) TB.FontSize = 20;
					TB.TextAlignment = TextAlignment.Center;
					TB.MouseEnter += OnByteGrid_MouseEnter;
					TB.MouseLeave += OnByteGrid_MouseLeave;
					ByteGrid.Children.Add(TB);
				}
			}
			int nc = ByteGrid.Children.Count, cap = ByteGrid.Children.Capacity;
			tb = FN(ByteGrid, "Addr00");
			if (tb != null) tb.Text = "Abs Address";
		}
		//================================================================
		private void SetupWordGrid() {
			int ix, rw, cl; string st, sn;
			double wd = WordGrid.Width / nWordCol;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			GridLength wgl = new GridLength(3, GridUnitType.Star);
			WordGrid.ColumnDefinitions.Clear();
			WordGrid.RowDefinitions.Clear();

			for (ix = 0; ix < nWordCol; ix++) {
				ColumnDefinition col = new ColumnDefinition();
				col.Width = gl;
				WordGrid.ColumnDefinitions.Add(col);
			}
			for (ix = 0; ix < nWordRow; ix++) {
				RowDefinition row = new RowDefinition();
				row.Height = gl;
				WordGrid.RowDefinitions.Add(row);
			}
			//fill children
			for (rw = 0; rw < nWordRow; rw++) {
				for (cl = 0; cl < nWordCol; cl++) {
					TextBlock TB = new TextBlock();
					st = (rw == 0 ? "Hdr" : "W");
					if (rw == 0) sn = sHex.Substring(cl, 1);
					else sn = sHex.Substring(rw - 1, 1) + sHex.Substring(cl, 1);
					TB.Name = st + sn;
					Grid.SetRow(TB, rw);
					SetTB(TB, cl, rw);
					Grid.SetColumn(TB, cl);
					if (rw == 0) TB.FontSize = 20;
					TB.TextAlignment = TextAlignment.Center;
					TB.MouseEnter += OnWordGrid_MouseEnter;
					TB.MouseLeave += OnWordGrid_MouseLeave;
					WordGrid.Children.Add(TB);
				}
			}
			int nc = WordGrid.Children.Count, cap = WordGrid.Children.Capacity;

		}
		//================================================================
		private void SetupIntGrid() {
			int ix, rw, cl; string st, sn;
			double wd = IntGrid.Width / nIcol;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			IntGrid.ColumnDefinitions.Clear();
			IntGrid.RowDefinitions.Clear();

			for (ix = 0; ix < nIcol; ix++) {
				ColumnDefinition col = new ColumnDefinition();
				col.Width = gl;
				IntGrid.ColumnDefinitions.Add(col);
			}
			for (ix = 0; ix < nIrow; ix++) {
				RowDefinition row = new RowDefinition();
				row.Height = gl;
				IntGrid.RowDefinitions.Add(row);
			}
			//fill children
			for (rw = 0; rw < nIrow; rw++) {
				for (cl = 0; cl < nIcol; cl++) {
					TextBlock TB = new TextBlock();
					st = (rw == 0 ? "Hdr" : "I");
					sn = (rw == 0) ? sHex.Substring(cl, 1) : sHex.Substring(rw - 1, 1) + sHex.Substring(cl, 1);
					TB.Name = st + sn;
					Grid.SetRow(TB, rw);
					SetTB(TB, cl, rw);
					Grid.SetColumn(TB, cl);
					if (rw == 0) TB.FontSize = 20;
					TB.TextAlignment = TextAlignment.Center;
					IntGrid.Children.Add(TB);
				}
			}
			int nc = IntGrid.Children.Count, cap = IntGrid.Children.Capacity;
		}
		//================================================================
		private void SetupColorGrid() {
			int ix, rw, cl; string st, sn;
			double wd = IntGrid.Width / nIcol;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			ColorGrid.ColumnDefinitions.Clear();
			ColorGrid.RowDefinitions.Clear();

			for (ix = 0; ix < nIcol; ix++) {
				ColumnDefinition col = new ColumnDefinition();
				col.Width = gl;
				ColorGrid.ColumnDefinitions.Add(col);
			}
			for (ix = 0; ix < nIrow; ix++) {
				RowDefinition row = new RowDefinition();
				row.Height = gl;
				ColorGrid.RowDefinitions.Add(row);
			}
			//fill children
			for (rw = 0; rw < nIrow; rw++) {
				for (cl = 0; cl < nIcol; cl++) {
					TextBlock TB = new TextBlock();
					st = (rw == 0 ? "Hdr" : "I");
					if (rw == 0) sn = sHex.Substring(cl, 1);
					else sn = sHex.Substring(rw - 1, 1) + sHex.Substring(cl, 1);
					TB.Name = st + sn;
					Grid.SetRow(TB, rw);
					SetTB(TB, cl, rw);
					Grid.SetColumn(TB, cl);
					if (rw == 0) TB.FontSize = 20;
					TB.TextAlignment = TextAlignment.Center;
					ColorGrid.Children.Add(TB);
				}
			}
		}
		//================================================================
		private void SetupFLoatGrid() {
			int ix, rw, cl; string st, sn;
			double wd = FloatGrid.Width / nFcol;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			FloatGrid.ColumnDefinitions.Clear();
			FloatGrid.RowDefinitions.Clear();

			for (ix = 0; ix < nFcol; ix++) {
				ColumnDefinition col = new ColumnDefinition();
				col.Width = gl;
				FloatGrid.ColumnDefinitions.Add(col);
			}
			for (ix = 0; ix < nFrow; ix++) {
				RowDefinition row = new RowDefinition();
				row.Height = gl;
				FloatGrid.RowDefinitions.Add(row);
			}
			//fill children
			for (rw = 0; rw < nFrow; rw++) {
				for (cl = 0; cl < nFcol; cl++) {
					TextBlock TB = new TextBlock();
					st = (rw == 0 ? "Hdr" : "F");
					if (rw == 0) sn = sHex.Substring(cl, 1);
					else sn = sHex.Substring(rw - 1, 1) + sHex.Substring(cl, 1);
					TB.Name = st + sn;
					Grid.SetRow(TB, rw);
					SetTB(TB, cl, rw);
					Grid.SetColumn(TB, cl);
					if (rw == 0) TB.FontSize = 20;
					TB.TextAlignment = TextAlignment.Center;
					FloatGrid.Children.Add(TB);
				}
			}
			int nc = FloatGrid.Children.Count, cap = FloatGrid.Children.Capacity;
		}
		//================================================================
		private void SetupLongGrid() {
			int ix, rw, cl; string st, sn;
			double wd = LongGrid.Width / nLcol;
			GridLength gl = new GridLength(1, GridUnitType.Star);
			GridLength wgl = new GridLength(3, GridUnitType.Star);
			LongGrid.ColumnDefinitions.Clear();
			LongGrid.RowDefinitions.Clear();

			for (ix = 0; ix < nLcol; ix++) {
				ColumnDefinition col = new ColumnDefinition();
				col.Width = gl;
				LongGrid.ColumnDefinitions.Add(col);
			}
			for (ix = 0; ix < nLrow; ix++) {
				RowDefinition row = new RowDefinition();
				row.Height = gl;
				LongGrid.RowDefinitions.Add(row);
			}
			//fill children
			for (rw = 0; rw < nLrow; rw++) {
				for (cl = 0; cl < nLcol; cl++) {
					TextBlock TB = new TextBlock();
					st = (rw == 0 ? "Hdr" : "L");
					if (rw == 0) sn = sHex.Substring(cl, 1);
					else sn = sHex.Substring(rw - 1, 1) + sHex.Substring(cl, 1);
					TB.Name = st + sn;
					Grid.SetRow(TB, rw);
					SetTB(TB, cl, rw);
					Grid.SetColumn(TB, cl);
					if (rw == 0) TB.FontSize = 20;
					TB.TextAlignment = TextAlignment.Center;
					//					TB.MouseEnter += OnByteGrid_MouseEnter;
					LongGrid.Children.Add(TB);
				}
			}
		}
		//================================================================
		private void SetTB(TextBlock TB, int cl, int rw) {
			System.Windows.Media.FontFamily fontFamily = new FontFamily("Times New Roman");
			TB.Background = new SolidColorBrush(Colors.White);
			TB.Foreground = new SolidColorBrush(Colors.Black);
			TB.Text = TB.Name;
			TB.Margin = new Thickness(2, 2, 2, 2);
			TB.Padding = new Thickness(2, 4, 2, 4);
			TB.VerticalAlignment = VerticalAlignment.Stretch;
			TB.HorizontalAlignment = HorizontalAlignment.Stretch;
			TB.TextAlignment = (cl == 0) ? TextAlignment.Left : TextAlignment.Center;
			TB.FontFamily = fontFamily;
			TB.FontSize = szFont;
			TB.FontStyle = (rw == 0) ? FontStyles.Italic : FontStyles.Normal;
			TB.FontWeight = FontWeights.Bold;
			TB.MouseEnter += OnByteGrid_MouseEnter;
		}
		//================================================================
		private TextBlock FN(System.Windows.Controls.Grid E, string sN) {
			if (E == null) return null;
			int ix, nc = E.Children.Count;
			if (nc == 0) return null;
			for (ix = 0; ix < nc; ix++) {
				TextBlock tb = (TextBlock)E.Children[ix];
				if (tb == null) continue;
				if (tb.Name == sN) return tb;
			}
			return null;
		}
		//================================================================
		private void OnByteGrid_MouseEnter(object sender, MouseEventArgs e) {
			TextBlock TB = sender as TextBlock;
			if (TB == null) return;
			if (dmp == null || dmp.wtype != CDump.WD.Byte) return;
			string sv, sn = TB.Name; int rw, cl;
			if (sn.Contains("H") || !sn.Contains("B") || sn.Length < 3) { AddressTB.Text = ""; OffTB.Text = ""; return; }
			ulong adr = dmp.Address + dmp.ByteOffset;

			sv = sn.Substring(1, 2);
			rw = sHex.IndexOf(sv[0]);
			cl = sHex.IndexOf(sv[1]);
			int off, v = (rw * nByteCol + cl);
			ulong abadr = adr + (ulong)v;
			if (abadr >= (ulong)dmp.FileLength) return;

			string st = abadr.ToString("n0");
			off = v + (int)dmp.ByteOffset;
			AddressTB.Text = st;
			OffTB.Text = off.ToString("n0");
			TB.Background = AHit;
			TB = (TextBlock)AlphaGrid.Children[v + nAcol];
			TB.Background = AHit;
		}
		//================================================================
		private void OnByteGrid_MouseLeave(object sender, MouseEventArgs e) {
			TextBlock TB = sender as TextBlock;
			if (TB == null) return;
			if (dmp == null || dmp.wtype != CDump.WD.Byte) return;

			string sv, sn = TB.Name; int rw, cl;
			if (sn.Contains("Hdr") || !sn.Contains("B")) { AddressTB.Text = ""; OffTB.Text = ""; return; }
			ulong adr = dmp.Address + dmp.ByteOffset;

			sv = sn.Substring(1, 2);
			rw = sHex.IndexOf(sv[0]);
			cl = sHex.IndexOf(sv[1]);
			int v = rw * nAcol + cl;
			ulong abadr = adr + (ulong)v;

			if (abadr >= (ulong)dmp.FileLength) return;
			TB.Background = ANot;

			TB = (TextBlock)AlphaGrid.Children[v + nAcol];
			TB.Background = ANot;
		}
		//=================================================================
		private void OnAlphaGrid_MouseEnter(object sender, MouseEventArgs e) {
			TextBlock TB = sender as TextBlock;
			if (TB == null) return;
			if (dmp == null || dmp.wtype != CDump.WD.Byte) return;

			string sv, sn = TB.Name; int rw, cl;
			if (sn.Contains("H") || !sn.Contains("A")) { AddressTB.Text = ""; OffTB.Text = ""; return; }
			ulong adr = dmp.Address + dmp.ByteOffset;
			if (adr >= (ulong)dmp.FileLength) return;
			sv = sn.Substring(1, 2);
			rw = sHex.IndexOf(sv[0]);
			cl = sHex.IndexOf(sv[1]);
			int off, v = (rw * nAcol + cl);
			string st = (adr + (ulong)v).ToString("n0");
			off = v + (int)dmp.ByteOffset;
			ulong abadr = adr + (ulong)v;
			if (abadr >= (ulong)dmp.FileLength) return;

			TB.Background = AHit;
			AddressTB.Text = st;
			OffTB.Text = off.ToString("n0");
			TB = (TextBlock)ByteGrid.Children[v + nByteCol];
			TB.Background = AHit;
		}
		//===============================================================
		private void OnAlphaGrid_MouseLeave(object sender, MouseEventArgs e) {
			TextBlock TB = sender as TextBlock;
			if (TB == null) return;
			if (dmp == null || dmp.wtype != CDump.WD.Byte) return;

			string sv, sn = TB.Name; int rw, cl;
			if (sn.Contains("H") || !sn.Contains("A")) return;
			ulong adr = dmp.Address + dmp.ByteOffset;
			if (adr >= (ulong)dmp.FileLength) return;
			sv = sn.Substring(1, 2);
			rw = sHex.IndexOf(sv[0]);
			cl = sHex.IndexOf(sv[1]);
			int v = (rw * nAcol + cl);
			ulong abadr = adr + (ulong)v;
			if (abadr >= (ulong)dmp.FileLength) return;

			TB.Background = ANot;
			TB = (TextBlock)ByteGrid.Children[v + nByteCol];
			TB.Background = ANot;
		}
		//================================================================
		private void OnUnicode_TB_MouseLeave(object sender, MouseEventArgs e) {
			TextBlock TB = sender as TextBlock;
			if (TB == null) return;
			if (dmp == null || dmp.wtype != CDump.WD.Word) return;

			string sv, sn = TB.Name; int rw, cl;
			if (sn.Length != 3 || !sn.Contains("U")) return;
			ulong adr = dmp.Address + dmp.ByteOffset;
			if (adr >= (ulong)dmp.FileLength) return;

			sv = sn.Substring(1, 2);
			rw = sHex.IndexOf(sv[0]);
			cl = sHex.IndexOf(sv[1]);
			int v = (rw * nUcol + cl);
			ulong abadr = adr + (ulong)v;
			if (abadr >= (ulong)dmp.FileLength) return;

			TB.Background = ANot;
			TB = (TextBlock)WordGrid.Children[v + nWordCol];
			TB.Background = ANot;
		}
		//================================================================
		private void OnUnicode_TB_MouseEnter(object sender, MouseEventArgs e) {
			TextBlock TB = sender as TextBlock;
			if (TB == null) return;
			if (dmp == null || dmp.wtype != CDump.WD.Word) return;

			string sv, sn = TB.Name; int rw, cl;
			if (sn.Length != 3 || !sn.Contains("U")) { AddressTB.Text = ""; OffTB.Text = ""; return; }
			ulong adr = dmp.Address + dmp.ByteOffset;

			sv = sn.Substring(1, 2);
			rw = sHex.IndexOf(sv[0]);
			cl = sHex.IndexOf(sv[1]);

			int off, v = (rw * nUcol + cl);
			string st = (adr + (ulong)v).ToString("n0");
			off = v + (int)dmp.ByteOffset;
			ulong abadr = adr + (ulong)v;
			if (abadr >= (ulong)dmp.FileLength) return;

			TB.Background = AHit;
			AddressTB.Text = st;
			OffTB.Text = off.ToString("n0");
			TB = (TextBlock)WordGrid.Children[v + nWordCol];
			TB.Background = AHit;
		}
		//================================================================
		private void OnWordGrid_MouseLeave(object sender, MouseEventArgs e) {
			TextBlock TB = sender as TextBlock;
			if (TB == null) return;
			if (dmp == null || dmp.wtype != CDump.WD.Word) return;

			string sv, sn = TB.Name; int rw, cl;
			if (sn.Length != 3 || !sn.Contains("W")) return;
			ulong adr = dmp.Address + dmp.ByteOffset;

			sv = sn.Substring(1, 2);
			rw = sHex.IndexOf(sv[0]);
			cl = sHex.IndexOf(sv[1]);
			int v = (rw * nWordCol + cl);
			ulong abadr = adr + (ulong)v;
			if (abadr >= (ulong)dmp.FileLength) return;

			TB.Background = ANot;
			TB = (TextBlock)UniGrid.Children[v + nUcol];
			TB.Background = ANot;
		}
		//=================================================================
		private void OnWordGrid_MouseEnter(object sender, MouseEventArgs e) {
			TextBlock TB = sender as TextBlock;
			if (TB == null) return;
			if (dmp == null || dmp.wtype != CDump.WD.Word) return;

			string sv, sn = TB.Name; int rw, cl;
			if (sn.Length != 3 || !sn.Contains("W")) { AddressTB.Text = ""; OffTB.Text = ""; return; }
			ulong adr = dmp.Address + dmp.ByteOffset;

			sv = sn.Substring(1, 2);
			rw = sHex.IndexOf(sv[0]);
			cl = sHex.IndexOf(sv[1]);

			int off, v = (rw * nWordCol + cl);
			string st = (adr + (ulong)v).ToString("n0");
			off = v + (int)dmp.ByteOffset;
			ulong abadr = adr + (ulong)v;
			if (abadr >= (ulong)dmp.FileLength) return;

			TB.Background = AHit;
			AddressTB.Text = st;
			OffTB.Text = off.ToString("n0");
			TB = (TextBlock)UniGrid.Children[v + nUcol];
			TB.Background = AHit;

		}
		//================================================================
		private void ResetButtons() {
			ByteBtn.Background = BtnBack;
			WordBtn.Background = BtnBack;
			IntBtn.Background = BtnBack;
			FloatBtn.Background = BtnBack;
			LongBtn.Background = BtnBack;
			DoubleBtn.Background = BtnBack;
			Color4Btn.Background = BtnBack;
			Color3Btn.Background = BtnBack;
		}
		//==================================================================
		private void FillDoubleGrid() {
			if (dmp == null) throw new Exception("Null dmp object");
			if (DoubleGrid.Children.Count < nDcol * nDrow) throw new Exception("DoubleGrid Children count error ");
			int ix; long addr = (long)dmp.AbsAddress, flen = dmp.FileLength;
			string[] sa = dmp.Get2HeaderArray;
			TextBlock tb = null;

			for (ix = 0; ix < sa.Length; ix++) {
				tb = (TextBlock)DoubleGrid.Children[ix];
				tb.Foreground = SCBFore;
				tb.Background = SCBack;
				tb.FontStyle = FontStyles.Normal;
				tb.Text = sa[ix];
			}
			ix = 0;
			sa = dmp.GetDoubleArray;
			for (int row = 1; row < nDrow; row++) {
				for (int col = 0; col < nDcol; col++) {
					tb = (TextBlock)DoubleGrid.Children[row * nDcol + col];
					string sn = tb.Name;
					tb.Background = (ix + addr < flen) ? ABrush : PBrush;
					tb.Text = sa[ix++];
				}
			}
			while (ix < sa.Length) sa[ix++] = "n/a";
		}
		//==================================================================
		private void FillLongGrid() {
			if (dmp == null) throw new Exception("Null dmp object");
			if (LongGrid.Children.Count < nLcol * nLrow) throw new Exception("LongGrid Children count error ");
			int ix; long addr = (long)dmp.AbsAddress, flen = dmp.FileLength;
			string[] sa = dmp.Get2HeaderArray;
			TextBlock tb = null;

			for (ix = 0; ix < sa.Length; ix++) {
				tb = (TextBlock)LongGrid.Children[ix];
				tb.Foreground = SCBFore;
				tb.Background = SCBack;
				tb.FontStyle = FontStyles.Normal;
				tb.Text = sa[ix];
			}
			ix = 0;
			sa = dmp.GetLongArray;
			for (int row = 1; row < nLrow; row++) {
				for (int col = 0; col < nLcol; col++) {
					tb = (TextBlock)LongGrid.Children[row * nLcol + col];
					string sn = tb.Name;
					tb.Background = (ix + addr < flen) ? ABrush : PBrush;
					tb.Text = sa[ix++];
				}
			}
			while (ix < sa.Length) sa[ix++] = "n/a";
		}
		//==================================================================
		private void FIllFloatGrid() {
			if (dmp == null) throw new Exception("Null dmp object");
			if (FloatGrid.Children.Count < nFcol * nFrow) throw new Exception("FLoatGrid Children count error ");
			int ix; long addr = (long)dmp.AbsAddress, flen = dmp.FileLength;
			string[] sa = dmp.Get4HeaderArray;
			TextBlock tb = null;

			for (ix = 0; ix < sa.Length; ix++) {
				tb = (TextBlock)FloatGrid.Children[ix];
				tb.Foreground = SCBFore;
				tb.Background = SCBack;
				tb.FontStyle = FontStyles.Normal;
				tb.Text = sa[ix];
			}
			ix = 0;
			sa = dmp.GetFloatArray;
			for (int row = 1; row < nFrow; row++) {
				for (int col = 0; col < nFcol; col++) {
					tb = (TextBlock)FloatGrid.Children[row * nFcol + col];
					tb.Background = (ix + addr < flen) ? ABrush : PBrush;
					tb.Text = sa[ix++];
				}
			}
			while (ix < sa.Length) sa[ix++] = "n/a";
		}
		//=================================================================
		private void FIllIntGrid() {
			if (dmp == null) throw new Exception("Null dmp object");
			if (IntGrid.Children.Count < nIcol * nIrow) throw new Exception("IntGrid Children count error ");
			int ix; long addr = (long)dmp.AbsAddress, flen = dmp.FileLength;
			string[] sa = dmp.Get4HeaderArray;
			TextBlock tb = null;

			//fill header 
			for (ix = 0; ix < sa.Length; ix++) {
				tb = (TextBlock)IntGrid.Children[ix];
				tb.Foreground = SCBFore;
				tb.Background = SCBack;
				tb.FontStyle = FontStyles.Normal;
				tb.Text = sa[ix];
			}
			//fill body
			ix = 0;
			sa = dmp.GetIntArray; ;
			for (int row = 1; row < nIrow; row++) {
				for (int col = 0; col < nIcol; col++) {
					tb = (TextBlock)IntGrid.Children[row * nIcol + col];
					string sn = tb.Name;
					tb.Background = (ix + addr < flen) ? ABrush : PBrush;
					tb.Text = sa[ix++];
				}
			}
			while (ix < sa.Length) sa[ix++] = "n/a";
		}
		//================================================================
		private void FillColorGrid() {
			if (dmp == null) throw new Exception("Null dmp object");
			if (ColorGrid.Children.Count < nIcol * nIrow) throw new Exception("ColorGrid Children count error ");
			int ix; long addr = (long)dmp.AbsAddress, flen = dmp.FileLength;
			System.Windows.Media.Color swc;
			string[] sa = dmp.Get4HeaderArray;

			TextBlock tb = null;

			for (ix = 0; ix < sa.Length; ix++) {
				tb = (TextBlock)ColorGrid.Children[ix];
				tb.Foreground = SCBFore;
				tb.Background = SCBack;
				tb.FontStyle = FontStyles.Normal;
				tb.Text = sa[ix];
			}
			System.Drawing.Color[] ca = (bRGB4) ? dmp.GetColorArrayARGB : dmp.GetColorArrayRGB;

			ix = 0;
			sa = dmp.GetIntArray; ;
			for (int row = 1; row < nIrow; row++) {
				for (int col = 0; col < nIcol; col++) {
					tb = (TextBlock)ColorGrid.Children[row * nIcol + col];
					if (ix >= ca.Length) break;
					swc = CSNC.Drawing2Media(ca[ix++]);
					string sn = CSNC.xSWMColor(swc);
					//					tb.Background = (ix + addr < flen) ? ABrush : PBrush;
					tb.Background = new SolidColorBrush(swc);
					tb.Text = sn;
				}
			}
			while (ix < sa.Length) sa[ix++] = "n/a";

		}
		//================================================================
		private void FillAbsGrid() {
			double wd = AbsGrid.RenderSize.Width;
			string[] sa = dmp.GetAbsAddess;
			int ix, nc = AbsGrid.Children.Count;
			if (sa.Length != nc) return;
			long baddr = dmp.BlockNumber * dmp.BufLength, last = dmp.FileLength;
			for (ix = 0; ix < nc; ix++) {
				TextBlock TB = (TextBlock)AbsGrid.Children[ix];
				TB.TextAlignment = (ix == 0) ? TextAlignment.Center : TextAlignment.Right;
				TB.FontSize = wd / 8;
				TB.FontStyle = FontStyles.Normal;
				TB.Text = sa[ix];
				if (ix > 0) {
					TB.Background = (baddr + (long)ix < last) ? SCBWhite : SCBLGray;
				}
			}
		}
		//================================================================
		private void FillOffGrid() {
			double wd = AbsGrid.RenderSize.Width;
			string[] sa = dmp.GetOffset;
			int ix, nc = OffGrid.Children.Count;
			if (sa.Length != nc) return;
			for (ix = 0; ix < nc; ix++) {
				TextBlock TB = (TextBlock)OffGrid.Children[ix];
				TB.FontStyle = FontStyles.Normal;
				TB.FontSize = wd / 8;
				TB.TextAlignment = (ix == 0) ? TextAlignment.Center : TextAlignment.Right;
				TB.Text = sa[ix];
			}
		}
		//================================================================
		private void FillAlphaGrid() {
			double wd = AbsGrid.RenderSize.Width;
			if (dmp == null) throw new Exception("Null dmp object");
			int i, ix, alen = 16 * 17; long addr = (long)dmp.AbsAddress, flen = dmp.FileLength;
			if (AlphaGrid.Children.Count < alen) throw new Exception("AlphaGrid Children count error ");
			string[] sa = dmp.GetAlphaArray; TextBlock tb = null;
			for (i = 0, ix = 16; ix < alen; ix++) {
				tb = (TextBlock)AlphaGrid.Children[ix];
				tb.Background = (i + addr < flen) ? ABrush : PBrush;
				tb.Text = sa[i++];
			}
			sa = dmp.GetByteHeaderArray;
			for (ix = 0; ix < sa.Length; ix++) {
				tb = (TextBlock)AlphaGrid.Children[ix];
				tb.Foreground = SCBFore;
				tb.Background = SCBack;
				tb.FontStyle = FontStyles.Normal;
				tb.Text = sa[ix];
			}
		}
		//=================================================================
		private void FillByteGrid() {
			double wd = AbsGrid.RenderSize.Width;
			if (dmp == null) throw new Exception("Null dmp object");
			if (ByteGrid.Children.Count < nByteCol * nByteRow) throw new Exception("ByteGrid Children count error ");
			int ix; long addr = (long)dmp.AbsAddress, flen = dmp.FileLength;
			string[] sa = dmp.GetByteHeaderArray; TextBlock tb = null;

			for (ix = 0; ix < sa.Length; ix++) {
				tb = (TextBlock)ByteGrid.Children[ix];
				tb.Foreground = SCBFore;
				tb.Background = SCBack;
				tb.FontStyle = FontStyles.Normal;
				tb.Text = sa[ix];
			}
			ix = 0;
			sa = dmp.GetByteArray; ;
			for (int row = 1; row < nByteRow; row++) {
				for (int col = 0; col < nByteCol; col++) {
					tb = (TextBlock)ByteGrid.Children[row * nByteCol + col];
					tb.Background = (ix + addr < flen) ? ABrush : PBrush;
					tb.Text = sa[ix++];
					tb.FontSize = wd / 8;
				}
			}
		}
		//================================================================
		private void FillWordGrid() {
			if (dmp == null) throw new Exception("Null dmp object");
			if (WordGrid.Children.Count < nWordCol * nWordRow) throw new Exception("WordGrid Children count error ");
			int ix; long addr = (long)dmp.AbsAddress, flen = dmp.FileLength;
			string[] sa = dmp.GetWordHeaderArray;
			TextBlock tb = null;

			for (ix = 0; ix < sa.Length; ix++) {
				tb = (TextBlock)WordGrid.Children[ix];
				tb.Foreground = SCBFore;
				tb.Background = SCBack;
				tb.FontStyle = FontStyles.Normal;
				tb.Text = sa[ix];
			}
			ix = 0;
			sa = dmp.GetWordArray; ;
			for (int row = 1; row < nWordRow; row++) {
				for (int col = 0; col < nWordCol; col++) {
					tb = (TextBlock)WordGrid.Children[row * nWordCol + col];
					string sn = tb.Name;
					tb.Background = (ix + addr < flen) ? ABrush : PBrush;
					tb.Text = sa[ix++];
				}
			}
			while (ix < sa.Length) sa[ix++] = "n/a";
		}
		//================================================================
		private void FillUniGrid()/*Fill unicode area, use Endian setting*/ {
			if (dmp == null) throw new Exception("Null dmp object");
			if (UniGrid.Children.Count < nUcol * nUrow) throw new Exception("UniGrid Children count error ");
			int ix; long addr = (long)dmp.AbsAddress, flen = dmp.FileLength;
			string[] sa = dmp.GetHeaderArray(16);
			TextBlock tb = null;

			for (ix = 0; ix < sa.Length; ix++) {
				tb = (TextBlock)UniGrid.Children[ix];
				tb.Foreground = SCBFore;
				tb.Background = SCBack;
				tb.FontStyle = FontStyles.Normal;
				tb.Text = sa[ix];
			}
			char[] ca = dmp.GetUnicodeArray;
			ix = 0;
			for (int row = 1; row < nUrow; row++) {
				for (int col = 0; col < nUcol; col++) {
					tb = (TextBlock)UniGrid.Children[row * nUcol + col];
					string sn = tb.Name;
					tb.Background = (ix + addr < flen) ? ABrush : PBrush;
					tb.Text = ca[ix++].ToString();
				}
			}
			while (ix < ca.Length) sa[ix++] = "n/a";

		}
		//================================================================
		private void OnClosed(object sender, EventArgs e) {
		}
		//================================================================
		private string sFilter = "Any file(*.*)|*.*";
		//================================================================
		public CSN_Asm Info { get => info; set => info = value; }
		//================================================================
		private void OnOpenBtn(object sender, RoutedEventArgs e) {
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.AddExtension = true;
			dlg.ShowReadOnly = true;
			dlg.ReadOnlyChecked = true;
			dlg.FileName = "*.*";
			dlg.Filter = sFilter;
			dlg.Multiselect = false;
			dlg.Title = "Select file to examine:";
			if (dlg.ShowDialog() != true) return;
			string spath = dlg.FileName;
			if (!File.Exists(spath)) return;
			dmp = new CFileDump(spath);
			dmp.Address = 0;
			dmp.isSigned = false;
			fi = new FileInfo(dmp.sPath);
			Display();
		}
		//================================================================
		private void OnAbout(object sender, RoutedEventArgs e) {
			AboutFA dlg = new AboutFA();
			dlg.szGrid = MainGrid.RenderSize;
			dlg.sPath = (dmp == null ? "No file" : dmp.sPath);
			dlg.ShowDialog();
		}
		//================================================================
		private void OnClass(object sender, RoutedEventArgs e) {

			Button B = sender as Button;
			if (dmp == null) return;
			switch (B.Name) {
				default:
				case "ByteBtn": dmp.wtype = CDump.WD.Byte; break;
				case "WordBtn": dmp.wtype = CDump.WD.Word; break;
				case "IntBtn": dmp.wtype = CDump.WD.Int; break;
				case "FloatBtn": dmp.wtype = CDump.WD.Float; break;
				case "LongBtn": dmp.wtype = CDump.WD.Long; break;
				case "DoubleBtn": dmp.wtype = CDump.WD.Double; break;
				case "Color3Btn": bRGB4 = false; dmp.wtype = CDump.WD.Color; break;
				case "Color4Btn": bRGB4 = true; dmp.wtype = CDump.WD.Color; break;
			}
			Display();
		}
		//=============================================================
		private void OnSign(object sender, RoutedEventArgs e) {
			if (dmp == null) return;
			dmp.SignUnsign();
			Display();
		}
		//=============================================================
		private void OnHex(object sender, RoutedEventArgs e) {
			if (dmp == null) return;
			dmp.isHex = !dmp.isHex;
			Display();
		}
		//===============================================================
		private void OnEndian(object sender, RoutedEventArgs e) {
			if (dmp == null) return;
			dmp.isBigEndian = !dmp.isBigEndian;
			Display();
		}
		//================================================================
		private void OnDoc2Uni(object sender, RoutedEventArgs e) {

		}
		//================================================================
		private void OnSLPM(object sender, RoutedEventArgs e)/*4 buttons: start, Last, +1, -1*/	{
			Button B = sender as Button;
			if (B == null) return;
			if (dmp == null) return;
			switch (B.Name) {
				default: return;
				case "MainSLZero": dmp.ReadBlock(0); break;
				case "MainSLMinus": Decrease(); break;
				case "MainSLPlus": Increase(); break;
				case "MainSLLast": dmp.ReadBlock(dmp.LastBlock); break;
			}
			SetSlider();
		}
		//==================================================================
		private void OnMainSLChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			Slider SL = sender as Slider;
			if (SL == null || dmp == null) return;
			//adjust dmp current block to slider position
			double ratio = (double)SL.Value / (double)SL.Maximum;
			int block = (int)(0.5 + ratio * dmp.LastBlock);

			if (block != dmp.BlockNumber) dmp.ReadBlock(block);
			Display();
		}
		//===============================================================
		private void OnSliderKey(object sender, KeyEventArgs e)//user keys up,down arrow, home, end
		{
			StackPanel SP = sender as StackPanel;
			if (SP == null) return;
			Slider SL = MainSL;
			if (dmp == null || dmp.LastBlock == 0) return;
			Key key = e.Key;
			switch (key) {
				default: return;
				case Key.Up: Decrease(); break;
				case Key.Down: Increase(); return;
				case Key.Home: dmp.ReadBlock(0); break;
				case Key.End: dmp.ReadBlock(dmp.LastBlock); break;
			}
			dmp.Address = (ulong)(dmp.BlockNumber * dmp.BufLength);
			SetSlider();
		}
		//=================================================================
		private void OnWriteTestDoc(object sender, RoutedEventArgs e) {
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.AddExtension = true;
			dlg.DefaultExt = "tst";
			dlg.FileName = CSNUtil.yymmdd(DateTime.Now) + "-test.tst";
			dlg.Filter = "Test document (*.tst)|*.tst|Any filename (*.*)|*.*";
			dlg.Title = "Enter file name to generate a new test document";
			if (dlg.ShowDialog() != true) return;
			WriteTestDocument(dlg.FileName);
		}
		//==================================================================
		private void WriteTestDocument(string fileName) {
			FileStream fs = null; BinaryWriter sr = null;
			int ix, iy; byte upper, lower; string se; byte b; ushort us;
			try {
				fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
				sr = new BinaryWriter(fs);
				//write 1 block for byte
				for (ix = 0; ix <= Byte.MaxValue; ix++) {
					b = (byte)ix;
					sr.Write(b);
				}
				//write two x two blocks for unicode.  1st littleEndian, 2nd set BigEndian
				for (iy = 0; iy < byte.MaxValue; iy++) {
					lower = (byte)iy;
					upper = 0x01;
					us = (ushort)(upper << 8);
					us += lower;
					sr.Write(us);
				}
				for (iy = 0; iy < byte.MaxValue; iy++) {
					lower = (byte)iy;
					upper = 0x01;
					us = (ushort)(lower << 8);
					us += upper;
					sr.Write(us);
				}

				sr.Flush();
				sr.Close();
				fs.Close();
			} catch (Exception exp) {
				se = "Error while writing file:\n" + fileName
				+ "\nError code: " + exp.Message +
				"\nStack: " + exp.StackTrace;
				MessageBox.Show(se);
			} finally {
				if (sr != null) sr.Dispose();
				if (fs != null) fs.Dispose();
			}
		}
		//================================================================
		private void Increase() {
			if (dmp == null) return;
			if (dmp.BlockNumber < dmp.LastBlock) dmp.ReadBlock(dmp.BlockNumber + 1);
			SetSlider();
		}
		private void Decrease() {
			if (dmp == null) return;
			if (dmp.BlockNumber > 0) dmp.ReadBlock(dmp.BlockNumber - 1);
			SetSlider();
		}
		//=================================================================
		private void SetSlider() {
			if (dmp == null) return;
			double ratio = (double)dmp.BlockNumber / (double)dmp.LastBlock;
			double v = MainSL.Maximum * ratio;
			MainSL.Value = v;
		}
		//=================================================================
		private void OnOffset(object sender, RoutedEventArgs e) {
			Button B = sender as Button;
			if (B == null) return;
			switch (B.Name) {
				default: break;
				case "Offset0": dmp.ResetByteOffset(); break;
				case "OffsetP1": dmp.IncrementByteOffset(); break;
				case "OffsetM1": dmp.DecrementByteOffset(); break;
			}
			Display();
		}
		//================================================================
		private void OnNewAddress(object sender, KeyEventArgs e) {
			if (dmp == null) return;
			Key key = e.Key;
			if (key != Key.Enter) return;
			string s = AddressTB.Text;
			AddressTB.Text = " ";
			bool bOK = SNL.CParser.TryParseExp(s, out double dv);
			if(!bOK) return;
			ulong addr = (ulong)dv;
//			len = (ulong)dmp.BufLength;
//			addr = 0;
//			if (!ulong.TryParse(s, out addr)) return;
			dmp.ReadAddress(addr);
			Display();
		}
	}
}
