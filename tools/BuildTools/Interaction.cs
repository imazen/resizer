using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BuildTools {
    public class Interaction {

        /// <summary>
        /// Makes the console window half the max horizontal and vertical height it can be. This produces a readable, usable windows size.
        /// Sets the buffer size to 5000 lines so you can actually read the log.
        /// </summary>
        public void MakeConsoleNicer() {
            Console.WindowHeight = Console.LargestWindowHeight / 2;
            Console.WindowWidth = Console.LargestWindowWidth / 2;
            Console.SetBufferSize(Console.WindowWidth, 5000);
        }
        /// <summary>
        /// Returns true if and only if the user presses 'y'
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        public bool ask(string question) {
            Console.WriteLine(question);
            bool yes = Console.ReadKey(false).KeyChar.ToString().ToLower().Equals("y");
            Console.WriteLine();
            return yes;
        }
        /// <summary>
        /// Prompts the user to change the specified default value for the 'message', in the form "Message (defaultValue):" 
        /// Returns the result if specified, otherwise returns the defaultValue. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string change(string message, string defaultValue) {
            Console.Write(message + " (" + defaultValue + "):");
            string response = Console.ReadLine();
            if (string.IsNullOrEmpty(response.Trim())) return defaultValue;
            else return response.Trim();
        }

        /// <summary>
        /// Prints a key/value pair and returns the value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string list(string name, string value) {
            Console.WriteLine(name.PadRight(25,'.') + " " + value);
            return value;
        }
        /// <summary>
        /// Writes a newline.
        /// </summary>
        public void nl() { Console.WriteLine(); }
        /// <summary>
        /// Writes a line of text to the console.
        /// </summary>
        /// <param name="text"></param>
        public void say(string text) { Console.WriteLine(text); }

        public void say(string text, ConsoleColor color) {
            ConsoleColor original = Console.ForegroundColor;
            Console.ForegroundColor =color; 
            Console.WriteLine(text);
            Console.ForegroundColor = original;
        }


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
