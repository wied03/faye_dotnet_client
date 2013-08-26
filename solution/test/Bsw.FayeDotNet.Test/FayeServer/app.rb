puts "Loading FAYE..."

App = Faye::RackAdapter.new(:mount => '/tester')
