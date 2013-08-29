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

class CustomSubscriptionOverride
	def outgoing(message,callback)		
		if (message["channel"] == Faye::Channel::HANDSHAKE)
			ext = message["ext"] || {}
			ext["customhsinfo"] = "123test"
		end
		callback.call(message)
	end
	def incoming(message,callback)
		if (message["channel"] == Faye::Channel::SUBSCRIBE and message["subscription"] == "/servertest/customsubscription")
			App.get_client.publish("/servertest/customsubscriptioninforesponse", 'text' => 'foobar')
		end

		callback.call(message)
	end
end

App.add_extension(CustomSubscriptionOverride.new)


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