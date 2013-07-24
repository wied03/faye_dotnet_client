﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
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
        private const string TEST_MESSAGE = "hi there";
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

        static void SocketOpened(object sender, EventArgs e)
        {
            var socket = (IWebSocket)sender;
            socket.Send(TEST_MESSAGE);
        }

        void SocketMessageReceived(object sender, WebSocket4Net.MessageReceivedEventArgs e)
        {
            var messageReceived = e.Message;
            _messageReceivedTask.SetResult(messageReceived);
        }

        private void AddTrustedCert(string testCertFile)
        {
            var basePath = Path.GetFullPath(@"..\..\Socket\test_certs");
            var fullPath = Path.Combine(basePath,
                                        testCertFile);
            var cert = new X509Certificate(fullPath);
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

        // openssl genrsa -out not_trusted.ca.key 4096
        // openssl req -new -x509 -days 3650 -key not_trusted.ca.key -out not_trusted.ca.crt
        // openssl genrsa -out not_trusted.key 4096
        // openssl req -new -key not_trusted.key -out not_trusted.csr
        // openssl x509 -req -days 3650 -in not_trusted.csr -CA not_trusted.ca.crt -CAkey not_trusted.ca.key -set_serial 01 -out not_trusted.crt
        [Test]
        public void Ssl_but_not_trusted_by_us()
        {
            // arrange
            AddTrustedCert("trusted.ca.crt");
            StartRubyWebsocketServer("--ssl --ssl-key-file Sockets/test_certs/not_trusted.key --ssl-cert-file Sockets/test_certs/not_trusted.crt");

            // act + assert
            _socket.Invoking(s => s.Open())
                   .ShouldThrow<SSLException>()
                   .WithMessage("The certificate XXX is not part of the trusted list of certificates");
        }

        [Test]
        public void Wrong_hostname()
        {
            // arrange
            AddTrustedCert("trusted.ca.crt");
            StartRubyWebsocketServer("--ssl --ssl-key-file Sockets/test_certs/trusted.key --ssl-cert-file Sockets/test_certs/trusted_wronghost.crt");

            // act + assert
            _socket.Invoking(s => s.Open())
                   .ShouldThrow<SSLException>()
                   .WithMessage("The certificate XXX is not part of the trusted list of certificates");
        }

        [Test]
        public void Ssl_trusted_by_us()
        {
            // arrange
            AddTrustedCert("trusted.ca.crt");
            StartRubyWebsocketServer("--ssl --ssl-key-file Sockets/test_certs/trusted.key --ssl-cert-file Sockets/test_certs/trusted.crt");
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
            AddTrustedCert("trusted.ca.crt");
            StartRubyWebsocketServer("--ssl --ssl-key-file Sockets/test_certs/trusted.key --ssl-cert-file Sockets/test_certs/trusted_expired.crt");

            // act + assert
            _socket.Invoking(s => s.Open())
                   .ShouldThrow<SSLException>()
                   .WithMessage("The certificate XXX is expired");
        }
    }
}