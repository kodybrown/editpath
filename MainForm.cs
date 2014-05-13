//
// Copyright (C) 2007-2014 Kody Brown (kody@bricksoft.com).
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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SystemPathEditor
{
	public partial class MainForm : Form
	{
		private object _expandingPath = new object();
		private object _collapsingPath = new object();
		private bool _isExpandingPath = false;
		private bool _isCollapsingPath = false;

		private ListViewItem selectedItem;
		private int selectedSubItemIndex = 0;
		private TextBox editBox;

		private RegistryKey _curPathVar;
		private bool _hasChanged;

		#region MainForm

		public MainForm()
		{
			InitializeComponent();
		}

		private void MainForm_Load( object sender, EventArgs e )
		{
			SetupListView(pathListView);
			SetupEditBox();
		}

		private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
		{
			if (!CheckForChanges()) {
				e.Cancel = true;
				return;
			}
		}

		private void MainForm_KeyUp( object sender, KeyEventArgs e )
		{
			if (e.KeyCode == Keys.F2) {
				pathListView_KeyUp(sender, e);
				e.Handled = true;
			}
		}

		#endregion

		#region Path ListView

		private void SetupListView( ListView listView )
		{
			listView.BeginUpdate();

			listView.Items.Clear();

			listView.AllowColumnReorder = false;
			listView.AllowDrop = false;
			listView.CheckBoxes = false;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
			listView.HideSelection = false;
			listView.HotTracking = false;
			listView.HoverSelection = false;
			listView.LabelEdit = false;
			listView.LabelWrap = false;
			listView.MultiSelect = true;
			listView.Scrollable = true;
			listView.ShowGroups = false;
			listView.ShowItemToolTips = false;
			listView.Sorting = SortOrder.None;
			listView.View = View.Details;

			listView.Columns.Clear();
			listView.Columns.Add("Path", "Path", 415);
			listView.Columns.Add("ChangePath", "", 30);

			listView.EndUpdate();
		}

		private void pathListView_SelectedIndexChanged( object sender, EventArgs e )
		{
			removeButton.Enabled = pathListView.Items.Count > 0;
			moveUpButton.Enabled = false;
			moveDownButton.Enabled = false;

			if (pathListView.SelectedItems.Count == 1 && pathListView.Items.Count > 1) {
				if (pathListView.SelectedIndices[0] > 0) {
					moveUpButton.Enabled = true;
				}
				if (pathListView.SelectedIndices[0] < pathListView.Items.Count - 1) {
					moveDownButton.Enabled = true;
				}
			}
		}

		private void pathListView_KeyUp( object sender, KeyEventArgs e )
		{
			if (pathListView.SelectedItems.Count == 1 && (e.KeyCode == Keys.Return || e.KeyCode == Keys.F2)) {
				ListViewItem item = pathListView.SelectedItems[0];
				item.EnsureVisible();
				pathListView.FocusedItem = item;

				MouseEventArgs me = new MouseEventArgs(MouseButtons.Left, 2, item.Position.X + 1, item.Position.Y + 1, 0);
				pathListView_MouseDown(sender, me);

				pathListView_DoubleClick(sender, e);
			}
		}

		private void pathListView_DoubleClick( object sender, EventArgs e )
		{
			if (pathListView.SelectedItems.Count != 1) {
				return;
			}

			if (selectedItem == null) {
				return;
			}

			string subItemText = selectedItem.SubItems[selectedSubItemIndex].Text;

			if (selectedSubItemIndex == pathListView.Columns["Path"].Index) {
				//Rectangle r = new Rectangle(leftPos, pathListView.Top + pathListView.Margin.Top + selectedItem.Bounds.Y, rightPos, selectedItem.Bounds.Bottom);
				editBox.BringToFront();
				editBox.Show();
				editBox.Text = subItemText;
				editBox.SelectAll();
				editBox.Focus();
			}
		}

		private void pathListView_MouseDown( object sender, MouseEventArgs e )
		{
			if (pathListView.SelectedItems.Count != 1) {
				selectedItem = null;
				return;
			}

			selectedItem = pathListView.GetItemAt(e.X, e.Y);
			if (null == selectedItem) {
				return;
			}

			int leftPos = 0;
			int rightPos = pathListView.Columns[0].Width;

			for (int i = 0; i < pathListView.Columns.Count; i++) {
				if (e.X > leftPos && e.X < rightPos) {
					selectedSubItemIndex = i;
					break;
				}

				leftPos = rightPos;
				if (i < pathListView.Columns.Count - 1) {
					rightPos += pathListView.Columns[i + 1].Width;
				}
			}

			editBox.Size = new System.Drawing.Size(rightPos - leftPos, selectedItem.Bounds.Bottom - selectedItem.Bounds.Top);
			editBox.Location = new System.Drawing.Point(
							pathListView.Left + pathListView.Margin.Left + leftPos + 1,
							splitContainer1.SplitterDistance + splitContainer1.Top + 2 + pathListView.Top + pathListView.Margin.Top + selectedItem.Bounds.Y);

			if (selectedSubItemIndex == pathListView.Columns["ChangePath"].Index) {
				string subItemText = selectedItem.SubItems[selectedSubItemIndex].Text;
				string path = selectedItem.SubItems[pathListView.Columns["Path"].Index].Text;

				Bricksoft.PowerCode.FolderBrowserDialogEx f = new Bricksoft.PowerCode.FolderBrowserDialogEx();
				f.Description = "Select the path";
				f.SelectedPath = path;
				f.ShowNewFolderButton = true;

				if (f.ShowDialog() == DialogResult.OK) {
					selectedItem.SubItems[pathListView.Columns["Path"].Index].Text = f.SelectedPath;
					SetPath(selectedItem);
					CollapsePath();
				}
			}
		}

		#endregion

		#region Edit textbox

		private void SetupEditBox()
		{
			editBox = new System.Windows.Forms.TextBox();
			editBox.BackColor = Color.LightYellow;
			editBox.BorderStyle = BorderStyle.Fixed3D;
			editBox.Location = new System.Drawing.Point(0, 0);
			editBox.Size = new System.Drawing.Size(0, 0);
			editBox.Text = "";
			editBox.LostFocus += new EventHandler(editBox_LostFocus);
			editBox.KeyPress += new KeyPressEventHandler(editBox_KeyPress);
			Controls.AddRange(new System.Windows.Forms.Control[] { this.editBox });
			editBox.Hide();
		}

		private void editBox_KeyPress( object sender, KeyPressEventArgs e )
		{
			if (e.KeyChar == 13) {
				if (selectedItem == null) {
					return;
				}
				if (selectedItem.SubItems[selectedSubItemIndex].Text != editBox.Text) {
					_hasChanged = true;
					selectedItem.SubItems[selectedSubItemIndex].Text = editBox.Text;
					SetPath(selectedItem);
					CollapsePath();
					editBox.Hide();
				}
			}

			if (e.KeyChar == 27) {
				editBox.Hide();
			}
		}

		private void editBox_LostFocus( object sender, EventArgs e )
		{
			if (selectedItem != null) {
				if (selectedItem.SubItems[selectedSubItemIndex].Text != editBox.Text) {
					_hasChanged = true;
					selectedItem.SubItems[selectedSubItemIndex].Text = editBox.Text;
					SetPath(selectedItem);
					CollapsePath();
				}
			}
			editBox.Hide();
		}

		#endregion

		#region Path textbox

		private void pathTextbox_TextChanged( object sender, EventArgs e )
		{
			PathChanged(!_isCollapsingPath && !_isExpandingPath);
		}

		private void PathChanged( bool expandPath = false )
		{
			_hasChanged = true;

			undoToolStripMenuItem.Enabled = pathTextbox.CanUndo;
			redoToolStripMenuItem.Enabled = pathTextbox.CanRedo;
			cutToolStripMenuItem.Enabled = pathTextbox.SelectedText.Length > 0;
			copyToolStripMenuItem.Enabled = pathTextbox.SelectedText.Length > 0;
			pasteToolStripMenuItem.Enabled = pathTextbox.Focused && Clipboard.ContainsText();
			selectAllToolStripMenuItem.Enabled = pathTextbox.Text.Length > 0;

			if (expandPath) {
				ExpandPath();
			}
		}

		#endregion

		#region Button event handlers

		private void newButton_Click( object sender, EventArgs e )
		{
			_hasChanged = true;

			AddPath("");
			pathListView.Focus();

			pathListView.BeginUpdate();
			foreach (ListViewItem item in pathListView.Items) {
				item.Selected = false;
			}
			pathListView.EndUpdate();

			ListViewItem newItem = pathListView.Items[pathListView.Items.Count - 1];
			newItem.Selected = true;
			newItem.EnsureVisible();
			pathListView.FocusedItem = newItem;

			MouseEventArgs me = new MouseEventArgs(MouseButtons.Left, 2, newItem.Position.X + 1, newItem.Position.Y + 1, 0);
			pathListView_MouseDown(sender, me);

			pathListView_DoubleClick(sender, e);
		}

		private void removeButton_Click( object sender, EventArgs e )
		{
			if (pathListView.SelectedItems.Count > 0) {
				_hasChanged = true;

				ListView.SelectedIndexCollection indexes = pathListView.SelectedIndices;
				int selectedIndex = pathListView.SelectedIndices[0];

				for (int i = indexes.Count - 1; i >= 0; i--) {
					pathListView.Items.RemoveAt(indexes[i]);
				}

				if (selectedIndex < pathListView.Items.Count) {
					pathListView.Items[selectedIndex].Selected = true;
				} else if (pathListView.Items.Count > 0) {
					pathListView.Items[pathListView.Items.Count - 1].Selected = true;
				}

				CollapsePath();
			}
		}

		private void moveUpButton_Click( object sender, EventArgs e )
		{
			if (pathListView.SelectedItems.Count == 1 && pathListView.Items.Count > 1) {
				if (pathListView.SelectedIndices[0] > 0) {
					_hasChanged = true;

					ListViewItem item = pathListView.SelectedItems[0];
					int index = item.Index;

					pathListView.Items.RemoveAt(index);
					pathListView.Items.Insert(index - 1, item);

					item.Selected = true;
					item.EnsureVisible();

					CollapsePath();
				}
			}
		}

		private void moveDownButton_Click( object sender, EventArgs e )
		{
			if (pathListView.SelectedItems.Count == 1 && pathListView.Items.Count > 1) {
				if (pathListView.SelectedIndices[0] < pathListView.Items.Count - 1) {
					_hasChanged = true;

					ListViewItem item = pathListView.SelectedItems[0];
					int index = item.Index;

					pathListView.Items.RemoveAt(index);
					pathListView.Items.Insert(index + 1, item);

					item.Selected = true;
					item.EnsureVisible();

					CollapsePath();
				}
			}
		}

		#endregion

		#region Menu events

		private void exitToolStripMenuItem_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void undoToolStripMenuItem_Click( object sender, EventArgs e )
		{
			pathTextbox.Focus();
			if (pathTextbox.CanUndo) {
				pathTextbox.Undo();
			}
		}

		private void redoToolStripMenuItem_Click( object sender, EventArgs e )
		{
			pathTextbox.Focus();
			if (pathTextbox.CanRedo) {
				pathTextbox.Redo();
			}
		}

		private void cutToolStripMenuItem_Click( object sender, EventArgs e )
		{
			pathTextbox.Focus();
			if (pathTextbox.SelectedText.Length > 0) {
				Clipboard.SetData(DataFormats.Text, pathTextbox.SelectedText);
				pathTextbox.SelectedText = string.Empty;
			}
		}

		private void copyToolStripMenuItem_Click( object sender, EventArgs e )
		{
			pathTextbox.Focus();
			if (pathTextbox.SelectedText.Length > 0) {
				Clipboard.SetData(DataFormats.Text, pathTextbox.SelectedText);
			}
		}

		private void pasteToolStripMenuItem_Click( object sender, EventArgs e )
		{
			pathTextbox.Focus();
			if (Clipboard.ContainsText()) {
				pathTextbox.SelectedText = (string)Clipboard.GetData(DataFormats.Text);
			}
		}

		private void selectAllToolStripMenuItem_Click( object sender, EventArgs e )
		{
			pathTextbox.Focus();
			pathTextbox.SelectAll();
		}

		private void clearToolStripMenuItem_Click( object sender, EventArgs e )
		{
			if (!CheckForChanges()) {
				return;
			}

			_curPathVar = null;
			_hasChanged = false;

			pathTextbox.ClearUndo();
			pathTextbox.Clear();

			loadUserPathToolStripMenuItem.Enabled = true;
			loadSystemPathToolStripMenuItem.Enabled = true;
			clearToolStripMenuItem.Enabled = false;
			saveToolStripMenuItem.Enabled = false;
		}

		private void saveToolStripMenuItem_Click( object sender, EventArgs e )
		{
			SavePath();
		}

		#endregion

		#region Misc methods

		private void ExpandPath()
		{
			lock (_expandingPath) {
				_isExpandingPath = true;

				List<string> path;

				path = Path.Split(pathTextbox.Text);

				if (null != path) {
					ListViewItem item = null;
					int selectedIndex = -1;

					// Save the current selected path..
					if (pathListView.SelectedItems.Count > 0) {
						item = pathListView.SelectedItems[0];
						selectedIndex = pathListView.SelectedIndices[0];
					}

					pathListView.BeginUpdate();
					pathListView.Items.Clear();

					foreach (string p in path) {
						AddPath(p, false);
					}

					if (item != null) {
						foreach (ListViewItem itm in pathListView.Items) {
							if (itm.SubItems[pathListView.Columns["Path"].Index].Text.Equals(item.SubItems[pathListView.Columns["Path"].Index].Text, StringComparison.CurrentCultureIgnoreCase)) {
								itm.Selected = true;
								itm.EnsureVisible();
							}
						}
					}

					CollapsePath();

					pathListView.EndUpdate();
				}

				_isExpandingPath = false;
			}
		}

		private void CollapsePath()
		{
			lock (_collapsingPath) {
				_isCollapsingPath = true;

				StringBuilder path = new StringBuilder();

				if (0 < pathListView.Items.Count) {
					int index = pathListView.Columns["Path"].Index;
					for (int i = 0; i < pathListView.Items.Count; i++) {
						path.Append(pathListView.Items[i].SubItems[index].Text).Append(";");
					}
				}

				pathTextbox.Text = path.ToString();

				_isCollapsingPath = false;
			}
		}

		private void AddPath( string path, bool collapsePath = true )
		{
			ListViewItem item;

			item = new ListViewItem(path);
			SetPath(item);

			pathListView.Items.Add(item);

			if (collapsePath) {
				CollapsePath();
			}
		}

		private void SetPath( ListViewItem item )
		{
			string path;
			bool containsEnvVar = false;

			path = item.SubItems[pathListView.Columns["Path"].Index].Text;

			if (Directory.Exists(path)) {
				item.ImageIndex = 0;
				item.ForeColor = Color.FromKnownColor(KnownColor.ControlText);
			} else if (path.StartsWith("%")) {
				item.ImageIndex = 1;
				containsEnvVar = true;

				if (Directory.Exists(Environment.ExpandEnvironmentVariables(path))) {
					item.ForeColor = Color.Blue;
				} else {
					item.ForeColor = Color.Blue;
				}
			} else {
				item.ImageIndex = 2;
				item.ForeColor = Color.Red;
			}

			if (item.SubItems.Count <= 1) {
				item.SubItems.Add(string.Empty);
			}

			item.SubItems[pathListView.Columns["ChangePath"].Index].Text = "..";
			if (containsEnvVar) {
				item.SubItems[pathListView.Columns["ChangePath"].Index].ForeColor = Color.Orange;
			}
		}

		private bool SavePath()
		{
			if (_curPathVar == null) {
				return true;
			}

			// Set PATH variable from user's typed path
			_curPathVar.SetValue("PATH", pathTextbox.Text, RegistryValueKind.String);
			_hasChanged = false;

			return true;
		}

		/// <summary>
		/// If the path was loaded from the User or System environment path,
		/// and it was changed, the user is prompted to save it.
		/// </summary>
		/// <returns>
		/// True if the user declined to save or saved. False is the 
		/// user pressed cancel.
		/// </returns>
		private bool CheckForChanges()
		{
			if (_curPathVar != null && _hasChanged) {
				DialogResult result = MessageBox.Show("The path has changed, would you like to save it?", "Path Editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
				if (result == DialogResult.Yes) {
					return SavePath();
				} else if (result == System.Windows.Forms.DialogResult.Cancel) {
					return false;
				}
			}

			return true;
		}

		private void loadUserPathToolStripMenuItem_Click( object sender, EventArgs e )
		{
			LoadPath(Registry.CurrentUser.OpenSubKey(@"Environment\", true));
		}

		private void loadSystemPathToolStripMenuItem_Click( object sender, EventArgs e )
		{
			try {
				LoadPath(Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment\", true));
			} catch (System.Security.SecurityException) {
				MessageBox.Show("This requires the application to be run with elevated privileges. (ie: Right click the file and select 'Run as Administrator')", "Elevated Privileges Required", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void LoadPath( RegistryKey key )
		{
			if (key != null) {
				_curPathVar = key;

				// Read PATH variable from specified key
				pathTextbox.Text = (string)key.GetValue("PATH", "", RegistryValueOptions.DoNotExpandEnvironmentNames);

				// When the user loads the user or system path, disable the 
				// load buttons and require them to 'Reset' before they can 
				// load one again.
				loadUserPathToolStripMenuItem.Enabled = false;
				loadSystemPathToolStripMenuItem.Enabled = false;
				clearToolStripMenuItem.Enabled = true;
				saveToolStripMenuItem.Enabled = true;

				_hasChanged = false;
			}
		}

		#endregion

	}
}
