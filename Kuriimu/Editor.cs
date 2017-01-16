﻿using Be.Windows.Forms;
using Kuriimu.Properties;
using KuriimuContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Kuriimu
{
	public partial class frmEditor : Form
	{
		private IFileAdapter _fileAdapter = null;
		private IGameHandler _gameHandler = null;
		private bool _fileOpen = false;
		private bool _hasChanges = false;

		private List<IFileAdapter> _fileAdapters = null;
		private List<IGameHandler> _gameHandlers = null;
		private List<IExtension> _extensions = null;

		private IEnumerable<IEntry> _entries = null;

		public frmEditor(string[] args)
		{
			InitializeComponent();
			Console.Write(Common.GetAppMessage());

			// Load Plugins
			_fileAdapters = PluginLoader<IFileAdapter>.LoadPlugins(Settings.Default.PluginDirectory, "file*.dll").ToList();
			_gameHandlers = Tools.LoadGameHandlers(tsbGameSelect, Resources.game_none, tsbGameSelect_SelectedIndexChanged);
			_extensions = PluginLoader<IExtension>.LoadPlugins(Settings.Default.PluginDirectory, "ext*.dll").ToList();

			// Load passed in file
			if (args.Length > 0 && File.Exists(args[0]))
				OpenFile(args[0]);
		}

		private void frmEditor_Load(object sender, EventArgs e)
		{
			Icon = Resources.kuriimu;
			Tools.DoubleBuffer(treEntries, true);
			UpdateForm();
		}

		// Menu/Toolbar
		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ConfirmOpenFile();
		}

		private void tsbOpen_Click(object sender, EventArgs e)
		{
			ConfirmOpenFile();
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFile();
		}

		private void tsbSave_Click(object sender, EventArgs e)
		{
			SaveFile();
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFile(true);
		}

		private void tsbSaveAs_Click(object sender, EventArgs e)
		{
			SaveFile(true);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void findToolStripMenuItem_Click(object sender, EventArgs e)
		{
			frmSearch search = new frmSearch();
			search.Entries = _entries;
			search.ShowDialog();

			if (search.Selected != null)
			{
				treEntries.SelectNodeByIEntry(search.Selected);

				if (txtEdit.Text.Contains(Settings.Default.FindWhat))
				{
					txtEdit.SelectionStart = txtEdit.Text.IndexOf(Settings.Default.FindWhat);
					txtEdit.SelectionLength = Settings.Default.FindWhat.Length;
					txtEdit.Focus();

					SelectInHex();
				}
			}
		}
		private void tsbFind_Click(object sender, EventArgs e)
		{
			findToolStripMenuItem_Click(sender, e);
		}

		private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_fileAdapter.ShowProperties(Resources.kuriimu))
			{
				_hasChanges = true;
				UpdateForm();
			}
		}
		private void tsbFileProperties_Click(object sender, EventArgs e)
		{
			propertiesToolStripMenuItem_Click(sender, e);
		}

		private void addEntryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			IEntry entry = _fileAdapter.NewEntry();

			frmName name = new frmName(entry, _fileAdapter.EntriesHaveUniqueNames, _fileAdapter.NameList, _fileAdapter.NameFilter, _fileAdapter.NameMaxLength, true);

			if (name.ShowDialog() == DialogResult.OK && name.NameChanged)
			{
				entry.Name = name.NewName;
				if (_fileAdapter.AddEntry(entry))
				{
					_hasChanges = true;
					LoadEntries();
					treEntries.SelectNodeByIEntry(entry);
					UpdateForm();
				}
			}
		}
		private void tsbEntryAdd_Click(object sender, EventArgs e)
		{
			addEntryToolStripMenuItem_Click(sender, e);
		}

		private void renameEntryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			IEntry entry = (IEntry)treEntries.SelectedNode.Tag;

			frmName name = new frmName(entry, _fileAdapter.EntriesHaveUniqueNames, _fileAdapter.NameList, _fileAdapter.NameFilter, _fileAdapter.NameMaxLength);

			if (name.ShowDialog() == DialogResult.OK && name.NameChanged)
			{
				if (_fileAdapter.RenameEntry(entry, name.NewName))
				{
					_hasChanges = true;
					treEntries.FindNodeByIEntry(entry).Text = name.NewName;
					UpdateForm();
				}
			}
		}
		private void tsbEntryRename_Click(object sender, EventArgs e)
		{
			renameEntryToolStripMenuItem_Click(sender, e);
		}

		private void deleteEntryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			IEntry entry = (IEntry)treEntries.SelectedNode.Tag;

			if (MessageBox.Show("Are you sure you want to delete " + entry.Name + "?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				if (_fileAdapter.DeleteEntry(entry))
				{
					_hasChanges = true;
					TreeNode nextNode = treEntries.SelectedNode.NextNode;
					UpdateEntries();
					treEntries.Nodes.Remove(treEntries.FindNodeByIEntry(entry));
					treEntries.SelectedNode = nextNode;
				}
			}
		}
		private void tsbEntryDelete_Click(object sender, EventArgs e)
		{
			deleteEntryToolStripMenuItem_Click(sender, e);
		}

		private void entryPropertiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			IEntry entry = (IEntry)treEntries.SelectedNode.Tag;
			if (_fileAdapter.ShowEntryProperties(entry, Resources.kuriimu))
			{
				_hasChanges = true;
				UpdateForm();
			}
		}
		private void tsbEntryProperties_Click(object sender, EventArgs e)
		{
			entryPropertiesToolStripMenuItem_Click(sender, e);
		}

		private void sortEntriesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_fileAdapter.SortEntries = !_fileAdapter.SortEntries;
			LoadEntries();
			UpdateForm();
		}
		private void tsbSortEntries_Click(object sender, EventArgs e)
		{
			sortEntriesToolStripMenuItem_Click(sender, e);
		}

		private void gBATempToolStripMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://gbatemp.net/threads/release-kuriimu-a-general-purpose-game-translation-toolkit-for-authors-of-fan-translations.452375/");
		}

		private void gitHubToolStripMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("https://github.com/Icyson55/Kuriimu");
		}

		private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			frmAbout about = new frmAbout();
			about.ShowDialog();
		}

		// UI Toolbars
		private void tsbGameSelect_SelectedIndexChanged(object sender, EventArgs e)
		{
			ToolStripItem tsi = (ToolStripItem)sender;
			_gameHandler = (IGameHandler)tsi.Tag;
			tsbGameSelect.Text = tsi.Text;
			tsbGameSelect.Image = tsi.Image;

			UpdateTextView();
			UpdatePreview();
			UpdateForm();

			Settings.Default.SelectedGameHandler = tsi.Text;
			Settings.Default.Save();
		}

		private void tsbPreviewEnabled_Click(object sender, EventArgs e)
		{
			Settings.Default.PreviewEnabled = !Settings.Default.PreviewEnabled;
			Settings.Default.Save();
			UpdatePreview();
			UpdateForm();
		}

		// File Handling
		private void frmEditor_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = DragDropEffects.Copy;
		}

		private void frmEditor_DragDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			if (files.Length > 0 && File.Exists(files[0]))
				ConfirmOpenFile(files[0]);
		}

		private void ConfirmOpenFile(string filename = "")
		{
			DialogResult dr = DialogResult.No;

			if (_fileOpen && _hasChanges)
				dr = MessageBox.Show("You have unsaved changes in " + FileName() + ". Save changes before opening another file?", "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

			switch (dr)
			{
				case DialogResult.Yes:
					dr = SaveFile();
					if (dr == DialogResult.OK) OpenFile(filename);
					break;
				case DialogResult.No:
					OpenFile(filename);
					break;
			}
		}

		private void OpenFile(string filename = "")
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.InitialDirectory = Settings.Default.LastDirectory;

			// Supported Types
			ofd.Filter = Tools.LoadFileFilters(_fileAdapters);

			DialogResult dr = DialogResult.OK;

			if (filename == string.Empty)
			{
				dr = ofd.ShowDialog();
				filename = ofd.FileName;
			}

			if (dr == DialogResult.OK)
			{
				try
				{
					_fileAdapter = SelectFileAdapter(filename);
					if (_fileAdapter != null && _fileAdapter.Load(filename) == LoadResult.Success)
					{
						_fileOpen = true;
						_hasChanges = false;

						// Select Game Handler
						foreach (ToolStripItem tsi in tsbGameSelect.DropDownItems)
							if (tsi.Text == Settings.Default.SelectedGameHandler)
							{
								_gameHandler = (IGameHandler)tsi.Tag;
								tsbGameSelect.Text = tsi.Text;
								tsbGameSelect.Image = tsi.Image;
								break;
							}
						if (_gameHandler == null)
							_gameHandler = (IGameHandler)tsbGameSelect.DropDownItems[0].Tag;

						LoadEntries();
						UpdateTextView();
						UpdatePreview();
						UpdateHexView();
					}

					Settings.Default.LastDirectory = new FileInfo(filename).DirectoryName;
					Settings.Default.Save();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString(), ex.Message, MessageBoxButtons.OK);
					_fileOpen = false;
					_hasChanges = false;
				}

				UpdateForm();
			}
		}

		private DialogResult SaveFile(bool saveAs = false)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			DialogResult dr = DialogResult.OK;

			sfd.FileName = _fileAdapter.FileInfo.Name;
			sfd.Filter = _fileAdapter.Description + " (" + _fileAdapter.Extension + ")|" + _fileAdapter.Extension;

			if (_fileAdapter.FileInfo == null || saveAs)
			{
				sfd.InitialDirectory = Settings.Default.LastDirectory;
				dr = sfd.ShowDialog();
			}

			if ((_fileAdapter.FileInfo == null || saveAs) && dr == DialogResult.OK)
			{
				_fileAdapter.FileInfo = new FileInfo(sfd.FileName);
				Settings.Default.LastDirectory = new FileInfo(sfd.FileName).DirectoryName;
				Settings.Default.Save();
			}

			if (dr == DialogResult.OK)
			{
				_fileAdapter.Save(_fileAdapter.FileInfo.FullName);
				_hasChanges = false;
				UpdateForm();
			}

			return dr;
		}

		private IFileAdapter SelectFileAdapter(string filename)
		{
			IFileAdapter result = null;

			try
			{
				// first look for adapters whose extension matches that of our filename
				List<IFileAdapter> matchingAdapters = _fileAdapters.Where(adapter => adapter.Extension.Split(';').Any(s => filename.ToLower().EndsWith(s.Substring(1).ToLower()))).ToList();

				result = matchingAdapters.FirstOrDefault(adapter => adapter.Identify(filename));

				if (result == null)
				{
					// if none of them match, then try all other adapters
					result = _fileAdapters.Except(matchingAdapters).FirstOrDefault(adapter => adapter.Identify(filename));
				}

				if (result == null)
					MessageBox.Show("None of the installed plugins were able to open the file.", "Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString(), ex.Message, MessageBoxButtons.OK);
				_fileOpen = false;
				_hasChanges = false;
			}

			return result;
		}

		private void LoadEntries()
		{
			UpdateEntries();

			treEntries.BeginUpdate();

			IEntry selectedEntry = null;
			if (treEntries.SelectedNode != null)
				selectedEntry = (IEntry)treEntries.SelectedNode.Tag;

			treEntries.Nodes.Clear();
			if (_entries != null)
			{
				foreach (IEntry entry in _entries)
				{
					TreeNode node = new TreeNode(entry.ToString());
					node.Tag = entry;
					if (_fileAdapter.OnlySubEntriesHaveText)
					{
						node.ForeColor = System.Drawing.Color.Gray;
					}
					treEntries.Nodes.Add(node);

					if (_fileAdapter.EntriesHaveSubEntries)
						foreach (IEntry sub in entry.SubEntries)
						{
							TreeNode subNode = new TreeNode(sub.ToString());
							subNode.Tag = sub;
							node.Nodes.Add(subNode);
						}

					node.Expand();
				}
			}

			if ((selectedEntry == null || !_entries.Contains(selectedEntry)) && treEntries.Nodes.Count > 0)
				treEntries.SelectedNode = treEntries.Nodes[0];
			else
				treEntries.SelectNodeByIEntry(selectedEntry);

			treEntries.EndUpdate();

			treEntries.Focus();
		}

		private void UpdateEntries()
		{
			_entries = _fileAdapter.Entries;

			if (_fileAdapter.SortEntries)
				_entries = _entries.OrderBy(x => x);
		}

		// Utilities
		private void UpdateTextView()
		{
			IEntry entry = (IEntry)treEntries.SelectedNode?.Tag;

			if (entry == null)
			{
				txtEdit.Text = string.Empty;
				txtOriginal.Text = string.Empty;
			}
			else
			{
				txtEdit.Text = _gameHandler.GetKuriimuString(entry.EditedTextString).Replace("\0", "<null>").Replace("\n", "\r\n");
				txtOriginal.Text = _gameHandler.GetKuriimuString(entry.OriginalTextString).Replace("\0", "<null>").Replace("\n", "\r\n");
			}

			if (entry != null && !entry.IsResizable)
				txtEdit.MaxLength = entry.MaxLength == 0 ? int.MaxValue : entry.MaxLength;
		}

		private void UpdatePreview()
		{
			IEntry entry = (IEntry)treEntries.SelectedNode?.Tag;

			if (entry != null && _gameHandler.HandlerCanGeneratePreviews && Settings.Default.PreviewEnabled)
				pbxPreview.Image = _gameHandler.GeneratePreview(entry.EditedTextString);
			else
				pbxPreview.Image = null;
		}

		private void UpdateHexView()
		{
			DynamicFileByteProvider dfbp = null;

			try
			{
				IEntry entry = (IEntry)treEntries.SelectedNode?.Tag;

				if (entry != null)
				{
					MemoryStream strm = new MemoryStream(entry.EditedText);
					dfbp = new DynamicFileByteProvider(strm);
					dfbp.Changed += new EventHandler(hbxEdit_Changed);
				}
			}
			catch (Exception)
			{ }

			hbxHexView.ByteProvider = dfbp;
		}

		private void UpdateForm()
		{
			Text = Settings.Default.ApplicationName + " " + Settings.Default.ApplicationVersion + (FileName() != string.Empty ? " - " + FileName() : string.Empty) + (_hasChanges ? "*" : string.Empty);

			IEntry entry = (IEntry)treEntries.SelectedNode?.Tag;

			if (_fileOpen)
				tslEntries.Text = (_fileAdapter.Entries?.Count() + " Entries").Trim();
			else
				tslEntries.Text = "Entries";

			if (_fileAdapter != null)
			{
				bool itemSelected = _fileOpen && treEntries.SelectedNode != null;
				bool canAdd = _fileOpen && _fileAdapter.CanAddEntries;
				bool canRename = itemSelected && _fileAdapter.CanRenameEntries && (_fileAdapter.OnlySubEntriesHaveText && entry.IsSubEntry || !_fileAdapter.OnlySubEntriesHaveText);
				bool canDelete = itemSelected && _fileAdapter.CanDeleteEntries && !entry.IsSubEntry;

				splMain.Enabled = _fileOpen;
				splContent.Enabled = _fileOpen;
				splText.Enabled = _fileOpen;
				splPreview.Enabled = _fileOpen;

				// Menu
				saveToolStripMenuItem.Enabled = _fileOpen && _fileAdapter.CanSave;
				tsbSave.Enabled = _fileOpen && _fileAdapter.CanSave;
				saveAsToolStripMenuItem.Enabled = _fileOpen && _fileAdapter.CanSave;
				tsbSaveAs.Enabled = _fileOpen && _fileAdapter.CanSave;
				findToolStripMenuItem.Enabled = _fileOpen;
				tsbFind.Enabled = _fileOpen;
				propertiesToolStripMenuItem.Enabled = _fileOpen && _fileAdapter.FileHasExtendedProperties;
				tsbProperties.Enabled = _fileOpen && _fileAdapter.FileHasExtendedProperties;

				// Toolbar
				addEntryToolStripMenuItem.Enabled = canAdd;
				tsbEntryAdd.Enabled = canAdd;
				renameEntryToolStripMenuItem.Enabled = canRename;
				tsbEntryRename.Enabled = canRename;
				deleteEntryToolStripMenuItem.Enabled = canDelete;
				tsbEntryDelete.Enabled = canDelete;
				entryPropertiesToolStripMenuItem.Enabled = itemSelected && _fileAdapter.EntriesHaveExtendedProperties;
				tsbEntryProperties.Enabled = itemSelected && _fileAdapter.EntriesHaveExtendedProperties;
				sortEntriesToolStripMenuItem.Enabled = _fileOpen && _fileAdapter.CanSortEntries;
				sortEntriesToolStripMenuItem.Image = _fileAdapter.SortEntries ? Resources.menu_sorted : Resources.menu_unsorted;
				tsbSortEntries.Enabled = _fileOpen && _fileAdapter.CanSortEntries;
				tsbSortEntries.Image = _fileAdapter.SortEntries ? Resources.menu_sorted : Resources.menu_unsorted;
				tsbPreviewEnabled.Enabled = _gameHandler != null ? _gameHandler.HandlerCanGeneratePreviews : false;
				tsbPreviewEnabled.Image = Settings.Default.PreviewEnabled ? Resources.menu_preview_visible : Resources.menu_preview_invisible;
				tsbPreviewEnabled.Text = Settings.Default.PreviewEnabled ? "Disable Preview" : "Enable Preview";

				treEntries.Enabled = _fileOpen;
				if (itemSelected && _fileAdapter.OnlySubEntriesHaveText)
				{
					txtEdit.Enabled = entry.IsSubEntry;
					if (!entry.IsSubEntry)
						txtEdit.Text = "Please select a sub entry to edit the text.";
					txtOriginal.Enabled = entry.IsSubEntry && txtOriginal.Text.Trim().Length > 0;
					hbxHexView.Enabled = entry.IsSubEntry;
				}
				else
				{
					txtEdit.Enabled = itemSelected;
					txtOriginal.Enabled = itemSelected && txtOriginal.Text.Trim().Length > 0;
					hbxHexView.Enabled = itemSelected;
				}

				tsbGameSelect.Enabled = itemSelected;
			}
		}

		private string FileName()
		{
			return _fileAdapter == null || _fileAdapter.FileInfo == null ? string.Empty : _fileAdapter.FileInfo.Name;
		}

		// List
		private void treEntries_AfterSelect(object sender, TreeViewEventArgs e)
		{
			UpdateTextView();
			UpdatePreview();
			UpdateHexView();
			UpdateForm();
		}

		private void treEntries_KeyDown(object sender, KeyEventArgs e)
		{
			if (treEntries.Focused && (e.KeyCode == Keys.Enter))
				tsbEntryProperties_Click(sender, e);
		}

		private void treEntries_DoubleClick(object sender, EventArgs e)
		{
			tsbEntryProperties_Click(sender, e);
		}

		private void treEntries_AfterCollapse(object sender, TreeViewEventArgs e)
		{
			e.Node.Expand();
		}

		// Text
		private void txtEdit_KeyUp(object sender, KeyEventArgs e)
		{
			IEntry entry = (IEntry)treEntries.SelectedNode.Tag;
			string next = string.Empty;
			string previous = string.Empty;

			previous = _gameHandler.GetKuriimuString(entry.EditedTextString);
			next = txtEdit.Text.Replace("<null>", "\0").Replace("\r\n", "\n");
			entry.EditedText = entry.Encoding.GetBytes(_gameHandler.GetRawString(next));

			UpdatePreview();
			UpdateHexView();
			SelectInHex();

			if (next != previous)
				_hasChanges = true;

			UpdateForm();
		}

		private void txtEdit_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control & e.KeyCode == Keys.A)
				txtEdit.SelectAll();
			SelectInHex();
		}

		private void txtEdit_MouseUp(object sender, MouseEventArgs e)
		{
			SelectInHex();
		}

		private void SelectInHex()
		{
			// Magic
			IEntry entry = (IEntry)treEntries.SelectedNode?.Tag;

			if (entry != null)
			{
				int selectionStart = 0;
				int selectionLength = 0;

				string startToSelection = txtEdit.Text.Substring(0, txtEdit.SelectionStart);
				selectionStart = _gameHandler.GetRawString(startToSelection.Replace("<null>", "\0").Replace("\r\n", "\n")).Length * (entry.Encoding.IsSingleByte ? 1 : 2);
				selectionLength = _gameHandler.GetRawString(txtEdit.SelectedText.Replace("<null>", "\0").Replace("\r\n", "\n")).Length * (entry.Encoding.IsSingleByte ? 1 : 2);

				hbxHexView.SelectionStart = selectionStart;
				hbxHexView.SelectionLength = selectionLength;
			}
		}

		protected void hbxEdit_Changed(object sender, EventArgs e)
		{
			DynamicFileByteProvider dfbp = (DynamicFileByteProvider)sender;

			IEntry entry = (IEntry)treEntries.SelectedNode?.Tag;

			if (entry != null)
			{
				List<byte> bytes = new List<byte>();
				for (int i = 0; i < (int)dfbp.Length; i++)
					bytes.Add(dfbp.ReadByte(i));
				entry.EditedText = bytes.ToArray();

				UpdateTextView();

				if (txtEdit.Text != txtOriginal.Text)
					_hasChanges = true;

				UpdatePreview();
				UpdateForm();
			}
		}
	}
}