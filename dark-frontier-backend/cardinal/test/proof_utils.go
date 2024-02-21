package utils

import (
	"bytes"
	"encoding/base64"
	"encoding/json"
	"github.com/argus-labs/darkfrontier-backend/circuit/initialize"
	"github.com/argus-labs/darkfrontier-backend/circuit/move"
	"github.com/argus-labs/darkfrontier-backend/circuit/perlin"
	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend"
	"github.com/consensys/gnark/backend/groth16"
	"github.com/consensys/gnark/frontend"
	"github.com/stretchr/testify/assert"
	"testing"
)

type GenerateProofRequest struct {
	CircuitType        string          `json:"circuitType"`
	CircuitAssignments json.RawMessage `json:"circuitAssignments"`
}

type GenerateProofResponse struct {
	CircuitArtifactUUID string `json:"circuitArtifactUUID"` // Echo the circuit artifact UUID
	CircuitType         string `json:"circuitType"`         // Echo the circuit type
	Proof               string `json:"proof"`               // Proof in Base64
}

func getProofForInitCircuit(t *testing.T, assignment initialize.InitCircuit) (proof string, err error) {
	fullWitness, err := frontend.NewWitness(&assignment, ecc.BN254.ScalarField())
	assert.NoError(t, err)

	tempProof, err := groth16.Prove(InitCCS, InitProvingKey, fullWitness, backend.WithHints(perlin.ModuloHint))
	assert.NoError(t, err)

	proofBuf := bytes.Buffer{}
	_, err = tempProof.WriteRawTo(&proofBuf)
	assert.NoError(t, err)

	proof = base64.StdEncoding.EncodeToString(proofBuf.Bytes())
	return proof, nil
}

func getProofForMoveCircuit(t *testing.T, assignment move.MoveCircuit) (proof string, err error) {
	fullWitness, err := frontend.NewWitness(&assignment, ecc.BN254.ScalarField())
	assert.NoError(t, err)

	tempProof, err := groth16.Prove(MoveCCS, MoveProvingKey, fullWitness, backend.WithHints(perlin.ModuloHint))
	assert.NoError(t, err)

	proofBuf := bytes.Buffer{}
	_, err = tempProof.WriteRawTo(&proofBuf)
	assert.NoError(t, err)
	proof = base64.StdEncoding.EncodeToString(proofBuf.Bytes())
	return proof, nil
}
