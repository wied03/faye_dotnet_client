#region

using System;
using System.Linq;
using System.Linq.Expressions;
using Bsw.FayeDotNet.Client;
using Bsw.WebSocket4NetSslExt.Socket;
using NUnit.Framework;
using FluentAssertions;

#endregion

namespace Bsw.FayeDotNet.Test.Client
{
    [TestFixture]
    public class FayeClientTest : BaseTest
    {
        private IFayeClient _client;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            var websocket = new WebSocketClient(uri: "ws://weez.weez.wied.us:8000");
            _client = new FayeClient(websocket);
        }

        [Test]
        public void Connect_wrong_connectivity_info()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Connect_websocketopens_but_handshake_fails()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Connect_handshake_completes()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Subscribe()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Unsubscribe()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public void Publish()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }
    }
}