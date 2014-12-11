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
using System.Threading;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;
using Microsoft.Test.Tools.WicCop.Rules;
using Microsoft.Test.Tools.WicCop.Rules.ShellIntegration;

namespace Microsoft.Test.Tools.WicCop
{
    public class Remote : MarshalByRefObject
    {
        [Serializable]
        public struct MessageInfo
        {
            public readonly string Path;
            public readonly string Text;
            public readonly List<DataEntryCollection> Data;

            internal MessageInfo(Message m)
            {
                Path = m.Parent.FullPath;
                Text = m.Text;
                Data = m.Data;
            }
        }

        MainForm form = new MainForm(true);

        public const string ObjectName = "Remote";

        private ManualResetEvent ev = new ManualResetEvent(false);

        public void Exit()
        {
            Console.WriteLine("Exit");

            ev.Set();
        }

        public void Wait()
        {
            ev.WaitOne();
        }

        public void SetFiles(StringCollection files)
        {
            Settings.Default.Files = files;
        }

        public MessageInfo[] Run(string path)
        {
            RuleBase rule = form.GetRule(path);
            if (rule == null)
            {
                return null;
            }
            else
            {
                rule.Run(form);

                List<MessageInfo> res = new List<MessageInfo>();
                foreach (Message m in form.Messages)
                {
                    Console.WriteLine(m);
                    res.Add(new MessageInfo(m));
                }
                form.Messages.Clear();


                return res.ToArray();
            }
        }

        public KeyValuePair<Guid, string>[] GetComponentInfoPairs(WICComponentType type)
        {
            return AllComponentsRuleGroup.GetComponentInfoPairs(type, null).ToArray();
        }

        public Dictionary<string, KeyValuePair<HashSet<string>, HashSet<Guid>>> GetExtensions()
        {
            return ShellIntegrationRuleGroup.GetExtensions(null);
        }
    }
}
