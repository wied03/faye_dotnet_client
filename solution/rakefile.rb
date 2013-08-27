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
task :package => [:clean, :version, :build, :pack]

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

with (".nuget/nuget.exe") do |ngetpath|
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
				
				nugetpack :packfayeclient do |n|
						n.command = ngetpath
						n.nuspec = "#{projPath}/Bsw.FayeDotNet.csproj"
						n.base_folder = projPath
						n.output = projPath
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
				
				nugetpack :packwebsocketwrapper do |n|
						n.command = ngetpath
						n.nuspec = "#{projPath}/Bsw.WebSocket4Net.Wrapper.csproj"
						n.base_folder = projPath
						n.output = projPath
				end					
			end	
			
		end
	end
end
