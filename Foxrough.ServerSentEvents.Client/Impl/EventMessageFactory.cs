using System;
using System.Collections.Generic;

namespace Foxrough.ServerSentEvents.Client.Impl
{
    /// <summary>
    /// Factory able to create <see cref="EventMessage"/> from the normalized ServerSentEvent format
    /// </summary>
    internal sealed class EventMessageFactory
    {
        /// <summary>
        /// Create a <see cref="EventMessage"/> from the normalized raw string
        /// </summary>
        /// <param name="message">raw string formatted as describe in https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events#Event_stream_format</param>
        public EventMessage CreateFromRawMessage(string message)
        {
            var messageFields = message.Split(ServerSentEventsConstants.MessageFieldSeparator);

            var id = string.Empty;
            var eventKey = string.Empty;
            var retryInMs = ServerSentEventsConstants.MessageField_Retry_DefaultValue; // default ServerSentEvent value
            var data = new List<string>();

            foreach (var messageField in messageFields)
            {
                if (!TryExtractMessageField(messageField, out var fieldName, out var fieldValue)) continue;

                // var notUsed exists only to use boolean OR operator to execute TryExtract...
                var notUsed =
                    TryExtractDataField(fieldName, fieldValue, data)
                    || TryExtractAsStringField(ServerSentEventsConstants.MessageField_Id, fieldName, fieldValue, ref id)
                    || TryExtractAsStringField(ServerSentEventsConstants.MessageField_Event, fieldName, fieldValue, ref eventKey)
                    || TryExtractRetryField(fieldName, fieldValue, ref retryInMs);
            }

            return new EventMessage(eventKey, id, retryInMs, data);
        }

        private static bool TryExtractDataField(string fieldName, string fieldValue, List<string> targetDataValues)
        {
            if (!ServerSentEventsConstants.MessageField_Data.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) return false;

            targetDataValues.Add(fieldValue);
            return true;
        }

        private static bool TryExtractAsStringField(string expectedFieldName, string fieldName, string fieldValue, ref string targetFieldValue)
        {
            if (!expectedFieldName.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) return false;

            targetFieldValue = fieldValue;
            return true;
        }

        private static bool TryExtractRetryField(string fieldName, string fieldValue, ref int targetRetryValue)
        {
            if (!ServerSentEventsConstants.MessageField_Retry.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) return false;

            if (!int.TryParse(fieldValue, out targetRetryValue))
                targetRetryValue = ServerSentEventsConstants.MessageField_Retry_DefaultValue;
            return true;
        }

        private static bool TryExtractMessageField(string rawValue, out string fieldName, out string fieldValue)
        {
            const char MessageFieldNameStep = ':';

            fieldName = default(string);
            fieldValue = default(string);

            var indexOfSep = rawValue.IndexOf(MessageFieldNameStep);
            if (indexOfSep < 0) return false;

            fieldName = rawValue.Substring(0, indexOfSep);
            fieldValue = rawValue.Length > indexOfSep + 1 ? rawValue.Substring(indexOfSep + 1) : string.Empty;
            return true;
        }
    }
}