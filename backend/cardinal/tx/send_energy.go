package tx

import (
	"bytes"
	"encoding/base64"
	"fmt"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/keys"
	"github.com/argus-labs/darkfrontier-backend/circuit/move"
	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend/groth16"
	"github.com/consensys/gnark/backend/witness"
	"github.com/consensys/gnark/frontend"
	"math/big"
	"pkg.world.dev/world-engine/cardinal"
)

type PlanetReceipt struct {
	Level               int64  `json:"level"`
	LocationHash        string `json:"locationHash"`
	OwnerPersonaTag     string `json:"ownerPersonaTag"`
	EnergyCurrent       string `json:"energyCurrent"`
	EnergyMax           string `json:"energyMax"`
	EnergyRefill        string `json:"energyRefill"`
	Defense             string `json:"defense"`
	Range               string `json:"range"`
	Speed               string `json:"speed"`
	LastUpdateRefillAge string `json:"lastUpdateRefillAge"`
	LastUpdateTick      string `json:"lastUpdateTick"`
	SpaceArea           int64  `json:"SpaceArea"`
}

type ShipReceipt struct {
	Id               uint64 `json:"id"`
	OwnerPersonaTag  string `json:"ownerPersonaTag"`
	LocationHashFrom string `json:"locationHashFrom"`
	LocationHashTo   string `json:"locationHashTo"`
	TickStart        int64  `json:"tickStart"`
	TickArrive       int64  `json:"tickArrive"`
	EnergyOnEmbark   string `json:"energyOnEmbark"`
}

type SendEnergyMsg struct {
	LocationHashFrom string `json:"locationHashFrom"`
	LocationHashTo   string `json:"locationHashTo"`
	PerlinTo         int64  `json:"perlinTo"`
	RadiusTo         int64  `json:"radiusTo"`
	MaxDistance      int64  `json:"maxDistance"`
	Energy           int64  `json:"energy"`
	Proof            string `json:"proof"`
}

type SendEnergyReply struct {
	SentShip        ShipReceipt   `json:"sentShip"`
	NewPlanet       PlanetReceipt `json:"newPlanet"`
	NewSenderEnergy string        `json:"newSenderEnergy"`
}

var SendEnergy = cardinal.NewMessageTypeWithEVMSupport[SendEnergyMsg, SendEnergyReply]("send-energy")

func (msg SendEnergyMsg) Validate() error {
	// Check that LocationHashFrom and LocationHashTo is 64 characters long
	if len(msg.LocationHashFrom) != 64 || len(msg.LocationHashTo) != 64 {
		return fmt.Errorf("location must be 64 characters long")
	}
	return nil
}

func (msg SendEnergyMsg) CreatePublicWitness() (witness.Witness, error) {
	pub1, _ := new(big.Int).SetString(msg.LocationHashFrom, 16)
	pub2, _ := new(big.Int).SetString(msg.LocationHashTo, 16)
	moveAssignment := move.MoveCircuit{
		R:       msg.RadiusTo,
		DistMax: msg.MaxDistance,
		Scale:   game.WorldConstants.Scale,
		XMirror: game.WorldConstants.XMirror,
		YMirror: game.WorldConstants.YMirror,
		Pub1:    pub1,
		Pub2:    pub2,
		Perl2:   msg.PerlinTo,
	}

	publicWitness, err := frontend.NewWitness(&moveAssignment, ecc.BN254.ScalarField(), frontend.PublicOnly())
	return publicWitness, err
}

func (msg SendEnergyMsg) VerifyMoveProof(publicWitness witness.Witness) error {
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
	err = groth16.Verify(proof, keys.MoveVerifyingKey, publicWitness)
	return err
}
