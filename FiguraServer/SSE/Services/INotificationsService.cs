using System.Threading.Tasks;

namespace FiguraServer.SSE.Services
{
    public interface INotificationsService
    {
        Task SendNotificationAsync(string notification, bool alert);
    }
}
