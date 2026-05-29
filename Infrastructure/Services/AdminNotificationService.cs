using System;
using System.Threading.Tasks;
using FloraCore.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using FloraCore.Domain.Entities;
using Microsoft.Extensions.Logging;
using FloraCore.Application.Common.Constants;

namespace FloraCore.Infrastructure.Services;

public class AdminNotificationService(
    UserManager<AppUser> userManager,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    ILogger<AdminNotificationService> logger) : IAdminNotificationService
{
    private readonly UserManager<AppUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly INotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<AdminNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task SendNewOrderNotification(Guid orderId)
    {
        try
        {
            var admins = await _userManager.GetUsersInRoleAsync(RoleConstants.Admin);

            foreach (var admin in admins)
            {
                await _notificationService.SendNotificationToUser(
                    admin.Id,
                    "Đơn hàng mới",
                    $"Một đơn hàng mới đã được tạo với ID: {orderId}",
                    "OrderCreated",
                    orderId.ToString());
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending new order notification to admins.");
        }
    }
}
