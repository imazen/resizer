// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using ImageResizer.Configuration;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     All plugins must implement this. Enables web.config addition and removal.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        ///     Installs the plugin in the specified Config instance. The plugin must handle all the work of loading settings,
        ///     registering the plugin etc.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        IPlugin Install(Config c);

        /// <summary>
        ///     Uninstalls the plugin. Should reverse all changes made during Install
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        bool Uninstall(Config c);
    }
}