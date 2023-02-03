//*********************************************************
//This file contains the Windows form printing package
//for the WPFDump package
//*********************************************************
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Documents;
using System.Drawing;
using System.Drawing.Printing;
using Microsoft.Win32;
using SNL;
using SNDump;
using System.Windows.Interop;
using System.Text;

namespace FileAnalysis {
	public partial class MainWindow : Window {
		private bool bPreview = false;
		private string sVF = "Times New Roman";
		private string sFF = "Courier New";
		private StringFormat sfl = new StringFormat();
		private StringFormat sfc = new StringFormat();
		private StringFormat sfr = new StringFormat();
		public PrintDocument PD;
		public PrinterSettings PS;
		//================================================================
		private void SetupPrintForm() {
			sfl.Alignment = StringAlignment.Near;
			sfc.Alignment = StringAlignment.Center;
			sfr.Alignment = StringAlignment.Far;
			PD = new PrintDocument();
			PD.DocumentName = "File Analysis page";
		}
		//================================================================
		private void OnPrinterSetup(object sender, RoutedEventArgs e) /*called when user selects "Setup" button */	{
			string sPrinter = PrintersCB.SelectedItem.ToString();
			if (PS == null) return;
			PS.PrinterName = sPrinter;

			if (!PS.IsValid) {
				System.Windows.MessageBox.Show(PS.PrinterName + " not a valid printer.");
				return;
			}
			WindowInteropHelper helper = new WindowInteropHelper(this);
			if (helper == null) return;
			CSN_Printing.OpenPrinterPropertiesDialog(PS, helper.Handle);

		}
		//================================================================
		private void SetupPrinterList() {
			int dix = -1, ix; PD = new PrintDocument();
			//setup printer list
			PS = PD.DefaultPageSettings.PrinterSettings;
			string dftptr = PS.PrinterName;
			PrintersCB.Items.Clear();
			for (ix = 0; ix < PrinterSettings.InstalledPrinters.Count; ix++) {
				PrintersCB.Items.Add(PrinterSettings.InstalledPrinters[ix]);
				if (PrinterSettings.InstalledPrinters[ix] == dftptr) dix = ix;
			}
			if (dix >= 0) PrintersCB.SelectedIndex = dix;

			int ne = PrintersCB.Items.Count;
		}
		//================================================================
		private void SetPrinterCB(string sP)    /*try to set the selectedindex to point to sP*/	{
			int ix, ne = PrintersCB.Items.Count; string s;
			if (ne == 0) return;
			for (ix = 0; ix < ne; ix++) {
				s = (string)PrintersCB.Items[ix];
				if (s.CompareTo(sP) == 0) { PrintersCB.SelectedIndex = ix; break; }
			}
		}
		//===============================================================
		private void SetupForm(Form form) {
			string st;
			form.WindowState = FormWindowState.Maximized;
			form.ShowIcon = true;
			form.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			form.MaximizeBox = false;
			st = " To print on " + PS.PrinterName + " click icon on far left.";
			form.Text = st;
		}
		//================================================================
		private void OnPrintPreview(object sender, RoutedEventArgs e) {
			PS.PrinterName = PrintersCB.SelectedItem.ToString();

			PrintPreviewDialog PrevDlg = new PrintPreviewDialog();
			PrintDocument pd = new PrintDocument();
			PrevDlg.Document = pd;
			pd.DocumentName = "File Contents Document";

			PrevDlg.Document.PrinterSettings = PS;
			pd.PrinterSettings = PS;
			pd.PrinterSettings.DefaultPageSettings.Landscape = true;

			pd.PrintPage += new PrintPageEventHandler(PrintDumpPage);

			bPreview = pd.PrintController.IsPreview;

			//setup form of Preview dialog
			Form form = PrevDlg.FindForm();
			SetupForm(form);
			if (PrevDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) PS = PrevDlg.Document.PrinterSettings;
			pd.Dispose();
			PrevDlg.Dispose();
		}
		//==============================================================
		private void PrintDumpPage(object sender, PrintPageEventArgs e) {
			string st, sTitle; float x, dx, dy, y, yb, em, mid;
			int ix, iy, iu, nlines = (dmp == null) ? 0 : dmp.BufLength / 16;
			char c;

			PrintDocument pd = sender as PrintDocument;
			bool isPreview = pd.PrintController.IsPreview;

			Graphics g = e.Graphics;
			SizeF hm = new SizeF(e.PageSettings.HardMarginX, e.PageSettings.HardMarginY);
			RectangleF rPA = e.MarginBounds;
			if (!isPreview) { rPA.X -= hm.Width; rPA.Y -= hm.Height; }

			if (dmp == null || fi == null) {
				PrtHdrFtr(g, rPA, hm, "No file loaded.", "", 0);
				e.HasMorePages = false;
				return;
			}
			mid = rPA.Left + rPA.Width / 2;
			Pen pen = new Pen(Color.DarkRed, 2.0f);
			Pen npen = new Pen(Color.Black, 0.1f);
			long nblocks = 1 + (fi.Length >> 8);
			sTitle = "Block " + (1 + dmp.BlockNumber).ToString("n0") + "/" + nblocks.ToString("n0");
			PrtHdrFtr(g, rPA, hm, sTitle, dmp.sPath, 0);
			em = 1.20f * rPA.Width / 80.0f;

			Font font = new Font(sFF, em, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
			y = rPA.Y + font.Height; x = rPA.X;
			//			fi = new FileInfo(dmp.sPath);
			if (fi != null) {
				st = "Length: " + fi.Length.ToString("n0")
					+ " B, created: " + fi.CreationTime.ToShortDateString()
					+ ", modified: " + fi.LastWriteTime.ToShortDateString()
					+ ", attributes: " + fi.Attributes.ToString();
				g.DrawString(st, font, Brushes.Black, x + rPA.Width / 2, y, sfc); y += 2 * font.Height;
			}
			//print ascii
			g.DrawString(dmp.sHdr, font, Brushes.Black, x, y, sfl); y += font.Height * 2;
			for (int i = 0; i < nlines; i++) {
				st = dmp.sLine(i * 16);
				g.DrawString(st, font, Brushes.Black, x, y, sfl);
				y += font.Height;
				g.DrawLine(npen, rPA.Left, y, rPA.Right, y);
			}
			char[] uniarray = null;
			//print unicode
			y += font.Height;
			g.DrawString("Unicode", font, Brushes.Black, mid, y, sfc); y += font.Height;
			g.DrawLine(Pens.Black, rPA.Left, y, rPA.Right, y);
			dx = rPA.Width / 33;
			yb = y + dx * 5;
			dy = yb - y;
			Font sfont = new Font(sFF, dx / 5, System.Drawing.FontStyle.Italic | System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
			for (ix = 1; ix < 33; ix++) {
				x = rPA.Left + ix * dx;
				g.DrawLine(Pens.Black, x, y, x, yb);
				g.DrawString((ix - 1).ToString(), font, Brushes.Black, x + dx / 2, y + font.Height / 2, sfc);
			}

			uniarray = dmp.GetUnicodeArray;
			StringBuilder SB = new StringBuilder(1+uniarray.Length);
			for (iu = 0, ix = 1; ix < 5; ix++) {
				var v = (ix - 1) * 32;
				y += dx;
				for (iy = 1; iy < 33; iy++) {
					c = uniarray[iu++];
					x = rPA.Left + dx * iy + dx / 2;
					if(char.IsLetterOrDigit(c)) SB.Append(c); else SB.Append("_");
					st = c.ToString();
					g.DrawString(st, font, Brushes.Black, x, y + font.Height / 2, sfc);
					st = CDump.sCharAttrs(c);
					g.DrawString(st, sfont, Brushes.Black, x, y, sfc);
				}
				g.DrawLine(Pens.Black, rPA.Left, y, rPA.Right, y);
				g.DrawString(v.ToString(), font, Brushes.Black, rPA.Left + dx / 2, y + font.Height / 2, sfc);
			}
			g.DrawLine(Pens.Black, rPA.Left, yb, rPA.Right, yb); yb += font.Height;

			//print unicode array
			g.DrawString(SB.ToString(0,32),font,Brushes.Black,mid,yb,sfc); yb += font.Height;
			g.DrawString(SB.ToString(32,32),font,Brushes.Black,mid,yb,sfc); yb += font.Height;
			g.DrawString(SB.ToString(64,32),font,Brushes.Black,mid,yb,sfc); yb += font.Height;
			g.DrawString(SB.ToString(96,32),font,Brushes.Black,mid,yb,sfc); yb += font.Height;
			yb+=font.Height;
			g.DrawString(CDump.sCharAttrExplanation, font, Brushes.Black, mid, yb, sfc);

			yb = rPA.Bottom - 2 * font.Height;
			y = yb;
			st = dmp.sSettings + ", " + dmp.sEndian;
			g.DrawString(st, font, Brushes.Black, mid, y, sfc); y += font.Height;
			st = "Length: " + fi.Length.ToString("N0") + " bytes, last updated: " + fi.LastWriteTime.ToShortDateString();
			g.DrawString(st, font, Brushes.Black, mid, y, sfc); y += font.Height;

			e.HasMorePages = false;
			sfont.Dispose();
			font.Dispose();
			pen.Dispose();

		}
		//=============================================================
		private void PrtHdrFtr(Graphics g, RectangleF rPA, SizeF hm, string sTitle, string subtitle, uint pgno) {
			float x, y, em; DateTime now = DateTime.Now;

			Rectangle rr = new Rectangle((int)rPA.X, (int)rPA.Y, (int)rPA.Width, (int)rPA.Height);
			g.DrawRectangle(Pens.Black, rr);
			em = (rPA.Y - hm.Height) / 2.5f;
			Font bfont = new Font(sVF, em, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
			em = bfont.Size * 0.5f;
			Font sfont = new Font(sVF, em, System.Drawing.FontStyle.Italic, GraphicsUnit.Pixel);
			x = rPA.X + rPA.Width / 2; y = rPA.Y - sfont.Height;
			sfc.Alignment = StringAlignment.Center;
			g.DrawString(subtitle, sfont, Brushes.Black, x, y, sfc); y -= bfont.Height;
			g.DrawString(sTitle, bfont, Brushes.Black, x, y, sfc);

			sfont.Dispose();
			em = rPA.Width / 100.0f;
			sfont = new Font(sVF, em, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);

			g.DrawString(Info.sCopyright, sfont, Brushes.Black, rPA.Left, rPA.Bottom, sfl);
			g.DrawString(now.ToShortDateString(), sfont, Brushes.Black, rPA.Left + rPA.Width / 2.0f, rPA.Bottom, sfc);
			g.DrawString(Info.sTitle + " ver " + Info.sVersion, sfont, Brushes.Black, rPA.Right, rPA.Bottom, sfr);

			//clean up
			bfont.Dispose();
			sfont.Dispose();
			return;
		}
	}
}
