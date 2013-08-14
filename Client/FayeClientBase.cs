#region

using System;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.FayeDotNet.Serialization;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Enum;
using MsBw.MsBwUtility.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using SuperSocket.ClientEngine;
using WebSocket4Net;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public abstract class FayeClientBase
    {
        internal const string ONLY_SUPPORTED_CONNECTION_TYPE = "websocket";
        private readonly IWebSocket _socket;
        protected readonly FayeJsonConverter Converter;
        protected int MessageCounter;
        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        // 10 seconds
        private static readonly TimeSpan DefaultHandshakeTimeout = new TimeSpan(0,
                                                                                0,
                                                                                10);

        protected FayeClientBase(IWebSocket socket,
                                 int messageCounter)
            : this(socket,
                   messageCounter,
                   DefaultHandshakeTimeout)
        {
        }

        protected FayeClientBase(IWebSocket socket,
                                 int messageCounter,
                                 TimeSpan handshakeTimeout)
        {
            _socket = socket;
            Converter = new FayeJsonConverter();
            MessageCounter = messageCounter;
            HandshakeTimeout = handshakeTimeout;
        }

        public TimeSpan HandshakeTimeout { get; set; }

        protected void SendConnect(string clientId)
        {
            var message = new ConnectRequestMessage(clientId: clientId,
                                                    connectionType: ONLY_SUPPORTED_CONNECTION_TYPE,
                                                    id: MessageCounter++);
            var json = Converter.Serialize(message);
            _socket.Send(json);
        }

        private static TimeSpan FromMilliSecondsStr(string milliseconds)
        {
            var ms = Convert.ToInt32(milliseconds);
            return new TimeSpan(0,
                                0,
                                0,
                                0,
                                ms);
        }

        protected static Advice ParseAdvice(MessageReceivedEventArgs e)
        {
            var array = JsonConvert.DeserializeObject<JArray>(e.Message);
            dynamic receivedAnonObj = array[0];
            if (receivedAnonObj.advice == null) return null;
            var advice = receivedAnonObj.advice;
            var timeout = FromMilliSecondsStr((string) advice.timeout);
            var interval = FromMilliSecondsStr((string) advice.interval);
            var reconnect = ((string) advice.reconnect).EnumValue<Reconnect>();
            return new Advice(reconnect: reconnect,
                              interval: interval,
                              timeout: timeout);
        }

        protected async Task OpenWebSocket()
        {
            Logger.Debug("Connecting to websocket");
            var tcs = new TaskCompletionSource<bool>();
            EventHandler socketOnOpened = (sender,
                                           args) => tcs.SetResult(true);
            _socket.Opened += socketOnOpened;
            Exception exception = null;
            EventHandler<ErrorEventArgs> socketOnError = (sender,
                                                          args) =>
                                                         {
                                                             exception = args.Exception;
                                                             tcs.SetResult(false);
                                                         };
            _socket.Error += socketOnError;
            _socket.Open();
            var task = tcs.Task;
            var result = await task.Timeout(HandshakeTimeout);
            try
            {
                if (result == Result.Timeout)
                {
                    var error = String.Format("Timed out, waited {0} milliseconds to connect via websockets",
                                              HandshakeTimeout.TotalMilliseconds);
                    throw new FayeConnectionException(error);
                }
                if (!task.Result)
                {
                    throw exception;
                }
            }
            finally
            {
                _socket.Error -= socketOnError;
                _socket.Opened -= socketOnOpened;
            }
            Logger.Debug("Connected to websocket");
        }
    }
}