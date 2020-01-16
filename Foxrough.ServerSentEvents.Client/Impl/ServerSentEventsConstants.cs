namespace Foxrough.ServerSentEvents.Client.Impl
{
    /// <summary>
    /// Normalized ServerSentEvents constants (as describe in https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events#Event_stream_format)
    /// </summary>C:\BDV\github\Foxrough.ServerSentEvents.Client\Impl\ServerSentEventsConstants.cs
    internal static class ServerSentEventsConstants
    {
        /// <summary>
        /// In a message, this value separate fields 
        /// </summary>
        public static readonly char[] MessageFieldSeparator = new[] { '\n' };

        /// <summary>
        /// This value separate messages
        /// </summary>
        public static readonly string MessageSeparator = "\n\n";

        /// <summary>
        /// Field key for message data
        /// </summary>
        public const string MessageField_Data = "data";

        /// <summary>
        /// Field key for message id
        /// </summary>
        public const string MessageField_Id = "id";

        /// <summary>
        /// Field key for message event id
        /// </summary>
        public const string MessageField_Event = "event";

        /// <summary>
        /// Field key for message retry value
        /// </summary>
        public const string MessageField_Retry = "retry";

        /// <summary>
        /// Default value for retry (in milliseconds)
        /// </summary>
        public const int MessageField_Retry_DefaultValue = 3000; // Milliseconds
    }
}