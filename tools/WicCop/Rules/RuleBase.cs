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
using Microsoft.Test.Tools.WicCop.Rules.Wow;

namespace Microsoft.Test.Tools.WicCop.Rules
{
    public abstract class RuleBase : TreeNode
    {
        protected RuleBase(string text)
            : base(text)
        {
        }

        protected virtual void RunOverride(MainForm form, object tag)
        {
        }

        void RunInternal(MainForm form)
        {
            form.SetStatus(string.Format(CultureInfo.CurrentUICulture, Resources.Running, FullPath));
            try
            {
                RunOverride(form, null);
            }
            catch (Exception e)
            {
                form.Add(this, e.TargetSite.ToString(Resources._0_Failed), new DataEntry(e));
            }
        }

        public void Run(MainForm form)
        {
            form.Run(RunInternal);
            foreach (RuleBase rule in Nodes)
            {
                rule.Run(form);
            }
        }
    }

    abstract class RuleBase<T> : RuleBase where T : RuleBase
    {
        static readonly bool wow = IntPtr.Size == 8;

        protected RuleBase(string text)
            : base(text)
        {
            if (wow && !Program.NoWow)
            {
                Nodes.Add(CreateSubWowRule());
            }
        }

        protected virtual WowRule CreateSubWowRule()
        {
            return new WowRule(); 
        }

        new public T Parent
        {
            get { return base.Parent as T; }
        }
    }
}
