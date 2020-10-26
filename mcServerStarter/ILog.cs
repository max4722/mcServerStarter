// <copyright file="ILog.cs" company="kf corp">
// Licensed under the Apache 2.0 license
// </copyright>

using System;

namespace McServerStarter
{
    internal interface ILog<T>
    {
        void LogDebug(string message, params object[] args);

        void LogError(string message, params object[] args);

        void LogInformation(string message, params object[] args);

        void LogWarning(string message, params object[] args);
    }
}