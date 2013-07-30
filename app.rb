require 'faye/websocket'

puts "Loading websocket..."
# TODO: Move this to the Ruby project
Thread.new {
	shutdownTrigger = ENV['SHUTDOWN']
	puts "Watching for shutdown trigger #{shutdownTrigger}"
	while true do
		sleep(2)		
		if File.exists?(shutdownTrigger)
			puts "Saw shutdown trigger, shutting down!"
			# Am not deleting trigger file here because of Windows file locking
			exit!
		end
	end
}

App = lambda do |env|
  if Faye::WebSocket.websocket?(env)
      ws = Faye::WebSocket.new(env,"isonas")

      ws.on :message do |event|
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