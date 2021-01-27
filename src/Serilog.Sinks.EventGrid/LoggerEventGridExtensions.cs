namespace Serilog.Sinks.EventGrid
{
    public static class LoggerEventGridExtensions
    {
        /// <summary>Debug log event helper for EventGrid sink</summary>
        /// <param name="logger">The Serilog ILogger</param>
        /// <param name="eventType">The event type sent to EventGrid Grid</param>
        /// <param name="subject">The event subject sent to EventGrid Grid</param>
        /// <param name="messageTemplate">The Serilog logger message template</param>
        /// <param name="props">The values references in the templace to be added to the EventGrid Grid data payload</param>
        public static void DebugEvent(this ILogger logger, string eventType, string subject, string messageTemplate = "", params object[] props)
        {
            logger = FixParams(logger, eventType, subject);

            logger.Debug(messageTemplate, props);
        }

        /// <summary>Information log event helper for EventGrid sink</summary>
        /// <param name="logger">The Serilog ILogger</param>
        /// <param name="eventType">The event type sent to EventGrid Grid</param>
        /// <param name="subject">The event subject sent to EventGrid Grid</param>
        /// <param name="messageTemplate">The Serilog logger message template</param>
        /// <param name="props">The values references in the templace to be added to the EventGrid Grid data payload</param>
        public static void InformationEvent(this ILogger logger, string eventType, string subject, string messageTemplate = "", params object[] props)
        {
            logger = FixParams(logger, eventType, subject);

            logger.Information(messageTemplate, props);
        }

        /// <summary>Warning log event helper for EventGrid sink</summary>
        /// <param name="logger">The Serilog ILogger</param>
        /// <param name="eventType">The event type sent to EventGrid Grid</param>
        /// <param name="subject">The event subject sent to EventGrid Grid</param>
        /// <param name="messageTemplate">The Serilog logger message template</param>
        /// <param name="props">The values references in the templace to be added to the EventGrid Grid data payload</param>
        public static void WarningEvent(this ILogger logger, string eventType, string subject, string messageTemplate = "", params object[] props)
        {
            logger = FixParams(logger, eventType, subject);

            logger.Warning(messageTemplate, props);
        }

        /// <summary>Error log event helper for EventGrid sink</summary>
        /// <param name="logger">The Serilog ILogger</param>
        /// <param name="eventType">The event type sent to EventGrid Grid</param>
        /// <param name="subject">The event subject sent to EventGrid Grid</param>
        /// <param name="messageTemplate">The Serilog logger message template</param>
        /// <param name="props">The values references in the templace to be added to the EventGrid Grid data payload</param>
        public static void ErrorEvent(this ILogger logger, string eventType, string subject, string messageTemplate = "", params object[] props)
        {
            logger = FixParams(logger, eventType, subject);

            logger.Error(messageTemplate, props);
        }

        /// <summary>Fatal log event helper for EventGrid sink</summary>
        /// <param name="logger">The Serilog ILogger</param>
        /// <param name="eventType">The event type sent to EventGrid Grid</param>
        /// <param name="subject">The event subject sent to EventGrid Grid</param>
        /// <param name="messageTemplate">The Serilog logger message template</param>
        /// <param name="props">The values references in the templace to be added to the EventGrid Grid data payload</param>
        public static void FatalEvent(this ILogger logger, string eventType, string subject, string messageTemplate = "", params object[] props)
        {
            logger = FixParams(logger, eventType, subject);

            logger.Fatal(messageTemplate, props);
        }

        private static ILogger FixParams(ILogger logger, string eventType, string subject)
        {
            if (!string.IsNullOrEmpty(subject))
                logger = logger.ForContext("EventSubject", subject);

            if (!string.IsNullOrEmpty(eventType))
                logger = logger.ForContext("EventType", eventType);
            return logger;
        }
    }
}
