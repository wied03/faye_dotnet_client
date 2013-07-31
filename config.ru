 rquire 'faye'
 require File.expand_path('../app', __FILE__)
 
 Faye::WebSocket.load_adapter('thin')
 
 run App
