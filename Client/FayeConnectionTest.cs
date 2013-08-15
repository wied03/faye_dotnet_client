#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Client;
using Bsw.RubyExecution;
using Bsw.WebSocket4NetSslExt.Socket;
using FluentAssertions;
using MsBw.MsBwUtility.JetBrains.Annotations;
using MsBw.MsBwUtility.Tasks;
using MsbwTest;
using Newtonsoft.Json;
using Nito.AsyncEx;
using NLog;
using NUnit.Framework;

#endregion

namespace Bsw.FayeDotNet.Test.Client
{
    [TestFixture]
    public class FayeConnectionTest : BaseTest
    {
        private const string TEST_SERVER_URL = "ws://localhost:8132/bayeux";
        private const int THIN_SERVER_PORT = 8132;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region Test Fields

        private IWebSocket _websocket;
        private IFayeClient _fayeClient;
        private IFayeConnection _connection;
        private RubyProcess _fayeServerProcess;
        private static readonly string WorkingDirectory = Path.GetFullPath(@"..\..");
        private Process _socatInterceptor;

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
            _fayeServerProcess = new RubyProcess(thinPort: THIN_SERVER_PORT,
                                                 workingDirectory: WorkingDirectory);
            _socatInterceptor = null;
        }

        [TearDown]
        public override void Teardown()
        {
            try
            {
                if (_connection != null)
                {
                    AsyncContext.Run(() => _connection.Disconnect());
                }
            }
            finally
            {
                if (_fayeServerProcess.Started)
                {
                    _fayeServerProcess.GracefulShutdown();
                }
                if (_socatInterceptor != null && !_socatInterceptor.HasExited)
                {
                    _socatInterceptor.Kill();
                }
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

        private static Process StartWritableSocket(string hostname,
                                                   int inputPort)
        {
            return StartSocat("TCP4-LISTEN:{0} TCP4:{1}:{2}",
                              inputPort,
                              hostname,
                              THIN_SERVER_PORT);
        }

        private static Process StartSocat(string argsFormat,
                                          params object[] args)
        {
            var arguments = string.Format(argsFormat,
                                          args);
            var path = Path.GetFullPath("../../../lib/socat-1.7.2.1/socat.exe");
            var procStart = new ProcessStartInfo
                            {
                                FileName = path,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                Arguments = arguments
                            };
            var process = Process.Start(procStart);
            return process;
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
        public async Task Connect_lost_connection_comes_back_before_publish()
        {
            // port 8133
            const int inputPort = THIN_SERVER_PORT + 1;
            _fayeServerProcess.StartThinServer();
            _socatInterceptor = StartWritableSocket(hostname: "localhost",
                                                    inputPort: inputPort);
            const string urlThroughSocat = "ws://localhost:8133/bayeux";
            var socket = new WebSocketClient(uri: urlThroughSocat);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            var tcs = new TaskCompletionSource<string>();
            const string channelName = "/somechannel";
            var messageToSend = new TestMsg {Stuff = "the message"};

            // act

            await _connection.Subscribe(channelName,
                                        tcs.SetResult);
            var json = JsonConvert.SerializeObject(messageToSend);
            var lostTcs = new TaskCompletionSource<bool>();
            _connection.ConnectionLost += (sender,
                                           args) => lostTcs.SetResult(true);
            var backTcs = new TaskCompletionSource<bool>();
            _connection.ConnectionReestablished += (sender,
                                                    args) => backTcs.SetResult(true);
// ReSharper disable once CSharpWarnings::CS4014
            Task.Factory.StartNew(() =>
                                  {
                                      _socatInterceptor.Kill();
                                      _socatInterceptor = StartWritableSocket(hostname: "localhost",
                                                                              inputPort: inputPort);
                                  });
            await lostTcs.Task.WithTimeout(t => t,
                                           20.Seconds());
            await backTcs.Task.WithTimeout(t => t,
                                           20.Seconds());
            await _connection.Publish(channel: channelName,
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

        [Test]
        public async Task Connect_lost_connection_comes_back_after_attempt_to_publish()
        {
            // arrange// port 8133
            const int inputPort = THIN_SERVER_PORT + 1;
            _fayeServerProcess.StartThinServer();
            _socatInterceptor = StartWritableSocket(hostname: "localhost",
                                                    inputPort: inputPort);
            const string urlThroughSocat = "ws://localhost:8133/bayeux";
            var socket = new WebSocketClient(uri: urlThroughSocat);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            var tcs = new TaskCompletionSource<string>();
            const string channelName = "/somechannel";
            var messageToSend = new TestMsg { Stuff = "the message" };

            // act
            await _connection.Subscribe(channelName,
                                        tcs.SetResult);
            var json = JsonConvert.SerializeObject(messageToSend);
            var lostTcs = new TaskCompletionSource<bool>();
            _connection.ConnectionLost += (sender,
                                           args) => lostTcs.SetResult(true);
            // ReSharper disable once CSharpWarnings::CS4014
            Task.Factory.StartNew(() =>
            {
                Logger.Info("Killing socat");
                _socatInterceptor.Kill();
                Logger.Info("Sleeping");
                Thread.Sleep(5.Seconds());
                Logger.Info("Restarting socat");
                _socatInterceptor = StartWritableSocket(hostname: "localhost",
                                                        inputPort: inputPort);
            });
            Logger.Info("Waiting for websocket to acknowledge disconnect");
            await lostTcs.Task.WithTimeout(t => t,
                                           20.Seconds());
            Logger.Info("Disconnect acknowledged, publishing");
            await _connection.Publish(channel: channelName,
                                      message: json);
            // assert
            var task = tcs.Task;
            var result = await task.Timeout(20.Seconds());
            if (result == Result.Timeout)
            {
                Assert.Fail("Timed out waiting for pub/sub to work");
            }
            var jsonReceived = task.Result;
            var objectReceived = JsonConvert.DeserializeObject<TestMsg>(jsonReceived);
            objectReceived
                .ShouldBeEquivalentTo(messageToSend);

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public async Task Connect_lost_connection_retry_not_allowed()
        {
            // arrange
            // make sure the server returns advice reconnect none

            // act

            // assert
            Assert.Fail("write test");
        }

        private class TestMsg
        {
            [UsedImplicitly]
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
            var messageToSend = new TestMsg {Stuff = "the message"};

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
        public async Task Publish_to_channel_we_subscribed_to()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateFayeClient();
            _connection = await _fayeClient.Connect();
            var tcs = new TaskCompletionSource<string>();
            const string channelName = "/somechannel";
            var messageToSend = new TestMsg {Stuff = "the message"};

            // act
            await _connection.Subscribe(channelName,
                                        tcs.SetResult);
            var json = JsonConvert.SerializeObject(messageToSend);
            await _connection.Publish(channel: channelName,
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

        [Test]
        public async Task Subscribe_twice()
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
            var tcs2 = new TaskCompletionSource<string>();
            const string channelName = "/somechannel";
            var messageToSend = new TestMsg {Stuff = "the message"};

            // act
            try
            {
                await _connection.Subscribe(channelName,
                                            tcs.SetResult);
                await _connection.Subscribe(channelName,
                                            tcs2.SetResult);
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
                task = tcs2.Task;
                result = await task.Timeout(5.Seconds());
                if (result == Result.Timeout)
                {
                    Assert.Fail("Timed out waiting for pub/sub to work");
                }
                jsonReceived = task.Result;
                objectReceived = JsonConvert.DeserializeObject<TestMsg>(jsonReceived);
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
            var messageToSendObj = new TestMsg {Stuff = "the message"};
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
                                                                             messageReceivedAction: dummyAction))
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
            var messageToSendObj = new TestMsg {Stuff = "the message"};
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