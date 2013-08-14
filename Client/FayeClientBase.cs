#region

using System;
using Bsw.FayeDotNet.Messages;
using Bsw.FayeDotNet.Serialization;
using Bsw.WebSocket4NetSslExt.Socket;
using MsBw.MsBwUtility.Enum;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        protected FayeClientBase(IWebSocket socket, int messageCounter)
        {
            _socket = socket;
            Converter = new FayeJsonConverter();
            MessageCounter = messageCounter;
        }

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
            var reconnect = ((string)advice.reconnect).EnumValue<Reconnect>();
            return new Advice(reconnect: reconnect,
                              interval: interval,
                              timeout: timeout);
        }
    }
}