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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules;
using Microsoft.Test.Tools.WicCop.Rules.Com;
using Microsoft.Test.Tools.WicCop.Rules.Decoder;
using Microsoft.Test.Tools.WicCop.Rules.Encoder;
using Microsoft.Test.Tools.WicCop.Rules.FormatConverter;
using Microsoft.Test.Tools.WicCop.Rules.PixelFormat;
using Microsoft.Test.Tools.WicCop.Rules.ShellIntegration;

namespace Microsoft.Test.Tools.WicCop
{
    public partial class MainForm : Form
    {
        public readonly Remote remote;

        private readonly ShellIntegrationRuleGroup shellIntegrationRuleGroup;
        private readonly Queue<Action<MainForm>> tasks = new Queue<Action<MainForm>>();
        private readonly bool remoted;

        public MainForm(bool remoted)
        {
            InitializeComponent();

            this.remoted = remoted;
            if (!remoted)
            {
                if (IntPtr.Size == 4)
                {
                    Text += " - " + Resources.X86;
                }
                else
                {
                    Text += " - " + Resources.Amd64;

                    if (!Program.NoWow)
                    {
                        string channel = string.Format(CultureInfo.InvariantCulture, "{0:N}{1:X}", Guid.NewGuid(), Handle);
                        int id;
                        using (Process p = Process.GetCurrentProcess())
                        {
                            id = p.Id;
                        }

                        using (Process p = Process.Start("WicCop.32BitsLoader.exe", string.Format(CultureInfo.InvariantCulture, "{0} {1}", channel, id)))
                        {
                            remote = (Remote)Activator.GetObject(typeof(MarshalByRefObject), string.Format(CultureInfo.InvariantCulture, "ipc://{0}/{1}", channel, Remote.ObjectName));
                            for (int i = 0; i < 7; i++)
                            {
                                try
                                {
                                    remote.SetFiles(Settings.Default.Files);
                                    break;
                                }
                                catch (RemotingException)
                                {
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                    }
                }
            }

            rulesTreeView.Nodes.Add(new AllComponentsRuleGroup(Resources.Decoders_Text, WICComponentType.WICDecoder, AddDecoderRules, remote));
            rulesTreeView.Nodes.Add(new AllComponentsRuleGroup(Resources.Encoders_Text, WICComponentType.WICEncoder, AddEncoderRules, remote));
            rulesTreeView.Nodes.Add(new AllComponentsRuleGroup(Resources.PixelFormats_Text, WICComponentType.WICPixelFormat, AddPixelFormatRules, remote));
            rulesTreeView.Nodes.Add(new AllComponentsRuleGroup(Resources.PixelFormatConverters_Text, WICComponentType.WICPixelFormatConverter, AddFormatConverterRules, remote));

            shellIntegrationRuleGroup = new ShellIntegrationRuleGroup(remote);
            rulesTreeView.Nodes.Add(shellIntegrationRuleGroup);

            rulesTreeView.Sort();

        }

        internal void RunParentRemotely(RuleBase rule)
        {
            string path = rule.Parent.FullPath;
            Remote.MessageInfo[] a = remote.Run(path);
            DataEntry de = new DataEntry(Resources.Mode, Resources.Wow);
            if (a == null)
            {
                Add(rule, Resources.WowNotImplemented, de);
            }
            else
            {
                foreach (Remote.MessageInfo m in a)
                {
                    RuleBase r = m.Path == path ? rule : GetRule(m.Path);

                    foreach (DataEntryCollection c in m.Data)
                    {
                        Add(r, m.Text, c.ToArray(), de);
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            tasks.Clear();
            base.OnClosed(e);
            if (remote != null)
            {
                try
                {
                    remote.Exit();
                }
                catch (RemotingException)
                {
                }
            }
        }

        private static void AddDecoderRules(ComponentRuleGroup node)
        {
            node.Nodes.Add(new ComRule());
            node.Nodes.Add(new BitmapDecoderInfoRule());
            node.Nodes.Add(new BitmapDecoderRule());
            node.Nodes.Add(new BitmapSourceTransformRule());
            node.Nodes.Add(new DevelopRawRule());
        }

        private static void AddPixelFormatRules(ComponentRuleGroup node)
        {
            node.Nodes.Add(new PixelFormatInfoRule());
        }

        private static void AddFormatConverterRules(ComponentRuleGroup node)
        {
            node.Nodes.Add(new ComRule());
            node.Nodes.Add(new FormatConverterInfoRule());
        }

        private static void AddEncoderRules(ComponentRuleGroup node)
        {
            node.Nodes.Add(new ComRule());
            node.Nodes.Add(new BitmapEncoderInfoRule());
            node.Nodes.Add(new BitmapEncoderRule());
            node.Nodes.Add(new BitmapFrameEncode());
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void runSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: Do we want to auto-select the first set of tests?
            if (!Run(rulesTreeView.Nodes))
            {
                RuleBase rule = rulesTreeView.SelectedNode as RuleBase;
                if (rule != null)
                {
                    rule.Run(this);
                }
            }
            toolStripProgressBar.Maximum = tasks.Count;
            toolStripProgressBar.Value = 0;
            toolStripProgressBar.Enabled = true;
            toolStripProgressBar.Style = ProgressBarStyle.Marquee;

            runSelectedToolStripMenuItem.Enabled = false;
            optionsToolStripMenuItem.Enabled = false;
            reportToolStripMenuItem.Enabled = false;
            backgroundWorker.RunWorkerAsync();
        }

        private void performanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form f = new PerformanceForm())
            {
                f.ShowDialog(this);
            }
        }

        private void reportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.Indent = true;
            xws.NewLineOnAttributes = true;
            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                using (XmlWriter xr = XmlWriter.Create(saveFileDialog.FileName, xws))
                {
                    xr.WriteStartElement("WicCop");
                    foreach (Message m in messagesListView.Items)
                    {
                        m.WriteTo(xr);
                    }
                }
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form f = new OptionsForm(shellIntegrationRuleGroup.Nodes))
            {
                f.ShowDialog(this);
            }
            if (remote != null)
            {
                remote.SetFiles(Settings.Default.Files);
            }
        }

        private void messagesListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (messagesListView.SelectedItems.Count > 0)
            {
                new MessageForm(messagesListView.SelectedItems[0] as Message).Show(this);
            }
        }

        private bool Run(TreeNodeCollection colection)
        {
            bool res = false;

            foreach (RuleBase rule in colection)
            {
                if (rule.Checked)
                {
                    rule.Run(this);
                    res = true;
                }
                else
                {
                    res = res || Run(rule.Nodes);
                }
            }

            return res;
        }

        private void Run(object sender, DoWorkEventArgs e)
        {
            while (tasks.Count > 0)
            {
                tasks.Dequeue()(this);
                Invoke(new Action(UpdateProgress));
            }
            Invoke(new Action(RunFinished));
        }

        private void UpdateProgress()
        {
            toolStripProgressBar.Value++;
        }

        private void RunFinished()
        {
            runSelectedToolStripMenuItem.Enabled = true;
            optionsToolStripMenuItem.Enabled = true;
            reportToolStripMenuItem.Enabled = true;
            toolStripProgressBar.Enabled = false;
            toolStripProgressBar.Style = ProgressBarStyle.Blocks;

            SetStatus(Resources.Done);
        }

        internal void Run(Action<MainForm> action)
        {
            if (remoted)
            {
                action(this);
            }
            else
            {
                tasks.Enqueue(action);
            }
        }

        internal RuleBase GetRule(string path)
        {
            return GetRule(path, rulesTreeView.Nodes);
        }

        private static RuleBase GetRule(string path, TreeNodeCollection collection)
        {
            foreach (RuleBase rule in collection)
            {
                if (rule.FullPath == path)
                {
                    return rule;
                }

                RuleBase res = GetRule(path, rule.Nodes);
                if (res != null)
                {
                    return res;
                }
            }

            return null;
        }

        internal void Add(RuleBase parent, string text, params DataEntry[] data)
        {
            Add(parent, text, data, new DataEntry[0]);
        }

        internal void Add(RuleBase parent, string text, DataEntry[] de, params DataEntry[] data)
        {
            if (remoted)
            {
                AddInternal(parent, text, de, data);
            }
            else
            {
                Invoke(new Action<RuleBase, string, DataEntry[], DataEntry[]>(AddInternal), new object[] { parent, text, de, data });
            }
        }

        internal void CheckHRESULT(RuleBase parent, WinCodecError error, Exception e, params DataEntry[] de)
        {
            CheckHRESULT(parent, error, e, null, de);
        }

        internal void CheckHRESULT(RuleBase parent, WinCodecError error, Exception e, string param, params DataEntry[] de)
        {
            if (Marshal.GetHRForException(e) != (int)error)
            {
                string text = param == null ? e.TargetSite.ToString(Resources._0_FailedWithIncorrectHRESULT) : e.TargetSite.ToString(Resources._0_FailedWithIncorrectHRESULT, param);

                Add(parent, text, de, new DataEntry(Resources.Actual, e), new DataEntry(Resources.Expected, error));
            }
        }

        private void AddInternal(RuleBase parent, string text, DataEntry[] de, DataEntry[] data)
        {
            foreach (Message m in messagesListView.Items)
            {
                if (m.Parent == parent && m.Text == text)
                {
                    m.Data.Add(new DataEntryCollection(parent, de, data));

                    return;
                }
            }

            messagesListView.Items.Add(new Message(parent, text, de, data));
        }

        internal ListView.ListViewItemCollection Messages
        {
            get { return messagesListView.Items; }
        }

        internal void SetStatus(string status)
        {
            toolStripStatusLabel.Text = status;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AboutBox about = new AboutBox())
            {
                about.ShowDialog(this);
            }
        }
    }
}

