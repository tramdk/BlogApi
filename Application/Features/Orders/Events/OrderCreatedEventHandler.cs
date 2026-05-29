using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using FloraCore.Domain.Entities;
using FloraCore.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using FloraCore.Application.Common.Constants;
using System;

namespace FloraCore.Application.Features.Orders.Events;

public class OrderCreatedEventHandler(
    UserManager<AppUser> userManager,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    ILogger<OrderCreatedEventHandler> logger) : INotificationHandler<OrderCreatedEvent>
{
    private readonly UserManager<AppUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly INotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<OrderCreatedEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var admins = await _userManager.GetUsersInRoleAsync(RoleConstants.Admin);

            foreach (var admin in admins)
            {
                await _notificationService.SendNotificationToUser(
                    admin.Id,
                    "Đơn hàng mới",
                    $"Một đơn hàng mới đã được tạo với ID: {notification.OrderId}",
                    "OrderCreated",
                    notification.OrderId.ToString());
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for new order.");
        }
    }
}
