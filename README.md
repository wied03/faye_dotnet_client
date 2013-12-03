faye_dotnet_client
==================

A .NET client for the Bayeux 1.0 protocol (tested with the FAYE server for Ruby).  Currently only supports the WebSocket4Net web socket implementation but could be enhanced later for long polling, etc.

Nuget package is available at https://www.nuget.org/packages/Bsw.FayeDotNet/

Development environment setup:

Add the websocket SSL development certificate to the trusted root CA list
Right click solution\test\Bsw.WebSocket4Net.Wrapper.Test\Socket\test_certs\trusted.ca.crt and choose Install Certificate.
Click Next.
Choose 'Place all certificates in the following store', select "show physical stores', and choose 'Trusted Root Certification Authorities'->Local Computer
Click Next and Finish.
Say Yes when asked if you want to install this certificate