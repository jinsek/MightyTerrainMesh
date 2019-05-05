// -----------------------------------------------------------------------
// <copyright file="SimpleLog.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace TriangleNet.Log
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A simple logger, which logs messages to a List.
    /// </summary>
    /// <remarks>Using singleton pattern as proposed by Jon Skeet.
    /// http://csharpindepth.com/Articles/General/Singleton.aspx
    /// </remarks>
    public sealed class SimpleLog : ILog<SimpleLogItem>
    {
        private List<SimpleLogItem> log = new List<SimpleLogItem>();

        private LogLevel level = LogLevel.Info;

        #region Singleton pattern

        private static readonly SimpleLog instance = new SimpleLog();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static SimpleLog() { }

        private SimpleLog() { }

        public static ILog<SimpleLogItem> Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion

        public void Add(SimpleLogItem item)
        {
            log.Add(item);
        }

        public void Clear()
        {
            log.Clear();
        }

        public void Info(string message)
        {
            log.Add(new SimpleLogItem(LogLevel.Info, message));
        }

        public void Warning(string message, string location)
        {
            log.Add(new SimpleLogItem(LogLevel.Warning, message, location));
        }

        public void Error(string message, string location)
        {
            log.Add(new SimpleLogItem(LogLevel.Error, message, location));
        }

        public IList<SimpleLogItem> Data
        {
            get { return log; }
        }

        public LogLevel Level
        {
            get { return level; }
        }
    }
}
