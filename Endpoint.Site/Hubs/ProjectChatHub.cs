using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Endpoint.Site.Hubs
{
    public class ProjectChatHub : Hub
    {

        public async Task SendNewMessage(string Sender, string Message)
        {
            await Clients.All.SendAsync("getNewMessage", Sender, Message, DateTime.Now.ToShortDateString());
        }


        public override Task OnConnectedAsync()
        {
            var s = Context.ConnectionId;
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
