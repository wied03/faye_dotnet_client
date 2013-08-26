// Copyright 2013 BSW Technology Consulting, released under the BSD license - see LICENSING.txt at the top of this repository for details
﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using WebSocket4Net;

#endregion

namespace Bsw.WebSocket4NetSslExt.Socket
{
    public class WebSocketCustomSslTrust : WebSocket,
                                           IWebSocket
    {
        private readonly IEnumerable<X509Certificate> _trustedCertChain;
        private readonly string _uri;
        private SslPolicyErrors _sslPolicyErrors;

        /// <summary>
        ///     Constructs a websocket that verifies the server's certificate is signed by someone we explicitly trust
        ///     before we open the websocket
        /// </summary>
        /// <param name="uri">URI to connect to</param>
        /// <param name="trustedCertChain">
        ///     List of certificates that should be trusted.  If any certificate in the server's chain
        ///     matches these certs, the verification will pass.  Otherwise an SSLException is thrown
        ///     when Open() is called.
        /// </param>
        public WebSocketCustomSslTrust(string uri,
                                       IEnumerable<X509Certificate> trustedCertChain) : base(uri)
        {
            _uri = uri;
            _trustedCertChain = trustedCertChain;
        }

        public new void Open()
        {
            TriggerSslValidation();
            base.Open();
        }

        private void TriggerSslValidation()
        {
            var uri = new Uri(_uri);
            var tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect(hostname: uri.Host,
                                  port: uri.Port);
                var sslStream = new SslStream(innerStream: tcpClient.GetStream(),
                                              leaveInnerStreamOpen: false,
                                              userCertificateValidationCallback: VerifyServersCert);
                try
                {
                    sslStream.AuthenticateAsClient(uri.Host);
                    // if we make it past this point, then we can trust the other side.  We close
                    // the connection below and let the real WebSocket class do the work
                }
                catch (AuthenticationException authenticationException)
                {
                    throw new SSLException(_sslPolicyErrors,
                                           authenticationException);
                }
                catch (IOException ioException)
                {
                    if (ioException.HResult != -2146232800) throw;
                    var error =
                        string.Format(
                                      "Was able to connect to host '{0}' on port {1} but SSL handshake failed.  Are you sure SSL is running?",
                                      uri.Host,
                                      uri.Port);
                    throw new ConnectionException(error,
                                                  ioException);
                }
                finally
                {
                    sslStream.Close();
                }
            }
            catch (SocketException e)
            {
                throw new ConnectionException("Unable to connect",
                                              e);
            }
            finally
            {
                tcpClient.Close();
            }
        }

        private bool VerifyServersCert(object sender,
                                       X509Certificate certificate,
                                       X509Chain chain,
                                       SslPolicyErrors sslPolicyErrors)
        {
            _sslPolicyErrors = sslPolicyErrors;
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                return false;
            }
            var actualCertChain = chain
                .ChainElements
                .Cast<X509ChainElement>()
                .Select(element => element.Certificate);

            return actualCertChain
                .Intersect(_trustedCertChain)
                .Any();
        }
    }
}