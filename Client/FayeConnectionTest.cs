#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bsw.FayeDotNet.Client;
using Bsw.WebSocket4NetSslExt.Socket;
using NUnit.Framework;
using FluentAssertions;

#endregion

namespace Bsw.FayeDotNet.Test.Client
{
    [TestFixture]
    public class FayeConnectionTest : BaseTest
    {
        #region Test Fields
        private IWebSocket _websocket;
        private List<string> _messagesSent;
        private IFayeClient _fayeClient;
        private IFayeConnection _connection;
        #endregion

        #region Setup/Teardown

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _messagesSent = new List<string>();
            _fayeClient = null;
            _websocket = null;
            _connection = null;
        }

        [TearDown]
        public override void Teardown()
        {
            if (_connection != null)
            {
                _connection.Disconnect().Wait();
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

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public async Task Connect_lost_connection_retry_happens_properly()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public async Task Subscribe()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public async Task Unsubscribe()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }

        [Test]
        public async Task Publish()
        {
            // arrange

            // act

            // assert
            Assert.Fail("write test");
        }
 
        #endregion
    }
}