using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using WFBC.Shared.Models;

namespace WFBC.Server.Hubs
{
    [Authorize(Policy = Policies.IsCommish)]
    public class ProgressHub : Hub
    {
        public async Task JoinProgressGroup(string progressId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"progress-{progressId}");
        }

        public async Task LeaveProgressGroup(string progressId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"progress-{progressId}");
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
