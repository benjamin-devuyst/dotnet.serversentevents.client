using Foxrough.ServerSentEvents.Client.Infrastructure;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Foxrough.ServerSentEvents.Client.Impl
{
    /// <summary>
    /// <c>ServerSentEventInstance</c> connect and catch data from a server that respect ServerSentEvent principles
    /// </summary>
    internal sealed class ServerSentEventInstance
    {
        private const int ReadBufferSize = 512;

        private readonly ILogger logger;

        private readonly string uri;
        private readonly CancellationToken cancelToken;
        private readonly Action<string> onDataReceived;
        private readonly Thread currentThread;
        private readonly TaskCompletionSource<bool> startedTaskCompletionSource = new TaskCompletionSource<bool>();

        private bool isStarted = false;

        public ServerSentEventInstance(string uri, CancellationToken cancelToken, Action<string> onDataReceived, ILogger logger)
        {
            this.uri = uri;
            this.cancelToken = cancelToken;
            this.onDataReceived = onDataReceived;
            this.logger = logger;

            this.currentThread = new Thread(this.OnThreadStarted) { IsBackground = true };
            this.cancelToken.Register(this.AbortCurrentRequestAfterDelay);
        }

        public int RetryDelay { get; set; }

        /// <summary>
        /// Try to abort request to unblock stream.Read()...
        /// </summary>
        private void AbortCurrentRequestAfterDelay()
        {
            const int delay = 500;
            Task.Delay(delay)
                .ContinueWith(t =>
                {
                    this.Log(TraceLevel.Warning, $"Abort request after timeout of {delay} milliseconds");
                    this.currentPollingContext?.Request.Abort();
                });
        }

        private void OnThreadStarted()
        {
            while (!this.cancelToken.IsCancellationRequested)
            {
                try { this.ProcessPollingUntilCancellation(); }
                catch (Exception e)
                {
                    var delay = this.RetryDelay;
                    this.Log(TraceLevel.Error, $"An error occurs during polling of '{this.uri}' - {e.Message}, retry in a while.");
                    Thread.Sleep(delay > 0 ? delay : ServerSentEventsConstants.MessageField_Retry_DefaultValue);
                }
            }
        }

        private PollingContext currentPollingContext;

        private void ProcessPollingUntilCancellation()
        {
            PollingContext context = null;
            try
            {
                this.Log(TraceLevel.Verbose, $"Get polling context for '{this.uri}'");
                context = GetOpenedPollingContext(this.uri);

                this.SetInstanceCurrentPollingContext(context);

                this.Log(TraceLevel.Verbose, $"Poll data from '{this.uri}'");
                this.PollDataFromStream(context.ResponseStream);
            }
            finally
            {
                context?.Dispose();
            }
        }

        private void SetInstanceCurrentPollingContext(PollingContext context) => this.currentPollingContext = context;

        private void PollDataFromStream(Stream currentStream)
        {
            var decoder = new UTF8Encoding();
            var buffer = new byte[ReadBufferSize];

            if (!currentStream.CanRead) return;

            while (!this.cancelToken.IsCancellationRequested)
            {
                // Read on stream that come from WebResponse is blocker at read until data is present
                var bytesRead = currentStream.Read(buffer, 0, ReadBufferSize);
                if (bytesRead <= 0) continue; // to respect MSDN (some case Read is returns with no data returns)

                var text = decoder.GetString(buffer, 0, bytesRead);

                this.onDataReceived(text);
            }
        }

        /// <summary>
        /// Factory that creates the web request, gets the response and open the stream
        /// </summary>
        /// <exception cref="NotSupportedException">Can occurs during <see cref="WebRequest"/> Creation</exception>
        /// <exception cref="SecurityException">Can occurs during <see cref="WebRequest"/> Creation</exception>
        private static PollingContext GetOpenedPollingContext(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.AllowReadStreamBuffering = false;
            request.AllowWriteStreamBuffering = false;

            var response = (HttpWebResponse)request.GetResponse();
            var currentStream = response.GetResponseStream();

            if (currentStream == null)
                throw new NullReferenceException("GetResponseStream returns null.");

            return new PollingContext(request, response, currentStream);
        }

        public Task StartAsync()
        {
            if (this.isStarted) return this.startedTaskCompletionSource.Task;

            this.currentThread.Start();
            this.isStarted = true;

            return this.startedTaskCompletionSource.Task;
        }

        private void Log(TraceLevel level, string message, [CallerMemberName]string callerMemberName = null)
            => this.logger.Write(level, nameof(ServerSentEventInstance), callerMemberName, message);

        private class PollingContext : IDisposable
        {
            public PollingContext(HttpWebRequest request, HttpWebResponse response, Stream responseStream)
            {
                this.Request = request;
                this.Response = response;
                this.ResponseStream = responseStream;
            }

            public HttpWebRequest Request { get; }

            public HttpWebResponse Response { get; }

            public Stream ResponseStream { get; }

            /// <inheritdoc />
            public void Dispose()
            {
                this.ResponseStream.Close();

                this.ResponseStream.Dispose();
                this.Response.Dispose();
                this.ResponseStream.Dispose();

                GC.SuppressFinalize(this);
            }
        }
    }

}