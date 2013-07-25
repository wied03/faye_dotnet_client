#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Bsw.WebSocket4NetSslExt.Socket;
using FluentAssertions;
using NUnit.Framework;
using WebSocket4Net;

#endregion

namespace Bsw.WebSocket4NetSslExt.Test.Socket
{
    [TestFixture]
    public class WebSocketCustomSslTrustTest : BaseTest
    {
        private const string URI = "wss://localhost:8132";
        private const string TEST_MESSAGE = "hi there";
        private static readonly string BasePath = Path.GetFullPath(@"..\..\Socket\test_certs");
        private IWebSocket _socket;
        private Process _thinProcess;
        // Use Ruby.exe path to avoid batch files which cause problems with Process.Kill()
        private static readonly string RubyPath = GetRubyPath();

        private static readonly string BundlePath = Path.Combine(Path.GetDirectoryName(RubyPath),
                                                                 "bundle");

        private TaskCompletionSource<string> _messageReceivedTask;
        private List<X509Certificate> _trustedCerts;

        [TestFixtureSetUp]
        public static void FixtureSetup()
        {
            InstallBundlerDependencies();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _trustedCerts = new List<X509Certificate>();
            _socket = new WebSocketCustomSslTrust(uri: URI,
                                                  trustedCertChain: _trustedCerts);
            _thinProcess = null;
            _messageReceivedTask = new TaskCompletionSource<string>();
        }

        [TearDown]
        public override void Teardown()
        {
            if (_thinProcess != null)
            {
                ProcessUtilities.KillProcessTree(_thinProcess);
            }
            base.Teardown();
        }

        private static string GetRubyPath()
        {
            // allow overrides of path variable
            var rubyPath = Environment.GetEnvironmentVariable("RUBY_PATH");
            if (!string.IsNullOrEmpty(rubyPath))
            {
                return rubyPath;
            }
            var sysPath = Environment.GetEnvironmentVariable("PATH");
            Debug.Assert(sysPath != null,
                         "sysPath != null");
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
                                Arguments = BundlePath + " install",
                                UseShellExecute = false
                            };
            Process.Start(procStart).WaitForExit();
        }

        private static string FullCertPath(string certFile)
        {
            return Path.Combine(BasePath,
                                certFile);
        }

        private void StartRubyWebsocketServer(string keyFile = null,
                                              string certFile = null)
        {
            var args = BundlePath + " exec thin start -R config.ru -p 8132 -V ";
            if (keyFile != null)
            {
                args += string.Format("--ssl --ssl-key-file {0} --ssl-cert-file {1}",
                                      FullCertPath(keyFile),
                                      FullCertPath(certFile));
            }

            // don't want to run this inside of bin
            var pathWhereConfigIs = Path.GetFullPath(@"..\..");
            var procStart = new ProcessStartInfo
                            {
                                FileName = RubyPath,
                                Arguments = args,
                                WorkingDirectory = pathWhereConfigIs,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };
            _thinProcess = Process.Start(procStart);
            Thread.Sleep(200.Milliseconds());
        }

        private static void SocketOpened(object sender,
                                         EventArgs e)
        {
            var socket = (IWebSocket) sender;
            socket.Send(TEST_MESSAGE);
        }

        private void SocketMessageReceived(object sender,
                                           MessageReceivedEventArgs e)
        {
            var messageReceived = e.Message;
            _messageReceivedTask.SetResult(messageReceived);
        }

        private void SetupOurTrustedCertToBe(string testCertFile)
        {
            var fullPath = FullCertPath(testCertFile);
            var cert = X509Certificate.CreateFromSignedFile(fullPath);
            _trustedCerts.Add(cert);
        }

        [Test]
        public void Cant_connect()
        {
            // arrange
            // (will not start the server on purpose)

            // act + assert
            _socket.Invoking(s => s.Open())
                   .ShouldThrow<ConnectionException>()
                   .WithMessage("Unable to connect",
                                URI)
                   .WithInnerException<SocketException>()
                   .WithInnerMessage(
                                     "No connection could be made because the target machine actively refused it 127.0.0.1:8132")
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
                   .WithMessage(
                                "Was able to connect to host 'localhost' on port 8132 but SSL handshake failed.  Are you sure SSL is running?")
                   .WithInnerException<IOException>()
                   .WithInnerMessage("The handshake failed due to an unexpected packet format.")
                ;
        }

        // openssl genrsa -out not_trusted.ca.key 4096
        // openssl req -new -x509 -days 3650 -key not_trusted.ca.key -out not_trusted.ca.crt
        // openssl genrsa -out not_trusted.key 4096
        // openssl req -new -key not_trusted.key -out not_trusted.csr
        // openssl x509 -req -days 3650 -in not_trusted.csr -CA not_trusted.ca.crt -CAkey not_trusted.ca.key -set_serial 01 -out not_trusted.crt
        [Test]
        public void Ssl_but_not_trusted_by_us()
        {
            // arrange
            StartRubyWebsocketServer(keyFile: "not_trusted.key",
                                     certFile: "not_trusted.crt");
            SetupOurTrustedCertToBe("trusted.ca.crt");

            // act + assert
            _socket.Invoking(s => s.Open())
                   .ShouldThrow<SSLException>()
                   .WithMessage("The server certificate is not trusted (RemoteCertificateChainErrors)!");
        }

        [Test]
        public void Wrong_hostname()
        {
            // arrange
            SetupOurTrustedCertToBe("trusted.ca.crt");
            StartRubyWebsocketServer(keyFile: "trusted.key",
                                     certFile: "trusted_wronghost.crt");

            // act + assert
            _socket.Invoking(s => s.Open())
                   .ShouldThrow<SSLException>()
                   .WithMessage("The server certificate is not trusted (RemoteCertificateNameMismatch)!");
        }

        [Test]
        public void Ssl_trusted_by_us()
        {
            // arrange
            SetupOurTrustedCertToBe("trusted.ca.crt");
            StartRubyWebsocketServer(keyFile: "trusted.key",
                                     certFile: "trusted.crt");
            _socket.MessageReceived += SocketMessageReceived;

            // act
            _socket.Opened += SocketOpened;
            _socket.Open();

            // assert
            var withinTimeout = _messageReceivedTask.Task.Wait(2.Seconds());
            if (!withinTimeout)
            {
                Assert.Fail("Timed out waiting for response from web socket!");
            }
            _messageReceivedTask
                .Task
                .Result
                .Should()
                .Be("Received your message hi there");
        }

        [Test]
        public void Ssl_expired_certificate()
        {
            // arrange
            SetupOurTrustedCertToBe("trusted.ca.crt");
            StartRubyWebsocketServer(keyFile: "trusted.key",
                                     certFile: "trusted_expired.crt");

            // act + assert
            _socket.Invoking(s => s.Open())
                   .ShouldThrow<SSLException>()
                   .WithMessage("The server certificate is not trusted (RemoteCertificateChainErrors)!");
        }
    }
}