using Xunit;
using Moq;
using FloraCore.Infrastructure.Services;
using FloraCore.Application.Common.Interfaces;
using FloraCore.Domain.Entities;
using FloraCore.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FloraCore.Tests.Infrastructure.Services
{
    public class NotificationServiceTests
    {
        [Fact]
        public async Task SendNotificationToUser_ShouldCallNotificationClientSendAsync()
        {
            // Arrange
            var mockRepo = new Mock<IGenericRepository<Notification, Guid>>();
            var mockHubContext = new Mock<IHubContext<NotificationHub, INotificationClient>>();
            var mockClients = new Mock<IHubClients<INotificationClient>>();
            var mockClient = new Mock<INotificationClient>();
            var mockLogger = new Mock<ILogger<NotificationService>>();

            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(mockClient.Object);

            var notificationService = new NotificationService(mockRepo.Object, mockHubContext.Object, mockLogger.Object);
            var userId = Guid.NewGuid();
            string title = "Test Title";
            string message = "Test Message";
            string type = "Test Type";
            string relatedId = "Test RelatedId";

            // Act
            await notificationService.SendNotificationToUser(userId, title, message, type, relatedId);

            // Assert
            mockRepo.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Once);
            mockClient.Verify(x => x.ReceiveNotification(title, message, type, relatedId), Times.Once);
        }
    }
}
