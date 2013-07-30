require 'faye/websocket'

puts "Loading websocket..."

App = lambda do |env|
  if Faye::WebSocket.websocket?(env)
      ws = Faye::WebSocket.new(env,"isonas")

      ws.on :message do |event|
		exit! if (event.data == "shutdownserver")
        puts "got message '#{event.data}'"       
        ws.send("Received your message "+event.data)
      end

      ws.on :close do |event|
        p [:close, event.code, event.reason]
        ws = nil
      end

      ws.on :open do |event|
        puts "Socket opened, awaiting authentication"
      end

      # Return async Rack response
      ws.rack_response

    else
      # Normal HTTP request
      [200, {'Content-Type' => 'text/plain'}, ['Hello']]
    end
end