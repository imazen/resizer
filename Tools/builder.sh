#!/usr/bin/env bash
if test "$OS" = "Windows_NT"
then
  # use .Net

  ../.paket/paket.bootstrapper.exe 
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi

  cd ./FakeBuilder
  ../../.paket/paket.exe restore
  exit_code=$?
  cd ..
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi

  ./FakeBuilder/packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO ./FakeBuilder/build.fsx 
else
  # use mono
  mono ../.paket/paket.bootstrapper.exe 
  exit_code=$?
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi
  cd ./FakeBuilder
  mono ../../.paket/paket.exe restore
  exit_code=$?
  cd ..
  if [ $exit_code -ne 0 ]; then
    exit $exit_code
  fi
  mono ./FakeBuilder/packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO ./FakeBuilder/Build.fsx 
fi
