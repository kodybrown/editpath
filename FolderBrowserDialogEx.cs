//
// Copyright (C) 2007-2013 Kody Brown (kody@bricksoft.com).
// 
// MIT License:
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Windows.Forms;

namespace Bricksoft.PowerCode
{
	/// <summary>
	/// Represents a FolderBrowserDialog where any environment variables
	/// passed in as part of the SelectedPath are preserved when the 
	/// dialog is closed (assuming the new folder is still beneath it).
	/// </summary>
	public class FolderBrowserDialogEx
	{
		/// <summary>
		/// The original (un-modified) path set by the caller.
		/// </summary>
		protected string originalPath = string.Empty;

		/// <summary>
		/// Gets or sets whether the BrowserDialogEx will expand to as 
		/// much of the path as possible.
		/// For instance, if the path specified is 'C:\Windows\System32\driverss' 
		/// and driverss does not exist, the dialog will still expand to 'C:\Windows\System32'.
		/// Defaults to true.
		/// </summary>
		public bool ExpandPath { get; set; }

		/// <summary>
		/// Gets or sets the selected path.
		/// </summary>
		public string SelectedPath
		{
			get
			{
				string path;
				int pos;
				string envVar;
				string envPath;

				if (null == originalPath || 0 == originalPath.Length) {
					return dlg.SelectedPath;
				}

				if (!originalPath.StartsWith("%")) {
					return dlg.SelectedPath;
				}

				path = dlg.SelectedPath;

				if (originalPath.StartsWith("%")) {
					pos = 0;
					envVar = string.Empty;
					envPath = string.Empty;

					pos = originalPath.LastIndexOf('%');
					envVar = originalPath.Substring(0, pos + 1); // skip the %'s
					envPath = Environment.ExpandEnvironmentVariables(envVar);

					if (path.StartsWith(envPath)) {
						return envVar + path.Substring(envPath.Length);
					}
				}

				return path;
			}
			set
			{
				originalPath = value;
				if (value.IndexOf("%") > -1) {
					dlg.SelectedPath = Environment.ExpandEnvironmentVariables(value);
				} else {
					dlg.SelectedPath = value;
				}

				if (ExpandPath) {
					while (dlg.SelectedPath.Length > 3 && !Directory.Exists(dlg.SelectedPath)) {
						dlg.SelectedPath = System.IO.Path.GetDirectoryName(dlg.SelectedPath);
					}
				}
			}
		}


		/// <summary>
		/// Creates a new instance of the class.
		/// </summary>
		public FolderBrowserDialogEx()
		{
			dlg = new FolderBrowserDialog();
			ExpandPath = true;
		}


		// ----- FolderBrowserDialog wrappers ----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Internal dialog control, since the FolderBrowserDialog does not
		/// allow inheriting.
		/// </summary>
		protected FolderBrowserDialog dlg;

		/// <summary>
		/// Occurrs when the component is disposed.
		/// </summary>
		public EventHandler Disposed { set { dlg.Disposed += value; } }

		/// <summary>
		/// Gets or sets whether the new folder button appears on the dialog.
		/// </summary>
		public bool ShowNewFolderButton { get { return dlg.ShowNewFolderButton; } set { dlg.ShowNewFolderButton = value; } }

		/// <summary>
		/// Gets or sets the descriptive text displayed over the treeview
		/// control in the dialog.
		/// </summary>
		public string Description { get { return dlg.Description; } set { dlg.Description = value; } }

		/// <summary>
		/// Gets or sets the root folder where browsing starts from.
		/// </summary>
		public System.Environment.SpecialFolder RootFolder { get { return dlg.RootFolder; } set { dlg.RootFolder = value; } }


		/// <summary>
		/// Gets or sets an object that contains data about the control.
		/// </summary>
		public object Tag { get; set; }

		/// <summary>
		/// Gets or sets the System.ComponentModel.ISite of the control.
		/// </summary>
		public System.ComponentModel.ISite Site { get { return dlg.Site; } set { dlg.Site = value; } }

		/// <summary>
		/// Gets the System.ComponentModel.IContainer of the control.
		/// </summary>
		public System.ComponentModel.IContainer Container { get { return dlg.Container; } }


		/// <summary>
		/// Shows the dialog box with a default owner.
		/// </summary>
		/// <returns></returns>
		public DialogResult ShowDialog() { return dlg.ShowDialog(); }

		/// <summary>
		/// Shows the dialog box with the specified owner.
		/// </summary>
		/// <param name="form"></param>
		/// <returns></returns>
		public DialogResult ShowDialog( IWin32Window form ) { return dlg.ShowDialog(form); }

		/// <summary>
		/// Releases all resources used by the component.
		/// </summary>
		public void Dispose() { dlg.Dispose(); }

		/// <summary>
		/// Resets properties to their default values.
		/// </summary>
		public void Reset() { dlg.Reset(); ExpandPath = true; }

		/// <summary>
		/// Creates an object that includes all the relevant information required 
		/// to generate a proxy used to communicate with a remote object.
		/// </summary>
		/// <param name="requestedType"></param>
		/// <returns></returns>
		public System.Runtime.Remoting.ObjRef CreateObjRef( Type requestedType ) { return dlg.CreateObjRef(requestedType); }
	}
}
