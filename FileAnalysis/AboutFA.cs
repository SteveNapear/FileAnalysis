using System.Reflection;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;


namespace FileAnalysis {
	partial class AboutFA : Form {
		public string sPath { get; set; }
		public Size szGrid = new Size();
		//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		public AboutFA() {
			InitializeComponent();
			Text = AssemblyTitle;
			labelProductName.Text = "Product: "+AssemblyProduct;
			labelVersion.Text = "Version: "+AssemblyVersion;
			labelCopyright.Text = AssemblyCopyright;
			labelCompanyName.Text = AssemblyCompany;
		}

		#region Assembly Attribute Accessors

		public string AssemblyTitle {
			get {
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (attributes.Length > 0) {
					AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
					if (titleAttribute.Title != "") {
						return titleAttribute.Title;
					}
				}
				return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
			}
		}

		public string AssemblyVersion {
			get {
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}
		}

		public string AssemblyDescription {
			get {
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				if (attributes.Length == 0) {
					return "";
				}
				return ((AssemblyDescriptionAttribute)attributes[0]).Description;
			}
		}

		public string AssemblyProduct {
			get {
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (attributes.Length == 0) {
					return "";
				}
				return ((AssemblyProductAttribute)attributes[0]).Product;
			}
		}

		public string AssemblyCopyright {
			get {
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (attributes.Length == 0) {
					return "";
				}
				return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
			}
		}

		public string AssemblyCompany {
			get {
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
				if (attributes.Length == 0) {
					return "";
				}
				return ((AssemblyCompanyAttribute)attributes[0]).Company;
			}
		}
		#endregion
		//================================================================
		private void OnAboutLoad(object sender, System.EventArgs e) {
			if (szGrid.Width > 0) {
				this.Width = (int)(szGrid.Width * 0.75);
				this.Height = (int)(szGrid.Height * 0.66);
			}
			string[] lines = new string[2];
			lines[0] = "Dir: "+System.IO.Path.GetDirectoryName(sPath);
			lines[1] = "File: "+System.IO.Path.GetFileName(sPath);
//			textBoxDescription.Text = "Path: " + sPath;
			textBoxDescription.Lines  = lines;
		}
	}
}
