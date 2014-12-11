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

namespace Microsoft.Test.Tools.WicCop.Rules
{
    class ComponentRuleGroup : RuleBase
    {
        readonly Guid clsid;

        public ComponentRuleGroup(Guid clsid, string text, Action<ComponentRuleGroup> childCreator)
            : base(text)
        {
            this.clsid = clsid;
            childCreator(this);
        }

        public Guid Clsid
        {
            get { return clsid; }
        }
    }
}
