using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
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

using SNL;
using SNDump;

namespace FileAnalysis {
	/////////////////////////////////////////////////////////////////////
	public partial class MainWindow : Window {
		public static Color[] WCA = {Colors.Chartreuse,Colors.DarkGreen,Colors.Brown,Colors.CadetBlue,
		Colors.DarkKhaki,Colors.DarkBlue,Colors.Magenta,Colors.DarkOrange,Colors.Teal,Colors.Violet};
		//.................................................................
		public static Color GetWColor(int ix) { return ix < WCA.Length ? WCA[ix] : Colors.Black; }
		public bool is16 = true;
		public Rect DCRect = new Rect();
		public Rect GridRect = new Rect();

		//visual variables
		private Color CurveColor = Colors.DarkRed;
		private Rect RA = new Rect();
		//===============================================================
		private void DrawUnicode(DrawingCanvas DC, CFileDump w) {
			const int nCol = 33, nRow = 5;
			Size sz = new Size(DC.ActualWidth, DC.ActualHeight);    //actual size of Canvas
			Rect R = new Rect(0, 0, sz.Width, sz.Height);
			string st; double x, y, dx = R.Width / nCol, dy = R.Height / nRow;
			const int fsz = 24; int ix, ixrow, ixcol;
			char C; StringBuilder sb = new StringBuilder(10);

			DC.ClearAllVisuals();
			DrawingVisual v = new DrawingVisual();
			DrawingContext dc = v.RenderOpen();
			dc.DrawRectangle(Brushes.LightGray, null, R);
			Pen pen = new Pen(Brushes.Black, 0.5);

			//draw grid
			for (ix = 0; ix < nCol - 1; ix++) {
				x = R.Left + (1 + ix) * dx;
				dc.DrawLine(pen, new Point(x, R.Top), new Point(x, R.Bottom));
				DrawText(dc, new Point(x + dx / 2, R.Top), ix.ToString(), fsz, TextAlignment.Center);
			}
			for (ix = 1; ix < nRow; ix++) {
				y = R.Top + ix * dy;
				dc.DrawLine(pen, new Point(R.Left, y), new Point(R.Right, y));
				DrawText(dc, new Point(R.Left, y), ((ix - 1) * 32).ToString(), fsz, TextAlignment.Left);
			}
			//draw unicode characters, place into grid
			ushort u = 0;
			for (ixrow = 0; ixrow < nRow - 1; ixrow++) {
				y = R.Top + dy + ixrow * dy;
				for (ixcol = 0; ixcol < nCol - 1; ixcol++) {
					x = R.Left + dx + ixcol * dx + dx / 2;
					u = (ushort)(ixrow * (nCol - 1) + ixcol);
					int iv = ixrow * (nCol - 1) + ixcol;
					C = dmp.Unicode(iv);
					st = iv.ToString();
					sb.Clear(); sb.Append(C);
					DrawText(dc, new Point(x, y), sb.ToString(), fsz, TextAlignment.Center);
				}
			}
			dc.Close();
			DC.AddVisual(v);
		}
		//===============================================================
		private void DrawPrimary(DrawingCanvas DC, CFileDump w) {
			Size sz = new Size(DC.ActualWidth, DC.ActualHeight);    //actual size of Canvas
			Rect R = new Rect(0, 0, sz.Width, sz.Height);
			string st; double x, y;
			const int fsz = 28;

			DC.ClearAllVisuals();
			DrawingVisual v = new DrawingVisual();
			DrawingContext dc = v.RenderOpen();
			dc.DrawRectangle(Brushes.LightGoldenrodYellow, null, R);
			Pen pen = new Pen(Brushes.Black, 0.5);

			st = (w == null) ? "No file present" :
				"Block #"
				+ (1 + w.BlockNumber).ToString("n0") + '/'
				+ (1 + w.LastBlock).ToString("n0");
			x = R.Left + R.Width / 2; y = R.Top;
			DrawText(dc, new Point(x, y), st, fsz, TextAlignment.Center); y += fsz;
			//			st = "012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567";
			//			DrawText(dc, new Point(R.Left, y), st, fsz, TextAlignment.Left);

			if (w != null) {
				y += fsz;
				st = w.sHdr;
				DrawText(dc, new Point(R.Left, y), st, fsz, TextAlignment.Left); y += fsz;
				for (int ix = 0; ix < 16; ix++) {
					st = w.sLine(ix * 16);
					DrawText(dc, new Point(R.Left, y), st, fsz, TextAlignment.Left); y += fsz;
				}
			}
			RA.X = R.Left;
			RA.Y = R.Top + 3 * fsz;
			RA.Width = R.Width;
			RA.Height = fsz * 16;
			dc.Close();
			DC.AddVisual(v);
		}
		//=================================================================
		private void OnDC1MouseMove(object sender, MouseEventArgs e) {
			DrawingCanvas D = sender as DrawingCanvas;
			if (D == null) return;
			Point mp = e.GetPosition(D); int ix, iy, ic; long pos; double yy, cw;
			string smp, st = " "; const int llen = 107;

			if (!RA.Contains(mp)) { S1.Text = st; return; }
			cw = RA.Width / llen;
			if (cw * 23 + RA.Left > mp.X || mp.X > cw * 86 + RA.Left) { S1.Text = st; return; }
			smp = '(' + ((int)(mp.X)).ToString("d") + ',' + ((int)(mp.Y)).ToString("d") + ')';
			ix = (int)((mp.X - cw * 23 + RA.Left) / (cw * 4));
			//			ix = (int)(llen * mp.X / RA.Width);
			yy = mp.Y - RA.Top;
			iy = (int)(16 * yy / RA.Height);
			ic = iy * 16 + ix;
			pos = dmp.BlockNumber * dmp.BufLength + ic;
			if (RA.Contains(mp)) st = ic.ToString("d") + "/" + pos.ToString("##,#");
			S1.Text = st;
		}
		//=================================================================
		private void DrawText(DrawingContext dc, Point pt, string sText, int FontSize, TextAlignment TA) {
			FormattedText FT = new FormattedText(sText,
				 CultureInfo.GetCultureInfo("en-US"),
				 FlowDirection.LeftToRight,
				 new Typeface("Courier New"),
				 FontSize,
				 System.Windows.Media.Brushes.Black
				);
			FT.SetFontWeight(FontWeights.Bold);
			FT.TextAlignment = TA;
			dc.DrawText(FT, new System.Windows.Point(pt.X, pt.Y));

		}
		//================================================================
		private void DrawCross(DrawingContext dc, Rect R, Brush BR)    //draw cross lines
		{
			Pen pen = new Pen(BR, 1.0);
			dc.DrawLine(pen, new Point(0, 0), new Point(R.Right, R.Bottom));
			dc.DrawLine(pen, new Point(0, R.Bottom), new Point(R.Right, 0));
		}
		//=================================================================
		private void DrawMark(DrawingContext dc, Point P, Pen pen)    //draws a mark
		{
			const double delta = 7.0;
			Point p1 = new Point(P.X - delta, P.Y), p2 = new Point(P.X + delta, P.Y);
			dc.DrawLine(pen, p1, p2);
			dc.DrawLine(pen, new Point(P.X, P.Y - delta), new Point(P.X, P.Y + delta));
		}
	}
	///////////////////////////////////////////////////////////////////////
	public partial class DrawingCanvas : Canvas //from fsz10 book
	{
		private static int count = 0;
		private List<Visual> visuals = new List<Visual>();
		protected override int VisualChildrenCount { get { return visuals.Count; } }
		protected override Visual GetVisualChild(int index) { return (index < visuals.Count) ? visuals[index] : null; }
		public int Count { get { return count; } }
		//==================================================================
		public void AddVisual(Visual v) //adds visual to list
		{
			visuals.Add(v);
			base.AddVisualChild(v);
			base.AddLogicalChild(v);
			count += 1;
		}
		//==================================================================
		public void DeleteVisual(Visual v) //removes visual from list
		{
			visuals.Remove(v);
			base.RemoveLogicalChild(v);
			base.RemoveVisualChild(v);
			count -= 1;
		}
		//==================================================================
		public DrawingVisual GetVisual(Point pt)    //retrieve the visial for near this point
		{
			HitTestResult tr = VisualTreeHelper.HitTest(this, pt);
			return tr.VisualHit as DrawingVisual;
		}
		//==================================================================
		public void ClearAllVisuals()   //clears out the visuals for this element
		{
			while (VisualChildrenCount > 0) {
				Visual v = visuals[0];
				DeleteVisual(v);
			}
		}   //end class
	}
}
