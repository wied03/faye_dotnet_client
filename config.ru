 require 'faye'
 require File.expand_path('../app', __FILE__)
 
 Faye::Logging.log_level = :debug
 Faye.logger = lambda { |m| puts m }
 Faye::WebSocket.load_adapter('thin')
 
 run App
