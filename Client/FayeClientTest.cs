﻿#region

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
using Bsw.WebSocket4NetSslExt.Socket;
using FluentAssertions;
using MsbwTest;
using Newtonsoft.Json;
using NUnit.Framework;

#endregion

namespace Bsw.FayeDotNet.Test.Client
{
    [TestFixture]
    public class FayeClientTest : BaseTest
    {
        #region Test Fields

        private IWebSocket _websocket;
        private List<string> _messagesSent;
        private IFayeClient _fayeClient;
        private IFayeConnection _connection;
        private RubyProcess _fayeServerProcess;
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
            _messagesSent = new List<string>();
            _fayeClient = null;
            _websocket = null;
            _connection = null;
            _fayeServerProcess = new RubyProcess(thinPort: 8132,
                                                workingDirectory: WorkingDirectory);
        }

        [TearDown]
        public override void Teardown()
        {
            if (_connection != null)
            {
                _connection.Disconnect().Wait();
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
            _fayeClient = new FayeClient(_websocket);
        }

        private void SetupWebSocket(IWebSocket webSocket)
        {
            _websocket = webSocket;
        }

        private static string GetHandshakeResponse(bool successful = true)
        {
            var response =
                new
                {
                    channel = HandshakeRequestMessage.HANDSHAKE_MESSAGE,
                    version = HandshakeRequestMessage.BAYEUX_VERSION_1,
                    successful
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

            // act + assert
            var result = await _fayeClient.InvokingAsync(t => t.Connect())
                                          .ShouldThrow<SocketException>();
            result.Message
                  .Should()
                  .Be("No such host is known");
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
                                 MessageReceiveAction = () =>
                                                        {
                                                            Thread.Sleep(100);
                                                            return GetHandshakeResponse(successful: false);
                                                        }
                             };
            
            SetupWebSocket(mockSocket);
            InstantiateFayeClient();

            // act + assert
            var result = await _fayeClient.InvokingAsync(t => t.Connect())
                                          .ShouldThrow<HandshakeException>();
            result.Message
                  .Should()
                  .Be("Handshaking with server failed.  Response from server was: " + FayeClient.SUCCESSFUL_FALSE);
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
        public async Task Connect_handshake_completes_ok()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: "ws://localhost:8132/bayeux");
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