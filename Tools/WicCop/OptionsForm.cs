//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules.ShellIntegration;

namespace Microsoft.Test.Tools.WicCop
{
    public partial class OptionsForm : Form
    {
        public OptionsForm(TreeNodeCollection form)
        {
            InitializeComponent();

            if (Settings.Default.Files != null)
            {
                filesListView.SuspendLayout();

                foreach (string s in Settings.Default.Files)
                {
                    filesListView.Items.Add(s);
                }

                filesListView.ResumeLayout();
                filesListView.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                filesListView.Columns[0].Width = Math.Max(filesListView.Columns[0].Width, filesListView.Width);
            }

            Dictionary<string, HashSet<string>> d = new Dictionary<string, HashSet<string>>();
            foreach (ExtensionRuleGroup e in form)
            {
                HashSet<string> set;
                if (!d.TryGetValue(e.FileTypeName, out set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    d.Add(e.FileTypeName, set);
                }
                set.Add(e.Extension);
            }

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, HashSet<string>> p in d.OrderBy(delegate(KeyValuePair<string, HashSet<string>> value) { return value.Key; }))
            {
                sb.Append(p.Key);
                sb.Append('|');
                bool first = true;
                foreach (string s in p.Value.OrderBy(delegate(string value) { return value; }))
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(';');
                    }
                    sb.Append('*');
                    sb.Append(s);
                }
                sb.Append('|');
            }

            sb.Append(openFileDialog.Filter);
            openFileDialog.Filter = sb.ToString();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (Settings.Default.Files == null)
            {
                Settings.Default.Files = new StringCollection();
            }
            else
            {
                Settings.Default.Files.Clear();
            }

            foreach (ListViewItem lvi in filesListView.Items)
            {
                Settings.Default.Files.Add(lvi.Text);
            }

            Settings.Default.Save();
        }

        private void filesListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            removeButton.Enabled = filesListView.SelectedItems.Count > 0;
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                filesListView.SuspendLayout();

                foreach (string s in openFileDialog.FileNames)
                {
                    filesListView.Items.Add(s);
                }

                filesListView.ResumeLayout();
            }
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            filesListView.SuspendLayout();
            
            foreach (ListViewItem lvi in filesListView.SelectedItems)
            {
                lvi.Remove();
            }

            filesListView.ResumeLayout();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
