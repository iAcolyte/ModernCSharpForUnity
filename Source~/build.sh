#!/usr/bin/env bash
# Builds the polyfill generator and copies it to Generators~/, the path csc.rsp files point to.
set -euo pipefail

cd "$(dirname "$0")"
dotnet build ModernCSharp.Generators/ModernCSharp.Generators.csproj -c Release -nologo
mkdir -p ../Generators~
cp ModernCSharp.Generators/bin/Release/netstandard2.0/ModernCSharp.Generators.dll ../Generators~/
echo "Copied to Generators~/ModernCSharp.Generators.dll"
