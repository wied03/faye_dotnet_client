#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
            var testMessage =
                new {channel = "/meta/handshake", version = "1.0", supportedConnectionTypes = new[] {"websocket"}};
            var testMessageStr = JsonConvert.SerializeObject(testMessage);

            // act
            _connection.Send(testMessageStr);
            var response = await tcs.Task.WithTimeout(s => s,
                                                      5.Seconds());

            // assert
            dynamic responseObj = JsonConvert.DeserializeObject<JArray>(response)[0];
            bool successful = responseObj.successful;
            successful
                .Should()
                .BeTrue();
        }

        #endregion
    }
}