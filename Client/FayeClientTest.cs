#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Client;
using Bsw.FayeDotNet.Messages;
using Bsw.WebSocket4NetSslExt.Socket;
using MsbwTest;
using Newtonsoft.Json;
using NUnit.Framework;
using FluentAssertions;
using Rhino.Mocks;

#endregion

namespace Bsw.FayeDotNet.Test.Client
{
    [TestFixture]
    public class FayeClientTest : BaseTest
    {
        #region Test Fields
        private IWebSocket _websocket;
        private List<string> _messagesSent; 
        #endregion

        #region Setup/Teardown

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _messagesSent = new List<string>();
        } 

        #endregion

        #region Utility Methods
        private async Task<IFayeClient> GetFayeClient()
        {
            var client = new FayeClient(_websocket);
            await client.Connect();
            return client;
        }

        private void SetupWebSocket(IWebSocket webSocket)
        {
            _websocket = webSocket;
        }

        private string GetHandshakeResponse(bool successful = true)
        {
            var response =
                new
                {
                    channel = HandshakeRequestMessage.HANDSHAKE_MESSAGE,
                    version = HandshakeRequestMessage.BAYEUX_VERSION_1, 
                    successful
                };
            return JsonConvert.SerializeObject(response);
        } 
        #endregion

        #region Tests

        [Test]
        public void Connect_wrong_connectivity_info()
        {
            // arrange
            SetupWebSocket(new WebSocketClient(uri: "ws://foobar:8000"));

            // act + assert
            this.InvokingAsync(t => t.GetFayeClient())
                .ShouldThrow<ArgumentNullException>();
            Assert.Fail("Fix the exception type");
        }

        [Test]
        public void Connect_websocketopens_but_handshake_fails()
        {
            // arrange
            var mockSocket = new MockSocket
                             {
                                 OpenedAction = handler =>
                                                {
                                                    Thread.Sleep(100);
                                                    handler.Invoke(this,
                                                                   new EventArgs());
                                                },
                                 MessageSentAction = msg => _messagesSent.Add(msg),
                                 MessageReceiveAction = () =>
                                                        {
                                                            Thread.Sleep(100);
                                                            return GetHandshakeResponse(successful:false);
                                                        }
                             };

            SetupWebSocket(mockSocket);

            // act + assert
            this.InvokingAsync(t => t.GetFayeClient())
                .ShouldThrow<Exception>();
                //.WithMessage("Should be some exception indicating the handshake failed");

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Connect_websocketopens_but_handshake_times_out()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Connect_handshake_completes()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Connect_lost_connection_retry_happens_properly()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Subscribe()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Unsubscribe()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Publish()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        #endregion
    }
}