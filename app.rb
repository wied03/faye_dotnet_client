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