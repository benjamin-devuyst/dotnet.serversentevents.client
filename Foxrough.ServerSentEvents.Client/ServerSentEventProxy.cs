using Foxrough.ServerSentEvents.Client.Impl;
using Foxrough.ServerSentEvents.Client.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Foxrough.ServerSentEvents.Client
{
    /// <summary>
    /// Client proxy that can connect a server that respect ServerSentEvents and provides <see cref="EventMessage"/> when available
    /// </summary>
    public sealed class ServerSentEventProxy
    {
        private readonly string uri;
        private readonly ILogger logger;
        private readonly EventMessagesParsingStrategy messageParser = new EventMessagesParsingStrategy();

        private bool isRunning = false;
        private CancellationTokenSource cancellationTokenSource;

        private ServerSentEventInstance currentInstance;

        public event EventHandler<IReadOnlyCollection<EventMessage>> DataReceived;

        private readonly SynchronizationContext currentSyncContext;

        public ServerSentEventProxy(string uri, ILogger logger)
        {
            this.uri = uri;
            this.logger = logger;
            this.currentSyncContext = SynchronizationContext.Current;
        }

        public void Start()
        {
            if (this.isRunning) return;

            this.SetCurrentServerSentEventInstanceAndRunIt();

            this.isRunning = true;
        }

        private void SetCurrentServerSentEventInstanceAndRunIt()
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            var raiseEventCallback =
                this.currentSyncContext == null ?
                    new Action<IReadOnlyCollection<EventMessage>>(this.OnDataReceived) :
                    new Action<IReadOnlyCollection<EventMessage>>(this.OnDataReceivedWithSyncContext);

            Action<string> realCallback =
                rawMessage =>
                {
                    this.messageParser.AddMessagePart(rawMessage);
                    var messagesCandidates = this.messageParser.ExtractCompleteMessageFromCache().ToList();

                    if (!messagesCandidates.Any()) return;

                    this.currentInstance.RetryDelay = messagesCandidates[messagesCandidates.Count - 1].Retry;
                    raiseEventCallback(messagesCandidates);
                };

            this.currentInstance = new ServerSentEventInstance(this.uri, this.cancellationTokenSource.Token, realCallback, logger);
            this.currentInstance
                .StartAsync()
                .ContinueWith(t =>
                    {

                    },
                    this.cancellationTokenSource.Token,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Current);
        }

        private void OnDataReceived(IReadOnlyCollection<EventMessage> data) => this.DataReceived?.Invoke(this, data);

        private void OnDataReceivedWithSyncContext(IReadOnlyCollection<EventMessage> data) =>
            this.currentSyncContext.Post(state => this.OnDataReceived((IReadOnlyCollection<EventMessage>)state), data);

        public void Stop()
        {
            this.cancellationTokenSource.Cancel(false);
            this.isRunning = false;
        }
    }
}