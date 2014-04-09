/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace PhotoshopFile.Text
{
    public class TdTaStylesheetReader
    {

        private Dictionary<string, object> tree;
        public TdTaStylesheetReader(Dictionary<string, object> tree)
        {
            this.tree = tree;
        }

        public string Text { get { return TdTaParser.getString(tree, "EngineDict.Editor.Text$"); } }

        public int ParagraphRunCount { get { return TdTaParser.getList(tree, "EngineDict.ParagraphRun.RunLengthArray").Count; } }
        public int StyleRunCount { get { return TdTaParser.getList(tree, "EngineDict.StyleRun.RunLengthArray").Count; } }

        public Dictionary<string, object> getParagraphRun(int index) { return BuildRunItem("EngineDict.ParagraphRun", index); }
        public Dictionary<string, object> getStyleRun(int index) { return BuildRunItem("EngineDict.StyleRun", index); }
        public List<int> getStyleRunLengths() { return GetRunLengthArray("EngineDict.StyleRun"); }
        public List<int> getParagraphRunLengths() { return GetRunLengthArray("EngineDict.StyleRun"); }

        protected List<int> GetRunLengthArray(string baseSelector)
        {
            List<object> a =  (List<object>)TdTaParser.getList(tree, baseSelector + ".RunLengthArray");
            //Copy to strongly-typed array
            List<int> n = new List<int>();
            foreach (object o in a) n.Add((int)o);
            return n;
        }

        protected Dictionary<string, object> BuildRunItem(string baseSelector, int index)
        {
            return TdTaParser.MergeObjects(TdTaParser.getDict(tree, baseSelector + ".DefaultRunData"), TdTaParser.getList(tree, baseSelector + ".RunArray")[index]);
        }


        public List<object> getFontSet()
        {
            return TdTaParser.getList(tree, "ResourceDict.FontSet");
        }

        public Dictionary<string, object> GetStylesheetDataFromLongestRun()
        {
            int index = GetIndexOfLargestValue(getStyleRunLengths());
            return TdTaParser.getDict(getStyleRun(index), "StyleSheet.StyleSheetData");
        }

        
        public int GetIndexOfLargestValue(List<int> l)
        {
            int maxval = -1;
            int maxix = -1;
            for (int i = 0; i < l.Count; i++)
            {
                if (l[i] > maxval)
                {
                    maxval = l[i];
                    maxix = i;
                }
            }
            return maxix;
        }

    }
}
