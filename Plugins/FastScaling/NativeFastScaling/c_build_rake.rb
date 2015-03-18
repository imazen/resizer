require 'rake/loaders/makefile'
require 'rbconfig'
Rake.application.add_loader('d', Rake::MakefileLoader.new)

CLEAN = Rake::FileList["**/*~", "**/*.bak", "**/core"]
CLEAN.clear_exclude.exclude { |fn|
  fn.pathmap("%f").downcase == 'core' && File.directory?(fn)
}

CLEAN.include('*.o')

MACOS = !!(/darwin|mac os/ =~ RbConfig::CONFIG['host_os'])


cxx = ENV['CXX'] || "g++"
cc = ENV['CC'] || "gcc"
CXX = MACOS && cxx == "g++" ? "g++-4.9" : cxx;
CC = MACOS && cc == "gcc" ? "gcc-4.9" : cc;

VALGRIND_OPTS =  MACOS ? "--dsymutil=yes" : ""

TRAVIS_USAFE_FLAGS = " -Wfloat-conversion "

COMMON_FLAGS=" -fPIC -O2 -g -Wpointer-arith -Wcast-qual -Wpedantic -Wall -Wextra -Wno-unused-parameter -Wuninitialized -Wredundant-decls -Werror #{ENV['CFLAGS']}"

CFLAGS="#{COMMON_FLAGS} #{ENV['CI'] ? '' : TRAVIS_USAFE_FLAGS} -std=c11 -Wstrict-prototypes -Wmissing-prototypes -Wc++-compat -Wshadow"

CXXFLAGS="#{COMMON_FLAGS} #{ENV['CI'] ? '' : TRAVIS_USAFE_FLAGS} -std=gnu++11"

#-std=c++11


desc "Remove any temporary products."
task :clean do
  CLEAN.each do |fn|
    rm_rf fn
    if Rake::Task.task_defined?(fn)
      task = Rake::Task[fn]
      task.reenable
    end
  end
end

CLOBBER = Rake::FileList.new

desc "Remove any generated file."
task :clobber => [:clean] do
  CLOBBER.each { |fn| rm_r fn rescue nil }
end


class String
  def to_task
    Rake::Task[self]
  end
end

CLEAN.include('*.o')
CLEAN.include('*.d')
CLEAN.include('*.mf')

rule '.o' => '.cpp' do |t|
  sh "#{CXX}  #{CXXFLAGS} -MMD -c -o #{t.name} #{t.source}"
end

rule '.o' => '.c' do |t|
  sh "#{CC}  #{CFLAGS} -MMD -c -o #{t.name} #{t.source}"
end


rule '.d' => '.cpp' do |t|
  verbose(false) do 
    sh "#{CXX} -MM -MG -MF #{t.name} #{t.source}"
  end
end

rule '.d' => '.c' do |t|
  verbose(false) do 
    sh "#{CC} -MM -MG -MF #{t.name} #{t.source}"
  end
end

(FileList['*.c'] + FileList['*.cpp']).each do |source_file|
  dependency_file = source_file.pathmap("%X.d")
  dependency_file.to_task
  import dependency_file
end



