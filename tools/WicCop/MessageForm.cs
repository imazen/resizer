//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Forms;

using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop
{
    partial class MessageForm : Form
    {
        private Message message;

        public MessageForm(Message message)
        {
            InitializeComponent();

            this.message = message;
            Update(0);
        }

        private void Update(int delta)
        {
            int index = message.ListView.Items.IndexOf(message) + delta;

            message = (Message)message.ListView.Items[index];

            previousButton.Enabled = index > 0;
            nextButton.Enabled = index < message.ListView.Items.Count - 1;

            index = 0;
            occuranceListBox.Items.Clear();
            foreach (DataEntryCollection d in message.Data)
            {
                index++;

                occuranceListBox.Items.Add(string.Format(CultureInfo.CurrentUICulture, Resources.Occurance, index));
            }
            occuranceListBox.SelectedIndex = 0;

            messageTextBox.Text = message.Text;
        }

        private void previousButton_Click(object sender, EventArgs e)
        {
            Update(-1);
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            Update(1);
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void occuranceListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataEntryCollection data = message.Data[occuranceListBox.SelectedIndex];
            if (data.Count == 0)
            {
                noPropertiesTextBox.Visible = true;
                messagePropertyGrid.Visible = false;
            }
            else
            {
                messagePropertyGrid.SelectedObject = message.Data[occuranceListBox.SelectedIndex];
                noPropertiesTextBox.Visible = false;
                messagePropertyGrid.Visible = true;
            }
        }
    }
}
