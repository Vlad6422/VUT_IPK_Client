PROJECT_NAME=client
APP_NAME=ipk25-chat
SUBMISSION_NAME=ipk25chat-client
OUTPUTPATH = .

.PHONY: build publish clean

all: publish clean

build_app:
	@echo "Building $(APP_NAME)..."
	dotnet build $(PROJECT_NAME)/$(APP_NAME).csproj

publish: build_app
	@echo "Publishing $(APP_NAME)..."
	dotnet publish $(PROJECT_NAME)/$(APP_NAME).csproj -p:PublishSingleFile=true -p:AssemblyName=$(SUBMISSION_NAME) -c Release -r linux-x64 --self-contained false  -o $(OUTPUTPATH)
	@echo "Publishing $(APP_NAME) done."

clean:
	@echo "Cleaning $(PROJECT_NAME) build artifacts..."
	dotnet clean $(PROJECT_NAME)/$(APP_NAME).csproj
	rm -rf $(PROJECT_NAME)/$(APP_NAME)/bin $(PROJECT_NAME)/$(APP_NAME)/obj