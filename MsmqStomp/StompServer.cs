using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsmqStomp
{
    class StompServer
    {
        readonly Stream connection;
        readonly FrameReader frames;
        bool _stopped;

        public StompServer(Stream connection)
        {
            this.connection = connection;
            frames = new FrameReader(connection);
        }

        public Task<Task> StartAsync() => Task.FromResult(RunAsync());

        async Task RunAsync()
        {
            await ConnectAsync();
            while (!_stopped)
            {
                var f = await frames.Parse();
                if (f == null) // connection broken
                    break;
                switch (f.Command)
                {
                    case "DISCONNECT":
                        await DisconnectAsync();
                        break;
                }
            }
            connection.Close();
        }

        private async Task ConnectAsync()
        {
            var f = await frames.Parse();
            if (f.Command == "CONNECT" || f.Command == "STOMP")
                await SendConnectedAsync(f);
            else
                await SendErrorAsync(f, "Expect CONNECT or STOMP command");
        }

        private Task SendConnectedAsync(Frame f)
        {
            throw new NotImplementedException();
        }

        private Task SendErrorAsync(Frame f, string message)
        {
            throw new NotImplementedException();
            _stopped = true;
        }

        private Task DisconnectAsync()
        {
            throw new NotImplementedException();
            _stopped = true;
        }
    }
}
