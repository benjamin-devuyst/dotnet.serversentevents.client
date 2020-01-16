using System.Collections.Generic;
using System.Text;

namespace Foxrough.ServerSentEvents.Client.Impl
{
    /// <summary>
    /// This strategy catch messages parts and is able to isolate full messages and returns it on demand.
    /// </summary>
    internal sealed class EventMessagesParsingStrategy
    {
        private static readonly int MessageSeparatorLength = ServerSentEventsConstants.MessageSeparator.Length;

        private readonly EventMessageFactory messageFactory = new EventMessageFactory();

        private readonly StringBuilder cache = new StringBuilder(string.Empty);

        /// <summary>
        /// Add a string that is a part of a message
        /// </summary>
        /// <param name="part"></param>
        public void AddMessagePart(string part)
            => cache.Append(part);

        /// <summary>
        /// Try to extract <see cref="EventMessage"/> from the cache
        /// </summary>
        public IEnumerable<EventMessage> ExtractCompleteMessageFromCache()
        {
            while (TryExtractOneMessage(out var messageResult))
            {
                if (messageResult != null)
                    yield return messageResult;
            }
        }

        private bool TryExtractOneMessage(out EventMessage result)
        {
            var currentCacheState = cache.ToString();

            var indexOfSeparator = currentCacheState.IndexOf(ServerSentEventsConstants.MessageSeparator);
            if (indexOfSeparator < 0)
            {
                result = default;
                return false;
            }

            var lengthToRemove = indexOfSeparator + MessageSeparatorLength;

            result = messageFactory.CreateFromRawMessage(currentCacheState.Substring(0, lengthToRemove));
            cache.Remove(0, lengthToRemove);

            return true;
        }
    }
}