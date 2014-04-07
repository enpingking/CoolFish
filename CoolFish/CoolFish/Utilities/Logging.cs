using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;

namespace CoolFishNS.Utilities
{
    /// <summary>
    ///     This class handles all logging done for Coolfish. From simple output, to writing to files, to logging exceptions
    ///     and user messages. It is event based, and thread safe.
    /// </summary>
    /// <example>Logging.Write("Oh noes! We failed again!");</example>
    public class Logging : IDisposable
    {
        #region Delegates

        /// <summary>
        ///     Delegate method that log events should conform to if they are to be fired
        /// </summary>
        public delegate void LogDelegate(object sender, MessageEventArgs e);

        /// <summary>
        ///     Delegate method that log events should conform to if they are to be fired
        /// </summary>
        public delegate void WriteDelegate(object sender, MessageEventArgs e);

        /// <summary>
        ///     Occurs when Logging.Write is called
        /// </summary>
        public static event WriteDelegate OnWrite;

        /// <summary>
        ///     Occurs when Logging.Log is called
        /// </summary>
        public static event LogDelegate OnLog;

        #endregion

        private static readonly Logging Instance;

        private readonly ManualResetEvent _hasNewItems = new ManualResetEvent(false);
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        private readonly ManualResetEvent _terminate = new ManualResetEvent(false);
        private readonly ManualResetEvent _waiting = new ManualResetEvent(false);
        private uint _exceptionCount;
        private Thread _processThread;

        static Logging()
        {
            CoolFishNS.Utilities.Log.StartUp();
            Instance = new Logging();
        }

        private Logging()
        {
            _processThread = new Thread(ProcessQueue)
            {
                IsBackground = true,
                CurrentUICulture = new CultureInfo("en-US"),
                CurrentCulture = new CultureInfo("en-US")
            };
            _processThread.Start();
        }

        private static void WriteAsync(object format, params object[] args)
        {
            OnWrite("Logging.Write", new MessageEventArgs(string.Format(format.ToString(), args)));
        }

        private static void LogAsync(object format, params object[] args)
        {
            OnLog("Logging.Log", new MessageEventArgs(string.Format(format.ToString(), args)));
        }

        /// <summary>
        ///     Writes the specified message to the message queue.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void Write(object format, params object[] args)
        {
            if (format != null)
            {
                Instance._queue.Enqueue(() => WriteAsync(format, args));
                Instance._hasNewItems.Set();
            }
        }

        /// <summary>
        ///     Logs the specified message to the message queue.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void Log(object format, params object[] args)
        {
            if (format != null)
            {
                Instance._queue.Enqueue(() => LogAsync(format, args));
                Instance._hasNewItems.Set();
            }
        }

        private void ProcessQueue()
        {
            while (true)
            {
                try
                {
                    _waiting.Set();
                    int i = WaitHandle.WaitAny(new WaitHandle[] {_hasNewItems, _terminate});

                    _hasNewItems.Reset();
                    _waiting.Reset();


                    while (!_queue.IsEmpty)
                    {
                        Action action;
                        if (_queue.TryDequeue(out action))
                        {
                            action();
                        }
                    }

                    if (i == 1)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _exceptionCount++;
                    if (_exceptionCount > 5)
                    {
                        throw new Exception("Exception while trying to Log", ex);
                    }
                    Log("Exception while trying to Log:" + ex);
                }
            }
        }


        /// <summary>
        ///     Blocks the current thread until all messages have been flushed from the logging queue
        /// </summary>
        public static void Flush()
        {
            Instance._waiting.WaitOne();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _terminate.Set();
                    _processThread = null;
                    _waiting.Dispose();
                    _terminate.Dispose();
                    _hasNewItems.Dispose();
                }
                catch
                {
                }
            }
        }

        ~Logging()
        {
            Dispose(false);
        }

        #endregion
    }
}