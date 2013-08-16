require 'faye/websocket'

puts "Loading FAYE server..."

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

App = Faye::RackAdapter.new

class RetryAdviceOverride
	def outgoing(message,callback)		
		if (message["advice"] and File.exists?("noreconnect.txt"))
			puts "FayeClientTest: Overriding reconnect advice with none instead of retry"
			message["advice"]["reconnect"] = "none"
		end
		callback.call(message)
	end
end

App.add_extension(RetryAdviceOverride.new)