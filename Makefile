.PHONY: test

# The NUGET_API_KEY variable can be passed in as an argument or as an environment variable.
# If it is passed in as an argument, it will take precedence over the environment variable.
NUGET_API_KEY ?= $(shell env | grep NUGET_API_KEY)

prepare:
	dotnet build

test:
	dotnet build ./src/Extism.Pdk.MSBuild
	dotnet publish -c Release ./samples/KitchenSink
	dotnet test ./tests/Extism.Pdk.MsBuild.Tests
	dotnet test ./tests/Extism.Pdk.WasmTests

clean:
	dotnet clean

publish: clean
	dotnet build -c Release ./src/Extism.Pdk.MSBuild/Extism.Pdk.MSBuild.csproj
	dotnet build -c Release ./src/Extism.Pdk/Extism.Pdk.csproj
	dotnet pack -c Release ./src/Extism.Pdk/Extism.Pdk.csproj
	dotnet nuget push --source https://api.nuget.org/v3/index.json ./src/Extism.Pdk/bin/Release/*.nupkg --api-key $(NUGET_API_KEY)

format:
	dotnet format
