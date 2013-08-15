#region

using System;
using System.Linq;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Messages;
using Bsw.FayeDotNet.Serialization;
using Bsw.FayeDotNet.Transports;
using MsBw.MsBwUtility.Enum;

#endregion

namespace Bsw.FayeDotNet.Client
{
    public abstract class FayeClientBase
    {
        internal const string ONLY_SUPPORTED_CONNECTION_TYPE = "websocket";

        internal const string CONNECTION_TYPE_ERROR_FORMAT =
            "We only support 'websocket' and the server only supports [{0}] so we cannot communicate";

        protected readonly FayeJsonConverter Converter;
        protected int MessageCounter;

        // 10 seconds
        private static readonly TimeSpan DefaultHandshakeTimeout = new TimeSpan(0,
                                                                                0,
                                                                                10);

        protected FayeClientBase(int messageCounter)
            : this(messageCounter,
                   DefaultHandshakeTimeout)
        {
        }

        protected FayeClientBase(int messageCounter,
                                 TimeSpan handshakeTimeout)
        {
            Converter = new FayeJsonConverter();
            MessageCounter = messageCounter;
            HandshakeTimeout = handshakeTimeout;
        }

        public TimeSpan HandshakeTimeout { get; set; }

        protected void SendConnect(string clientId,
                                   ITransportConnection connection)
        {
            var message = new ConnectRequestMessage(clientId: clientId,
                                                    connectionType: ONLY_SUPPORTED_CONNECTION_TYPE,
                                                    id: MessageCounter++);
            var json = Converter.Serialize(message);
            connection.Send(json);
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

        internal async Task<HandshakeResponseMessage> Handshake()
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

        protected abstract Task<T> ExecuteSynchronousMessage<T>(BaseFayeMessage message,
                                                                TimeSpan timeoutValue) where T : BaseFayeMessage;

        protected static Advice ParseAdvice(dynamic message)
        {
            if (message.advice == null) return null;
            var advice = message.advice;
            var timeout = FromMilliSecondsStr((string) advice.timeout);
            var interval = FromMilliSecondsStr((string) advice.interval);
            var reconnect = ((string) advice.reconnect).EnumValue<Reconnect>();
            return new Advice(reconnect: reconnect,
                              interval: interval,
                              timeout: timeout);
        }
    }
}