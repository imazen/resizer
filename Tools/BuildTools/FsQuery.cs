using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BuildTools {
    /// <summary>
    /// Provides high-speed, cached, regex-capable filesystem query ability. 
    /// </summary>
    public class FsQuery {
        string[] _files;
        string[] _folders;
        string baseDir = null;

        public FsQuery(string dir):this(dir,null) {

        }

        public FsQuery(string dir, IEnumerable<string> exclude) {
            baseDir = dir.TrimEnd('\\','/');
            Rescan();
            exclusions = new List<Pattern>();
            if (exclude != null) AddExclusions(exclude);
           
        }

        public void Rescan()
        {
            _files = Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories);
            _folders = Directory.GetDirectories(baseDir, "*", SearchOption.AllDirectories);
        }


        public void AddExclusions(IEnumerable<string> exclude) {
            foreach (string s in exclude) exclusions.Add(new Pattern(s));
        }

        public List<Pattern> exclusions = null;


        public List<string> files(params string[] p) {
            return query(_files, exclusions, toPatterns(p));
        }
        public List<string> folders(params string[] p) {
            return query(_folders, exclusions, toPatterns(p));
        }


        public List<string> files(params Pattern[] p){
            return query(_files, exclusions, p);
        }
        public List<string> folders(params Pattern[] p) {
            return query(_folders, exclusions, p);
        }

        public Pattern[] toPatterns(string[] patterns) {
            Pattern[] p = new Pattern[patterns.Length];
            for (int i = 0; i < patterns.Length; i++)
                p[i] = new Pattern(patterns[i]);
            return p;
        }

        protected List<String> query(string[] items, IEnumerable<Pattern> exclusions, IEnumerable<Pattern> queries) {
            Console.WriteLine("Applying queries:");
            foreach (Pattern p in queries) {
                Console.WriteLine(p.ToString());
            }
            List<string> results = new List<string>();
            foreach (string item in items) {
                //Trim to basedir
                if (!item.StartsWith(baseDir)) throw new Exception("Paths don't match: " + item + " , " + baseDir);
                string s = item.Substring(baseDir.Length).TrimEnd('\\', '/');

                bool excluded = false;
                if (exclusions != null)
                    foreach (Pattern e in exclusions)
                        if (e.IsMatch(s)) {
                            excluded = true;
                           // Console.WriteLine("Excluded " + s);
                            break;
                        }

                if (!excluded) {
                    foreach (Pattern q in queries) {
                        if (q.IsMatch(s)) {
                            results.Add(item);
                            break;
                        }
                    }
                }
            }
            return results;
        }


    }
}
