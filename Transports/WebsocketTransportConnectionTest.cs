#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Transports;
using Bsw.RubyExecution;
using Bsw.WebSocket4NetSslExt.Socket;
using FluentAssertions;
using MsBw.MsBwUtility.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using NLog;
using NUnit.Framework;

#endregion

namespace Bsw.FayeDotNet.Test.Transports
{
    [TestFixture]
    public class WebsocketTransportConnectionTest : BaseTest
    {
        private const string TEST_SERVER_URL = "ws://localhost:8132/bayeux";
        private const int THIN_SERVER_PORT = 8132;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly Object TestMessage =
            new {channel = "/meta/handshake", version = "1.0", supportedConnectionTypes = new[] {"websocket"}};

        private static readonly string TestMessageStr = JsonConvert.SerializeObject(TestMessage);

        #region Test Fields

        private IWebSocket _websocket;
        private ITransportClient _client;
        private ITransportConnection _connection;
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
            _websocket = null;
            _connection = null;
            _client = null;
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

        private void InstantiateTransportClient()
        {
            _client = new WebsocketTransportClient(_websocket);
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
        public async Task Connect_send_receive_and_disconnect()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateTransportClient();
            _connection = await _client.Connect();
            var tcs = new TaskCompletionSource<string>();
            _connection.MessageReceived += (sender,
                                            args) => tcs.SetResult(args.Message);
            // act
            _connection.Send(TestMessageStr);
            var response = await tcs.Task.WithTimeout(s => s,
                                                      5.Seconds());

            // assert
            dynamic responseObj = JsonConvert.DeserializeObject<JArray>(response)[0];
            bool successful = responseObj.successful;
            successful
                .Should()
                .BeTrue();
        }

        [Test]
        public async Task Connection_dies_reestablishes_before_message_send()
        {
            // arrange
            const int inputPort = THIN_SERVER_PORT + 1;
            _fayeServerProcess.StartThinServer();
            _socatInterceptor = StartWritableSocket(hostname: "localhost",
                                                    inputPort: inputPort);
            const string urlThroughSocat = "ws://localhost:8133/bayeux";
            var socket = new WebSocketClient(uri: urlThroughSocat);
            SetupWebSocket(socket);
            InstantiateTransportClient();
            _connection = await _client.Connect();
            var tcs = new TaskCompletionSource<string>();
            _connection.MessageReceived += (sender,
                                            args) => tcs.SetResult(args.Message);
            var lostTcs = new TaskCompletionSource<bool>();
            _connection.ConnectionLost += (sender,
                                           args) => lostTcs.SetResult(true);
            var backTcs = new TaskCompletionSource<bool>();
            _connection.ConnectionReestablished += (sender,
                                                    args) => backTcs.SetResult(true);
            // act
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
            _connection.Send(TestMessageStr);
            var response = await tcs.Task.WithTimeout(s => s,
                                                      5.Seconds());

            // assert
            dynamic responseObj = JsonConvert.DeserializeObject<JArray>(response)[0];
            bool successful = responseObj.successful;
            successful
                .Should()
                .BeTrue();
        }

        [Test]
        public async Task Connection_dies_reestablishes_after_message_send()
        {
            // arrange
            const int inputPort = THIN_SERVER_PORT + 1;
            _fayeServerProcess.StartThinServer();
            _socatInterceptor = StartWritableSocket(hostname: "localhost",
                                                    inputPort: inputPort);
            const string urlThroughSocat = "ws://localhost:8133/bayeux";
            var socket = new WebSocketClient(uri: urlThroughSocat);
            SetupWebSocket(socket);
            InstantiateTransportClient();
            _connection = await _client.Connect();
            var tcs = new TaskCompletionSource<string>();
            _connection.MessageReceived += (sender,
                                            args) => tcs.SetResult(args.Message);
            var lostTcs = new TaskCompletionSource<bool>();
            _connection.ConnectionLost += (sender,
                                           args) => lostTcs.SetResult(true);

            // act
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
            Logger.Info("Disconnect acknowledged, sending message");
            _connection.Send(TestMessageStr);

            // assert
            var response = await tcs.Task.WithTimeout(s => s,
                                                      15.Seconds());
            dynamic responseObj = JsonConvert.DeserializeObject<JArray>(response)[0];
            bool successful = responseObj.successful;
            successful
                .Should()
                .BeTrue();
        }

        [Test]
        public async Task Expected_server_disconnect()
        {
            // arrange
            _fayeServerProcess.StartThinServer();
            var socket = new WebSocketClient(uri: TEST_SERVER_URL);
            SetupWebSocket(socket);
            InstantiateTransportClient();
            _connection = await _client.Connect();
            var handshakeTcs = new TaskCompletionSource<string>();
            MessageReceived handshakeHandler = (sender,
                                                args) => handshakeTcs.SetResult(args.Message);
            _connection.MessageReceived += handshakeHandler;
            _connection.Send(TestMessageStr);
            var response = await handshakeTcs.Task.WithTimeout(s => s,
                                                               5.Seconds());
            dynamic responseObj = JsonConvert.DeserializeObject<JArray>(response)[0];
            string clientId = responseObj.clientId;
            var triggerServerDisconnectMessage =
                new {channel = "/meta/disconnect", clientId};
            var disconnectJson = JsonConvert.SerializeObject(triggerServerDisconnectMessage);
            var disconnectTcs = new TaskCompletionSource<string>();
            MessageReceived disconnectHandler = (sender,
                                                 args) => disconnectTcs.SetResult(args.Message);
            _connection.MessageReceived -= handshakeHandler;
            _connection.MessageReceived += disconnectHandler;
            var closedTcs = new TaskCompletionSource<bool>();
            _connection.ConnectionClosed += (sender,
                                             args) => closedTcs.SetResult(true);

            // act
            _connection.NotifyOfPendingServerDisconnection();
            _connection.Send(disconnectJson);
            await disconnectTcs.Task.WithTimeout(s => s,
                                                 10.Seconds());
            await closedTcs.Task.WithTimeout(s => s,
                                             10.Seconds());

            // assert
            _connection
                .ConnectionState
                .Should()
                .Be(ConnectionState.Disconnected);
        }

        [Test]
        public async Task Connection_lost_retry_not_enabled()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        #endregion
    }
}