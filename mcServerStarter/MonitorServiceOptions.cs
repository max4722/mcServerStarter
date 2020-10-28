// <copyright file="MonitorServiceOptions.cs" company="kf corp">
// Licensed under the Apache 2.0 license
// </copyright>

namespace McServerStarter
{
    using System;

    internal class MonitorServiceOptions
    {
        public string RootPath { get; set; }

        public string ProcessPath { get; set; }

        public string Args { get; set; }

        public int ShutDownTimeout { get; set; }
    }
}
