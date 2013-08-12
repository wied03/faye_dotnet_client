#region

using System;
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
using Newtonsoft.Json;
using NUnit.Framework;

#endregion

namespace Bsw.FayeDotNet.Test.Client
{
    [TestFixture]
    public class FayeConnectionTest : BaseTest
    {
        private const string TEST_SERVER_URL = "ws://localhost:8132/bayeux";

        #region Test Fields

        private IWebSocket _websocket;
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
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
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

        private class TestMsg
        {
            public string Stuff { get; set; }
        }

        [Test]
        public async Task Subscribe_and_publish()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            var secondClient = new FayeClient(new WebSocketClient(uri: TEST_SERVER_URL));
            var secondConnection = await secondClient.Connect();
            var tcs = new TaskCompletionSource<string>();

            // act
            try
            {
                await _connection.Subscribe("/somechannel",
                                            tcs.SetResult);
                var messageToSend = new TestMsg {Stuff = "the message"};
                var json = JsonConvert.SerializeObject(messageToSend);
                await secondConnection.Publish(channel: "/somechannel",
                                               message: json);
                // assert
                var task = tcs.Task;
                var result = await task.Timeout(5.Seconds());
                if (result == Result.Timeout)
                {
                    Assert.Fail("Timed out waiting for pub/sub to work");
                }
                var jsonReceived = task.Result;
                var objectReceived = JsonConvert.DeserializeObject<TestMsg>(jsonReceived);
                objectReceived
                    .ShouldBeEquivalentTo(messageToSend);
            }
            finally
            {
                secondConnection.Disconnect().Wait();
            }
        }

        [Test]
        public async Task Subscribe_wildcard_channel()
        {
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            var secondClient = new FayeClient(new WebSocketClient(uri: TEST_SERVER_URL));
            var secondConnection = await secondClient.Connect();
            var tcs = new TaskCompletionSource<string>();

            // act
            try
            {
                await _connection.Subscribe("/*",
                                            tcs.SetResult);
                var messageToSend = new TestMsg { Stuff = "the message" };
                var json = JsonConvert.SerializeObject(messageToSend);
                await secondConnection.Publish(channel: "/somechannel",
                                               message: json);
                // assert
                var task = tcs.Task;
                var result = await task.Timeout(5.Seconds());
                if (result == Result.Timeout)
                {
                    Assert.Fail("Timed out waiting for pub/sub to work");
                }
                var jsonReceived = task.Result;
                var objectReceived = JsonConvert.DeserializeObject<TestMsg>(jsonReceived);
                objectReceived
                    .ShouldBeEquivalentTo(messageToSend);
            }
            finally
            {
                secondConnection.Disconnect().Wait();
            }
        }

        [Test]
        public async Task Publish_invalid_channel()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            const string invalidChannel = "/*";
            const string throwawayMessage = "{\"foobar\":\"stuff\"}";

            // act + assert
            var exception = await _connection.InvokingAsync(c => c.Publish(channel: invalidChannel,
                                                                           message: throwawayMessage))
                                             .ShouldThrow<PublishException>();
            exception.Message
                     .Should()
                     .Be("405:/*:Invalid channel");
        }

        [Test]
        public async Task Subscribe_reserved_channel_name()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            const string reservedChannelName = "/meta/subscribe";
            Action<string> dummyAction = msg => Console.WriteLine("hi");

            // act + assert
            var exception = await _connection.InvokingAsync(s => s.Subscribe(channel: reservedChannelName,
                                                                             messageReceived: dummyAction))
                                             .ShouldThrow<SubscriptionException>();
            exception.Message
                     .Should()
                     .Be("403:/meta/subscribe:Forbidden channel");
        }

        [Test]
        public async Task Unsubscribe_not_currently_subscribed()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public async Task Unsubscribe()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            var secondClient = new FayeClient(new WebSocketClient(uri: TEST_SERVER_URL));
            var secondConnection = await secondClient.Connect();
            try
            {
                var tcs = new TaskCompletionSource<object>();

                await _connection.Subscribe("/somechannel",
                                            tcs.SetResult);

                // act
                await _connection.Unsubscribe("/somechannel");

                // assert
                await secondConnection.Publish("/somechannel",
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