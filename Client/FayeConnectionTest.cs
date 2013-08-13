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
            const string channelName = "/somechannel";
            var messageToSend = new TestMsg { Stuff = "the message" };

            // act
            try
            {
                await _connection.Subscribe(channelName,
                                            tcs.SetResult);
                var json = JsonConvert.SerializeObject(messageToSend);
                await secondConnection.Publish(channel: channelName,
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
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            const string wildcardChannel = "/*";
            Action<string> dummyAction = msg => Console.WriteLine("hi");

            // act + assert
            var exception = await _connection.InvokingAsync(c => c.Subscribe(wildcardChannel,
                                                                             dummyAction))
                                             .ShouldThrow<SubscriptionException>();
            var expectedMessage = string.Format(FayeConnection.WILDCARD_CHANNEL_ERROR_FORMAT,
                                                wildcardChannel);
            exception.Message
                     .Should()
                     .Be(expectedMessage);
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
            var messageToSendObj = new TestMsg { Stuff = "the message" };
            var messageToSend = JsonConvert.SerializeObject(messageToSendObj);

            // act + assert
            var exception = await _connection.InvokingAsync(c => c.Publish(channel: invalidChannel,
                                                                           message: messageToSend))
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
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            const string channelWeDidntSubscribeTo = "/foobar";

            // act + assert
            var exception = await _connection.InvokingAsync(c => c.Unsubscribe(channelWeDidntSubscribeTo))
                                             .ShouldThrow<SubscriptionException>();
            var expectedError = string.Format(FayeConnection.NOT_SUBSCRIBED,
                                              channelWeDidntSubscribeTo);
            exception.Message
                     .Should()
                     .Be(expectedError);
        }

        [Test]
        public async Task Unsubscribe_wildcard()
        {
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            const string wildcardChannel = "/*";

            // act + assert
            var exception = await _connection.InvokingAsync(c => c.Unsubscribe(wildcardChannel))
                                             .ShouldThrow<SubscriptionException>();
            var expectedMessage = string.Format(FayeConnection.WILDCARD_CHANNEL_ERROR_FORMAT,
                                                wildcardChannel);
            exception.Message
                     .Should()
                     .Be(expectedMessage);
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
            const string channelName = "/somechannel";
            var tcs = new TaskCompletionSource<object>();
            var messageToSendObj = new TestMsg { Stuff = "the message" };
            var messageToSend = JsonConvert.SerializeObject(messageToSendObj);

            try
            {
                await _connection.Subscribe(channelName,
                                            tcs.SetResult); // should never hit this

                // act
                await _connection.Unsubscribe(channelName);

                // assert
                await secondConnection.Publish(channelName,
                                               messageToSend);
                await Task.Delay(100.Milliseconds());
                tcs.Task
                   .Status
                   .Should()
                   .Be(TaskStatus.WaitingForActivation,
                       "We should never fire our event since we unsubscribe before the 1st message");
            }
            finally
            {
                secondConnection.Disconnect().Wait();
            }
        }

        #endregion
    }
}