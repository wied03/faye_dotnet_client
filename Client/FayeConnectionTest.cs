#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Client;
using Bsw.RubyExecution;
using Bsw.WebSocket4NetSslExt.Socket;
using FluentAssertions;
using MsBw.MsBwUtility.Tasks;
using MsbwTest;
using NUnit.Framework;

#endregion

namespace Bsw.FayeDotNet.Test.Client
{
    [TestFixture]
    public class FayeConnectionTest : BaseTest
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

        #endregion

        #region Tests

        [Test]
        public async Task Disconnect()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: "ws://localhost:8132/bayeux");
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();

            // act
            await _connection.Disconnect();

            // assert
            try
            {
                var ex = await _connection.InvokingAsync(c => c.Disconnect())
                                          .ShouldThrow<FayeConnectionException>();
                ex.Message
                  .Should()
                  .Be(FayeConnection.ALREADY_DISCONNECTED);
            }
            finally
            {
                // teardown will break if we try and disconnect again
                _connection = null;
            }
        }

        [Test]
        public async Task Connect_lost_connection_retry_happens_properly()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public async Task Subscribe_and_publish()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: "ws://localhost:8132/bayeux");
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            var secondClient = new FayeClient(new WebSocketClient(uri: "ws://localhost:8132/bayeux"));
            var secondConnection = await secondClient.Connect();
            var tcs = new TaskCompletionSource<dynamic>();

            // act
            try
            {
                await _connection.Subscribe("/somechannel",
                                            tcs.SetResult);
                var messageToSend = new {stuff = "the message"};
                secondConnection.Publish("/somechannel",
                                         messageToSend);
                // assert
                var task = tcs.Task;
                var result = await task.Timeout(5.Seconds());
                if (result == Result.Timeout)
                {
                    Assert.Fail("Timed out waiting for pub/sub to work");
                }
                var messageReceived = task.Result;
                AssertionExtensions.ShouldBeEquivalentTo(messageReceived,
                                                         messageToSend);
            }
            finally
            {
                secondConnection.Disconnect().Wait();
            }
        }

        [Test]
        public async Task Unsubscribe()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: "ws://localhost:8132/bayeux");
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            var secondClient = new FayeClient(new WebSocketClient(uri: "ws://localhost:8132/bayeux"));
            var secondConnection = await secondClient.Connect();
            try
            {
                var tcs = new TaskCompletionSource<object>();

                await _connection.Subscribe("/somechannel",
                                            tcs.SetResult);

                // act
                await _connection.Unsubscribe("/somechannel");

                // assert
                secondConnection.Publish("/somechannel",
                                         "foobar");
                await Task.Delay(100.Milliseconds());
                tcs.Should().BeNull();
            }
            finally
            {
                secondConnection.Disconnect().Wait();
            }
        }

        #endregion
    }
}