using System.Collections.Generic;

namespace Foxrough.ServerSentEvents.Client
{
    /// <summary>
    /// Readonly DTO that match ServerSentEvent Message 
    /// </summary>
    public sealed class EventMessage
    {
        public EventMessage(string eventKey, string id, int retry, IEnumerable<string> data)
        {
            this.Event = eventKey;
            this.Id = id;
            this.Retry = retry;
            this.Data = data;
        }

        public string Event { get; }

        public string Id { get; }

        public IEnumerable<string> Data { get; }

        public int Retry { get; }
    }
}