using System;
using System.Threading.Tasks;

namespace FloraCore.Application.Common.Interfaces;

public interface IAdminNotificationService
{
    Task SendNewOrderNotification(Guid orderId);
}
