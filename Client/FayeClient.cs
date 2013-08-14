#region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebSocket4Net;

#endregion

namespace Bsw.FayeDotNet.Client
{
    /// <summary>
    ///     Implementation with 10 second default timeout
    /// </summary>
    public class FayeClient : FayeClientBase,
                              IFayeClient
    {
        private const int FIRST_MESSAGE_INDEX = 1;
        private readonly IWebSocket _socket;
        private Advice _advice;

        private static readonly Advice DefaultAdvice = new Advice(reconnect: Reconnect.Retry,
                                                                  interval: new TimeSpan(0),
                                                                  timeout: new TimeSpan(0,
                                                                                        0,
                                                                                        60));

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public FayeClient(IWebSocket socket) : base(socket: socket,
                                                    messageCounter: FIRST_MESSAGE_INDEX,
                                                    logger: Logger)
        {
            _socket = socket;
            _advice = DefaultAdvice;
        }

        public async Task<IFayeConnection> Connect()
        {
            Logger.Info("Opening up initial connection to endpoint");
            await OpenWebSocket();
            var handshakeResponse = await Handshake();
            SendConnect(handshakeResponse.ClientId);
            Logger.Info("Initial connection established");
            return new FayeConnection(socket: _socket,
                                      handshakeResponse: handshakeResponse,
                                      messageCounter: MessageCounter,
                                      advice: _advice,
                                      handshakeTimeout: HandshakeTimeout);
        }

        protected override async Task<T> ExecuteSynchronousMessage<T>(BaseFayeMessage message,
                                                                      TimeSpan timeoutValue)
        {
            var json = Converter.Serialize(message);
            var tcs = new TaskCompletionSource<MessageReceivedEventArgs>();
            EventHandler<MessageReceivedEventArgs> received = (sender,
                                                               args) => tcs.SetResult(args);
            _socket.MessageReceived += received;
            SocketSend(json);
            var task = tcs.Task;
            var result = await task.Timeout(timeoutValue);
            if (result == Result.Timeout)
            {
                throw new TimeoutException();
            }
            var receivedString = task.Result.Message;
            Logger.Debug("Received message '{0}'",
                         receivedString);
            _socket.MessageReceived -= received;
            var array = JsonConvert.DeserializeObject<JArray>(receivedString);
            dynamic messageObj = array[0];
            var newAdvice = ParseAdvice(messageObj);
            if (newAdvice != null)
            {
                _advice = newAdvice;
            }
            return Converter.Deserialize<T>(receivedString);
        }
    }
}