using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ImageResizer.ReleaseBuilder.Classes {
    public class Interaction {

        public bool ask(string question) {
            Console.WriteLine(question);
            bool yes = Console.ReadKey(false).KeyChar.ToString().ToLower().Equals("y");
            Console.WriteLine();
            return yes;
        }
        public string change(string message, string defaultValue) {
            Console.Write(message + " (" + defaultValue + "):");
            string response = Console.ReadLine();
            if (string.IsNullOrEmpty(response.Trim())) return defaultValue;
            else return response.Trim();
        }
        public void nl() { Console.WriteLine(); }
        public void say(string text) { Console.WriteLine(text); }

        public void PromptDeleteBatch(string message, string[] paths) {
            List<string> exist = new List<string>();
            foreach (string s in paths)
                if (File.Exists(s)) exist.Add(s);

            if (exist.Count > 0) {
                Console.WriteLine(message);
                foreach (string s in exist) Console.WriteLine(s);

                if (Console.ReadKey(false).KeyChar.ToString().ToLower().Equals("y")) {
                    foreach (string s in exist) {

                        File.Delete(s);
                        Console.WriteLine("Deleted " + s);
                    }


                } else Console.WriteLine("No files will be overwritten.");

            }
        }
    }
}
