using System;
using DIHub.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DIHub.Core.Services
{
    public sealed class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public event EventHandler<NotificationEventArgs>? NotificationRequested;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Show(string title, string message,
            NotificationSeverity severity = NotificationSeverity.Information)
        {
            _logger.LogInformation("[{Severity}] {Title}: {Message}",
                severity, title, message);

            NotificationRequested?.Invoke(this, new NotificationEventArgs
            {
                Title = title,
                Message = message,
                Severity = severity
            });
        }
    }
}