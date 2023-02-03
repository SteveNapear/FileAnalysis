using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents.Serialization;

namespace Parser.Model {
	public static class CParser {
		//................................................................
		public static bool TryParseExp(string s, out double v) {
			string ops = "+-*/().";
			StringBuilder sb = new StringBuilder(s.Length);
			int ix; char c; v = 0;
			for (ix = 0; ix < s.Length; ix++) {
				c = s[ix];
				if (ops.Contains(c) || char.IsDigit(c)) sb.Append(c);
				else if (c == ' ') continue;
				else return false;
			}
			string ss = sb.ToString();

			int ixx = isexpression(ss, out v);
			return (ixx >= 0);
		}
		//............................................................
		private static int isexpression(string ss, out double vexp) {
			vexp = 0.0; int ix = -1;
			ix = isterm(ss, out double v);
			//			int ix = isterm(ss, out double v);
			if (ix == ss.Length) { vexp = v; return ix; }
			return -1;
		}
		//...............................................................
		private static int isterm(string ss, out double vt) {
			vt = 0.0; char op; string s;
			int i, ix = isfactor(ss, out double vf);
			if (ix < 0) return -1;
			vt = vf;
			while (ix < ss.Length) {
				op = ss[ix++];
				s = ss.Substring(ix);
				if ((s is null) || s.Length == 0) return -1;
				i = isfactor(s, out vf);
				if (i < 0) return -1;
				switch (op) {
					case '+': vt += vf; break;
					case '-': vt -= vf; break;
					default: return ix - 1;
				}
				ix += i;
			}
			return ix;
		}
		//................................................................
		private static int isfactor(string ss, out double vf) {
			vf = 0.0; char op; string s;
			int i, ix = isElement(ss, out double de);
			vf = de;
			while (ix < ss.Length) {
				op = ss[ix++];
				s = ss.Substring(ix);
				if (s is null || s.Length == 0) return -1;
				i = isElement(s, out double dev);
				if (i < 0) return -1;
				switch (op) {
					case '*': vf *= dev; break;
					case '/': vf /= dev; break;
					default: return ix - 1;
				}
				ix += i;
			}
			return ix;
		}
		//.................................................................
		private static int isElement(string ss, out double de) {
			de = 0.0;
			if (ss is null || ss.Length == 0) return -1;
			bool bOK = false; int ix;
			if (ss[0] == '(') {
				string s = ss.Substring(1);
				ix = isexpression(s, out double dex);
				return (ix < 0 || s[ix] != ')') ? -1 : ix + 1;

			}
			//extract element
			ix = 0; while (ix < ss.Length && (char.IsDigit(ss[ix]) || (ss[ix] == '.'))) ix += 1;
			string sn = ss.Substring(0, ix);
			if (double.TryParse(sn, out de)) return ix;
			return -1;
		}
	}
}
