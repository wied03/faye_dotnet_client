#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Bsw.WebSocket4NetSslExt.Socket;
using FluentAssertions;
using NUnit.Framework;

#endregion

namespace Bsw.WebSocket4NetSslExt.Test.Socket
{
    [TestFixture]
    public class WebSocketCustomSslTrustTest : BaseTest
    {
        private const string URI = "wss://localhost:8132";
        private IWebSocket _socket;
        private Process _thinProcess;
        // Use Ruby.exe path to avoid batch files which cause problems with Process.Kill()
        private static readonly string RubyPath = GetRubyPath();
        private static readonly string BundlePath = Path.Combine(Path.GetDirectoryName(RubyPath),
                                                                 "bundle");

        [TestFixtureSetUp]
        public static void FixtureSetup()
        {
            InstallBundlerDependencies();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _socket = new WebSocketCustomSslTrust(uri: URI,
                                                  trustedCertChain: null);
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

        private static string GetRubyPath()
        {
            var sysPath = Environment.GetEnvironmentVariable("PATH");
            return sysPath
                .Split(';')
                .Select(dir => Path.Combine(dir,
                                            "ruby.exe"))
                .First(File.Exists);
        }

        private static void InstallBundlerDependencies()
        {
            var procStart = new ProcessStartInfo
                            {
                                FileName = RubyPath,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                Arguments = BundlePath+" install",
                                UseShellExecute = false
                            };
            Process.Start(procStart).WaitForExit();
        }

        private void StartRubyWebsocketServer(string extraThinOptions = "")
        {
            const string defaultThinArgs = " exec thin start -R config.ru -p 8132 -V ";
            var args = BundlePath + defaultThinArgs + extraThinOptions;
            // don't want to run this inside of bin
            var pathWhereConfigIs = Path.GetFullPath(@"..\..");
            var procStart = new ProcessStartInfo
                            {
                                FileName = RubyPath,
                                Arguments = args,
                                WorkingDirectory = pathWhereConfigIs,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden
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