#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using FluentAssertions;
using Bsw.WebSocket4NetSslExt.Socket;

#endregion

namespace Bsw.WebSocket4NetSslExt.Test.Socket
{
    [TestFixture]
    public class WebSocketCustomSslTrustTest : BaseTest
    {
        private const string URI = "wss://localhost:8132";
        private WebSocketCustomSslTrust _socket;
        private Process _thinProcess;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _socket = new WebSocketCustomSslTrust(uri: URI);
            _thinProcess = null;
        }

        [TearDown]
        public override void Teardown()
        {
            if (_thinProcess != null)
            {
                _thinProcess.Kill();
            }
            base.Teardown();
        }

        private void StartRubyWebsocketServer(string extraThinOptions = "")
        {
            const string defaultThinArgs = "-R config.ru -p 8132 -V ";
            var args = defaultThinArgs + extraThinOptions;
            var scriptPath = Path.GetFullPath(@"..\..\runthinserver.bat");
            var procStart = new ProcessStartInfo
            {
                FileName = scriptPath,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = args
            };
            _thinProcess = Process.Start(procStart);
        }

        [Test]
        public void Cant_connect()
        {
            // arrange
            // (will not start the server on purpose)

            // act + assert
            _socket.Invoking(s => s.Open())
                .ShouldThrow<ConnectionException>()
                .WithMessage("Cannot connect to URI '{0}' because of XXXXX",
                    URI)
                ;
        }

        [Test]
        public void Not_ssl()
        {
            // arrange
            StartRubyWebsocketServer();

            // act + assert
            _socket.Invoking(s => s.Open())
                .ShouldThrow<ConnectionException>()
                .WithMessage("Was able to connect to URI '{0}' but no SSL");
        }

        [Test]
        public void Ssl_but_not_trusted_by_us()
        {
            // arrange
            // TODO: Generate certs we don't trust and put them here
            StartRubyWebsocketServer("--ssl --ssl-key-file ssl/rackthin.key --ssl-cert-file ssl/rackthin.crt");

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Ssl_trusted_by_us()
        {
            // arrange
            StartRubyWebsocketServer("--ssl --ssl-key-file ssl/rackthin.key --ssl-cert-file ssl/rackthin.crt");

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Ssl_expired_certificate()
        {
            // arrange
            // not sure if we can create one, but we can try and make sure this catches it

            // act

            // assert
            Assert.Fail("write test");
        }
    }
}