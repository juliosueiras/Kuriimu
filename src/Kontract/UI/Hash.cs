﻿using System;
using System.IO;
using System.Windows.Forms;
using Kontract.IO;
using Kontract.Hash;

namespace Kontract.UI
{
    public static class HashTools
    {
        public static void LoadHashTools(ToolStripMenuItem tsb)
        {
            ToolStripMenuItem tsb2;
            ToolStripMenuItem tsb3;
            tsb.DropDownItems.Clear();

            //General
            tsb.DropDownItems.Add(new ToolStripMenuItem("General", null));
            tsb2 = (ToolStripMenuItem)tsb.DropDownItems[0];
            tsb2.DropDownItems.Add(new ToolStripMenuItem(Hash.SHA256.ToString(), null));
            tsb3 = (ToolStripMenuItem)tsb2.DropDownItems[0];
            tsb3.DropDownItems.Add(new ToolStripMenuItem("Create", null, Create));
            tsb3.DropDownItems[0].Tag = Hash.SHA256;

            //3DS
            tsb.DropDownItems.Add(new ToolStripMenuItem("3DS", null));
            tsb2 = (ToolStripMenuItem)tsb.DropDownItems[1];
            tsb2.DropDownItems.Add(new ToolStripMenuItem(Hash.CRC32.ToString(), null));
            tsb3 = (ToolStripMenuItem)tsb2.DropDownItems[0];
            tsb3.DropDownItems.Add(new ToolStripMenuItem("Create", null, Create));
            tsb3.DropDownItems[0].Tag = Hash.CRC32;
        }

        public static void Create(object sender, EventArgs e)
        {
            var tsi = sender as ToolStripMenuItem;

            FileStream openFile = null;
            switch (tsi.Tag)
            {
                case Hash.CRC32:
                    ;
                    var ofd = new OpenFileDialog
                    {
                        Title = Hash.CRC32.ToString(),
                        Filter = "All Files (*.*)|*.*"
                    };

                    if (ofd.ShowDialog() != DialogResult.OK) break;
                    openFile = File.OpenRead(ofd.FileName);
                    MessageBox.Show($"0x{Crc32.Create(new BinaryReaderX(openFile).ReadBytes((int)openFile.Length)):X8}", tsi?.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    openFile.Close();
                    break;
                case Hash.SHA256:
                    ofd = new OpenFileDialog
                    {
                        Title = Hash.SHA256.ToString(),
                        Filter = "All Files (*.*)|*.*"
                    };

                    if (ofd.ShowDialog() != DialogResult.OK) break;
                    openFile = File.OpenRead(ofd.FileName);
                    MessageBox.Show($"{SHA256.Create(new BinaryReaderX(openFile).ReadBytes((int)openFile.Length)):X}", tsi?.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    openFile.Close();
                    break;
            }
        }

        public enum Hash : byte
        {
            CRC32,
            SHA256
        }
    }
}
