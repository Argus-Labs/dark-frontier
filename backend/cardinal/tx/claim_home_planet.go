package tx

import (
	"bytes"
	"encoding/base64"
	"fmt"
	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/keys"
	"github.com/argus-labs/darkfrontier-backend/circuit/initialize"
	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend/groth16"
	"github.com/consensys/gnark/backend/witness"
	"github.com/consensys/gnark/frontend"
	"math/big"
	"pkg.world.dev/world-engine/cardinal"
)

type ClaimHomePlanetMsg struct {
	LocationHash string `json:"locationHash"`
	Perlin       int64  `json:"perlin"`
	Proof        string `json:"proof"`
}

type ClaimHomePlanetReply struct {
	ClaimedPlanet PlanetReceipt             `json:"claimedPlanet"`
	CreatedPlayer component.PlayerComponent `json:"createdPlayer"`
}

var ClaimHomePlanet = cardinal.NewMessageTypeWithEVMSupport[ClaimHomePlanetMsg, ClaimHomePlanetReply]("claim-home-planet")

func (msg ClaimHomePlanetMsg) Validate() error {
	// Check that LocationHashFrom and LocationHashTo is 64 characters long
	if len(msg.LocationHash) != 64 {
		return fmt.Errorf("location hash length was not 64 chars: %s", msg.LocationHash)
	}

	return nil
}

func (msg ClaimHomePlanetMsg) CreatePublicWitness() (witness.Witness, error) {
	pub, _ := new(big.Int).SetString(msg.LocationHash, 16)
	initAssignment := initialize.InitCircuit{
		Scale:   game.WorldConstants.Scale,
		XMirror: game.WorldConstants.XMirror,
		YMirror: game.WorldConstants.YMirror,
		Pub:     pub,
		Perl:    msg.Perlin,
	}
	publicWitness, err := frontend.NewWitness(&initAssignment, ecc.BN254.ScalarField(), frontend.PublicOnly())
	return publicWitness, err
}

func (msg ClaimHomePlanetMsg) VerifyInitProof(publicWitness witness.Witness) error {
	// Parse proof from byte response
	proofByte, err := base64.StdEncoding.DecodeString(msg.Proof)
	if err != nil {
		return err
	}
	proof := groth16.NewProof(ecc.BN254)
	_, err = proof.ReadFrom(bytes.NewReader(proofByte))
	if err != nil {
		return err
	}
	err = groth16.Verify(proof, keys.InitVerifyingKey, publicWitness)
	return err
}
