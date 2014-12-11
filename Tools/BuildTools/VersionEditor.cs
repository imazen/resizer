using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace BuildTools {
    public class VersionEditor {
        string filename;
        string contents = null;

        public string Contents {
            get { return contents; }
            set { contents = value; }
        }
        public VersionEditor(string filename) {
            contents = File.ReadAllText(filename, UTF8Encoding.UTF8);
            this.filename = filename;
        }

        public Regex getRegex(string name){
            return new Regex("(?<before>\\[assembly\\:\\s*" + Regex.Escape(name) + "\\s*\\(\\s*\"" + ")(?<value>[^\"]*)(?<after>" + "\"\\s*\\)\\s*\\])", RegexOptions.IgnoreCase);
        }

        public string get(string name) {
            Regex r = getRegex(name);
            Match m = r.Match(Contents);
            if (m.Success) return m.Groups["value"].Value;
            return null;
        }
        /// <summary>
        /// Appends 'ending' to 'version' unless version already has 4 segments. 
        /// </summary>
        /// <param name="version"></param>
        /// <param name="ending"></param>
        /// <returns></returns>
        public string join(string version, string ending) {
            version = version.Trim('.');
            if (version.Split('.').Length >= 4) return version; //Already has 4 sections

            version = version + "." + ending.TrimStart('.');
            return version;
        }

        /// <summary>
        /// Replaces asterisks with the specified value
        /// </summary>
        /// <param name="version"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string resolve(string version, int value) {
            return version.Replace("*", value.ToString());
        }

        public bool set(string name, string value) {
            Regex r = getRegex(name);
            bool worked = false;

            Contents = r.Replace(Contents, new MatchEvaluator(delegate(Match m){
                worked = true;
                return m.Groups["before"].Value + value + m.Groups["after"].Value;
            }));
            return worked;
        }


        public void Save() {
            File.WriteAllText(filename, Contents, UTF8Encoding.UTF8);
        }
    }
}
