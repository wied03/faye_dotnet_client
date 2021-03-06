﻿// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
 #region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Client;
using Bsw.FayeDotNet.Messages;
using Bsw.RubyExecution;
using Bsw.WebSocket4Net.Wrapper.Socket;
using FluentAssertions;
using MsBw.MsBwUtility.Enum;
using MsbwTest;
using Newtonsoft.Json;
using Nito.AsyncEx;
using NUnit.Framework;

#endregion

namespace Bsw.FayeDotNet.Test.Client
{
    [TestFixture]
    public class FayeClientTest : BaseTest
    {
        private const string TEST_SERVER_URL = "ws://localhost:8132/bayeux";

        #region Test Fields

        private IWebSocket _websocket;
        private IFayeClient _fayeClient;
        private IFayeConnection _connection;
        private ThinServerProcess _fayeServerProcess;
        private static readonly string WorkingDirectory = Path.GetFullPath(@"..\..");

        #endregion

        #region Setup/Teardown

        [TestFixtureSetUp]
        public static void FixtureSetup()
        {
            RubyProcess.InstallBundlerDependencies();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _fayeClient = null;
            _websocket = null;
            _connection = null;
            _fayeServerProcess = new ThinServerProcess(thinPort: 8132,
                                                       workingDirectory: WorkingDirectory);
        }

        [TearDown]
        public override void Teardown()
        {
            if (_connection != null)
            {
                AsyncContext.Run(() => _connection.Disconnect());
            }
            if (_fayeServerProcess.Started)
            {
                _fayeServerProcess.GracefulShutdown();
            }
            base.Teardown();
        }

        #endregion

        #region Utility Methods

        private void InstantiateFayeClient()
        {
            _fayeClient = new FayeClient(socket: _websocket,
                                         connectionId: TestContext.CurrentContext.Test.Name);
            // test systems are slow, so give twice the normal amount of time
            _fayeClient.ConnectionOpenTimeout = new TimeSpan(_fayeClient.ConnectionOpenTimeout.Ticks*2);
        }

        private void SetupWebSocket(IWebSocket webSocket)
        {
            _websocket = webSocket;
        }

        private static string GetHandshakeResponse(bool successful = true, string error = null,List<string> connTypes = null)
        {
            var supportedConnectionTypes = connTypes ?? new List<string> {FayeClient.ONLY_SUPPORTED_CONNECTION_TYPE};
            var response =
                new
                {
                    channel = MetaChannels.Handshake.StringValue(),
                    version = HandshakeRequestMessage.BAYEUX_VERSION_1,
                    successful,
                    error,
                    supportedConnectionTypes
                };
            return JsonConvert.SerializeObject(new[] {response});
        }

        #endregion

        #region Tests

        [Test]
        public async Task Connect_wrong_connectivity_info()
        {
            // arrange
            SetupWebSocket(new WebSocketClient(uri: "ws://foobar:8000"));
            InstantiateFayeClient();

            // act
            try
            {
                await _fayeClient.Connect();
                // assert
                Assert.Fail("Expected at least 1 exception to be thrown");
            }
            catch (SocketException)
            {
                Assert.Pass("Got our socket exception");
            }
            catch (FayeConnectionException e)
            {
                e.Message
                 .Should().Contain("Timed out",
                                   "If we don't have a more graceful socket exception, then we should at least get a proper timeout exception");
            }
        }

        [Test]
        public async Task Connect_websocketopens_but_handshake_fails()
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
                                 MessageReceiveAction = gotThis =>
                                                        {
                                                            Thread.Sleep(100);
                                                            return GetHandshakeResponse(successful: false,
                                                                                        error: "something failed");
                                                        }
                             };
            
            SetupWebSocket(mockSocket);
            InstantiateFayeClient();

            // act + assert
            var result = await _fayeClient.InvokingAsync(t => t.Connect())
                                          .ShouldThrow<HandshakeException>();
            result.Message
                  .Should()
                  .Be("Handshaking with server failed. Reason: something failed");
        }

        [Test]
        public async Task Connect_websocketopens_but_handshake_times_out()
        {
            // arrange
            var mockSocket = new MockSocket
                             {
                                 OpenedAction = handler =>
                                                {
                                                    Thread.Sleep(100);
                                                    handler.Invoke(this,
                                                                   new EventArgs());
                                                }
                             };
            
            SetupWebSocket(mockSocket);
            InstantiateFayeClient();
            _fayeClient.HandshakeTimeout = 150.Milliseconds();

            // act
            var result = await _fayeClient.InvokingAsync(t => t.Connect())
                                          .ShouldThrow<HandshakeException>();

            // assert
            result.Message
                  .Should()
                  .Be("Timed out at 150 milliseconds waiting for server to respond to handshake request.");
        }

        [Test]
        public async Task Connect_no_common_connection_types()
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
                MessageReceiveAction = gotThisMsg =>
                {
                    Thread.Sleep(100);
                    return GetHandshakeResponse(connTypes: new List<string> {"someTypeWeDontSupport"});
                }
            };

            SetupWebSocket(mockSocket);
            InstantiateFayeClient();

            // act + assert
            var exception = await _fayeClient.InvokingAsync(c => c.Connect())
                                             .ShouldThrow<HandshakeException>();
            var expectedError = "Handshaking with server failed. Reason: " +
                                string.Format(FayeClient.CONNECTION_TYPE_ERROR_FORMAT,
                                              "'someTypeWeDontSupport'");
            exception
                .Message
                .Should()
                .Be(expectedError);
        }

        [Test]
        public async Task Connect_handshake_and_connect_complete_ok()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();

            // act
            _connection = await _fayeClient.Connect();

            // assert
            _connection
                .ClientId
                .Should()
                .NotBeNullOrEmpty();
        }

        #endregion
    }
}