using System;

namespace DIHub.Core.Interfaces
{
    public interface INotificationService
    {
        void Show(string title, string message,
            NotificationSeverity severity = NotificationSeverity.Information);

        event EventHandler<NotificationEventArgs>? NotificationRequested;
    }

    public enum NotificationSeverity { Information, Success, Warning, Error }

    public sealed class NotificationEventArgs : EventArgs
    {
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public NotificationSeverity Severity { get; init; }
    }
}