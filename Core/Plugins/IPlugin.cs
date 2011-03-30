using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Configuration;

namespace fbs.ImageResizer.Plugins {
    public interface IPlugin {
        /// <summary>
        /// The plugin should save a reference to the Config instance 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        IPlugin Install(Config c);
        bool Uninstall(Config c);
        /// <summary>
        /// The short name of the plugin. Should match the plugin namespace and default class.
        /// </summary>
        string ShortName { get; }
    }
}
