## Start Cardinal and Nakama
game:
	cd cardinal && rm -rf vendor && go mod tidy && go mod vendor && cd .. && docker compose up --build game nakama


# Start the EVM Base Shard and Celestia Devnet
rollup:
	cd scripts && chmod 777 start.sh && ./start.sh

bucketName := df-cloud-prover
artifactObjNames := init-084d8871-4cdb-46ab-b541-298cde6f9236-vk \
                    init-084d8871-4cdb-46ab-b541-298cde6f9236-pk \
                    init-084d8871-4cdb-46ab-b541-298cde6f9236-cs \
                    move-084d8871-4cdb-46ab-b541-298cde6f9236-vk \
                    move-084d8871-4cdb-46ab-b541-298cde6f9236-pk \
                    move-084d8871-4cdb-46ab-b541-298cde6f9236-cs

.PHONY: getCircuitArtifacts

# Downloads the circuit artifacts from GCS and places them in /circuit/artifacts/
getCircuitArtifacts:
	@echo "Executing getCircuitArtifacts"
	$(foreach obj,$(artifactObjNames), \
        echo "Downloading: $(obj)" && \
        make downloadFromGCS objectName=$(obj); \
    )

downloadFromGCS:
	@url="https://storage.googleapis.com/$(bucketName)/$(objectName)"; \
		dirPath="circuit/artifacts"; \
		if [ ! -d "$$dirPath" ]; then \
				mkdir -p "$$dirPath"; \
		fi; \
		fullPath="$${dirPath}/$(objectName)"; \
    if ! curl -sSf "$$url" --output "$$fullPath"; then \
        echo "Failed to query artifact from GCS: $$url"; \
        exit 1; \
    fi;



.PHONY: build prepareDir

build:
	@echo "Building cardinal"
	@make prepareDir dir=cardinal
	@docker compose build

prepareDir:
	@echo "Preparing directory $(dir)"
	@cd $(dir) && rm -rf ./vendor
	@cd $(dir) && go mod tidy
	@cd $(dir) && go mod vendor
	@cd ..

check:
	@echo "Checking Docker installation..."
	@if ! command -v docker > /dev/null; then \
		echo "FAILURE"; \
		echo "  Docker is not installed"; \
		exit 1; \
	fi; \
	echo "Success"

	@echo "Checking Docker Compose installation..."
	@if ! docker compose version > /dev/null 2>&1; then \
		echo "FAILURE"; \
		echo "  Docker Compose is not installed"; \
		exit 1; \
	fi; \
	echo "Success"

	@echo "Checking Docker daemon..."
	@if ! docker info > /dev/null 2>&1; then \
		echo "FAILURE"; \
		echo "  Docker daemon is not running"; \
		exit 1; \
	fi; \
	echo "Success"

	@echo "Checking Git installation..."
	@if ! command -v git > /dev/null; then \
		echo "FAILURE"; \
		echo "  Git is not installed"; \
		exit 1; \
	fi; \
	echo "Success"
