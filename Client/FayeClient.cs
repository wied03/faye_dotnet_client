#region

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.FayeDotNet.Transports;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

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
        private readonly ITransportClient _transportClient;
        private Advice _advice;

        private static readonly Advice DefaultAdvice = new Advice(reconnect: Reconnect.Retry,
                                                                  interval: new TimeSpan(0),
                                                                  timeout: new TimeSpan(0,
                                                                                        0,
                                                                                        60));

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private ITransportConnection _transportConnection;

        public FayeClient(IWebSocket socket) : base(messageCounter: FIRST_MESSAGE_INDEX)
        {
            _transportClient = new WebsocketTransportClient(socket);
            _advice = DefaultAdvice;
            _transportConnection = null;
        }

        public async Task<IFayeConnection> Connect()
        {
            Logger.Info("Opening up initial connection to endpoint");
            _transportConnection = await _transportClient.Connect();
            var handshakeResponse = await Handshake();
            SendConnect(handshakeResponse.ClientId,
                        _transportConnection);
            Logger.Info("Initial connection established");
            return new FayeConnection(connection: _transportConnection,
                                      handshakeResponse: handshakeResponse,
                                      messageCounter: MessageCounter,
                                      advice: _advice,
                                      handshakeTimeout: HandshakeTimeout);
        }

        private async Task<HandshakeResponseMessage> Handshake()
        {
            var message = new HandshakeRequestMessage(supportedConnectionTypes: new[] {ONLY_SUPPORTED_CONNECTION_TYPE},
                                                      id: MessageCounter++);
            HandshakeResponseMessage result;
            try
            {
                result = await ExecuteSynchronousMessage<HandshakeResponseMessage>(message,
                                                                                   HandshakeTimeout);
            }
            catch (TimeoutException)
            {
                throw new HandshakeException(HandshakeTimeout);
            }
            if (!result.Successful) throw new HandshakeException(result.Error);
            if (result.SupportedConnectionTypes.Contains(ONLY_SUPPORTED_CONNECTION_TYPE)) return result;
            var flatTypes = result
                .SupportedConnectionTypes
                .Select(ct => "'" + ct + "'")
                .Aggregate((c1,
                            c2) => c1 + "," + c2);
            var error = string.Format(CONNECTION_TYPE_ERROR_FORMAT,
                                      flatTypes);
            throw new HandshakeException(error);
        }

        private async Task<T> ExecuteSynchronousMessage<T>(BaseFayeMessage message,
                                                           TimeSpan timeoutValue) where T : BaseFayeMessage
        {
            var json = Converter.Serialize(message);
            var tcs = new TaskCompletionSource<MessageReceivedArgs>();
            MessageReceived received = (sender,
                                        args) => tcs.SetResult(args);
            _transportConnection.MessageReceived += received;
            _transportConnection.Send(json);
            var task = tcs.Task;
            var result = await task.Timeout(timeoutValue);
            if (result == Result.Timeout)
            {
                throw new TimeoutException();
            }
            var receivedString = task.Result.Message;
            Logger.Debug("Received message '{0}'",
                         receivedString);
            _transportConnection.MessageReceived -= received;
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