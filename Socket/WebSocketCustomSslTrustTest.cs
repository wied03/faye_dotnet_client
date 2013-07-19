#region

using System;
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
        private WebSocketCustomSslTrust _socket;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _socket = new WebSocketCustomSslTrust(uri: "localhost");
        }

        [Test]
        public void Cant_connect()
        {
            // arrange

            // act + assert
            _socket.Invoking(s => s.Open())
                .ShouldThrow<ConnectionException>()
                ;
           Assert.Fail("finish test");
        }

        [Test]
        public void Not_ssl()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Ssl_but_not_trusted_by_us()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Ssl_trusted_by_us()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }
    }
}