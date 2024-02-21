package tx

import (
	"fmt"
	"pkg.world.dev/world-engine/cardinal"
)

type DebugEnergyBoostMsg struct {
	LocationHash string `json:"locationHash"`
}

type DebugEnergyBoostReply struct{}

var DebugEnergyBoost = cardinal.NewMessageType[DebugEnergyBoostMsg, DebugEnergyBoostReply]("debug-energy-boost")

func (msg DebugEnergyBoostMsg) Validate() error {
	// Check that LocationHashFrom and LocationHashTo is 64 characters long
	if len(msg.LocationHash) != 64 {
		return fmt.Errorf("location hash length was not 64 chars: %s", msg.LocationHash)
	}

	return nil
}
