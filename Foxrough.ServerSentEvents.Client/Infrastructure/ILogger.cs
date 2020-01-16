using System;
using System.Diagnostics;

namespace Foxrough.ServerSentEvents.Client.Infrastructure
{
    /// <summary>
    ///     Contract of a logger
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        ///     Write a log
        /// </summary>
        void Write(TraceLevel level, string className, string methodName, string message);
    }
}