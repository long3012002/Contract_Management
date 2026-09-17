using System;
using demo1.Entity;

namespace demo1.Services.Helpers
{
    public class NotificationBuilder
    {
        private readonly Notification _notification = new();

        public static NotificationBuilder Create() => new();

        public NotificationBuilder WithTitle(string title)
        {
            _notification.Title = title;
            return this;
        }

        public NotificationBuilder WithContent(string content)
        {
            _notification.Content = content;
            return this;
        }

        public NotificationBuilder WithLink(string? link)
        {
            _notification.Link = link;
            return this;
        }

        public NotificationBuilder WithFeatureCode(string featureCode)
        {
            _notification.FeatureCode = featureCode;
            return this;
        }

        public NotificationBuilder WithEntity(string? entityName, string? entityId)
        {
            _notification.EntityName = entityName;
            _notification.EntityId = entityId;
            return this;
        }

        public NotificationBuilder ForUser(Guid? userId)
        {
            _notification.UserId = userId;
            return this;
        }

        public NotificationBuilder WithActor(string? actorName)
        {
            _notification.ActorName = actorName;
            return this;
        }

        public NotificationBuilder WithBadge(string? text, string? variant = "info")
        {
            _notification.ActionBadgeText = text;
            _notification.ActionBadgeVariant = variant ?? "info";
            return this;
        }

        public NotificationBuilder WithTarget(string? targetName)
        {
            _notification.TargetName = targetName;
            return this;
        }

        public Notification Build()
        {
            return _notification;
        }
    }
}
