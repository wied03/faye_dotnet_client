$: << File.expand_path(File.dirname(__FILE__))
require "msbuild"
require "tools"
require "config"
require "nunit"
require "albacore"

with('Bsw.FayeDotNet.sln') do |sln|
	BradyW::MSBuild.new :cleandnet do |clean|
		clean.targets = "clean"
		clean.solution = sln
	end

	BradyW::MSBuild.new :build do |build|
		build.solution = sln
	end
end

task :ci => [:clean, :build, :test]
task :clean => [:cleandnet, :cleanpackages]
task :test => [:codetest]
task :package => [:clean, :pack]

task :version => [:versionwebsocketwrapper,
				  :versionfayeclient]
				  
task :pack => [:packwebsocketwrapper,
			   :packfayeclient]

with ('test') do |t|	
	BradyW::Nunit.new :codetest => :build do |test|
		test.files = FileList["#{t}/**/bin/Debug/*Test.dll"]
	end	
end

task :cleanpackages do
	rm_rf FileList['**/*.nupkg']
end

# We might have already done this in this build cycle, but we update the source with versions
# so need to do force a build
task :forcebuildforpackages do
	Rake::Task["build"].execute
end

with (".nuget/nuget.exe") do |ngetpath|
	with (ENV['nuget_apikey']) do |apikey|
		with (ENV['version_number']) do |ver|
			with("BSW Technology Consulting") do |companyName|
				with ('src/Bsw.FayeDotNet') do |projPath|
					with ("#{projPath}/Properties/AssemblyInfo.cs") do |asminfo|
						assemblyinfo :versionfayeclient do |asm|
							puts "Putting version number #{ver} on assembly"
							asm.version = ver
							asm.file_version = ver
							asm.company_name = companyName
							asm.product_name = "BSW FAYE .NET Client"
							asm.output_file = asminfo
							asm.input_file = asminfo
						end			
					end
					
					nugetpack :packfayeclient => [:versionfayeclient,:forcebuildforpackages] do |n|
							n.command = ngetpath
							n.nuspec = "#{projPath}/Bsw.FayeDotNet.csproj"
							n.base_folder = projPath
							n.output = projPath
					end

					nugetpush :pushfayeclient => :packfayeclient do |n|
						n.command = ngetpath
						n.package = "#{projPath}/Bsw.FayeDotNet.#{ver}.nupkg"
						n.apikey = apikey						
					end
				end
				
				with ('src/Bsw.WebSocket4Net.Wrapper') do |projPath|
					with ("#{projPath}/Properties/AssemblyInfo.cs") do |asminfo|
						assemblyinfo :versionwebsocketwrapper do |asm|
							puts "Putting version number #{ver} on assembly"
							asm.version = ver
							asm.file_version = ver
							asm.company_name = companyName
							asm.product_name = "BSW WebSocket4Net Wrapper"
							asm.output_file = asminfo
							asm.input_file = asminfo
						end			
					end
					
					nugetpack :packwebsocketwrapper => [:versionwebsocketwrapper,:forcebuildforpackages] do |n|
							n.command = ngetpath
							n.nuspec = "#{projPath}/Bsw.WebSocket4Net.Wrapper.csproj"
							n.base_folder = projPath
							n.output = projPath
					end					
					
					nugetpush :pushwebsocketwrapper => :packwebsocketwrapper do |n|
						n.command = ngetpath
						n.package = "#{projPath}/Bsw.WebSocket4Net.Wrapper.#{ver}.nupkg"
						n.apikey = apikey						
					end
				end				
			end
		end
	end
end
