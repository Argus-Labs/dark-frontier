package tx

import (
	"fmt"
	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"pkg.world.dev/world-engine/cardinal"
)

type DebugClaimPlanetMsg struct {
	LocationHash string `json:"locationHash"`
	Perlin       int64  `json:"perlin"`
}

type DebugClaimPlanetReply struct {
	ClaimedPlanet PlanetReceipt             `json:"claimedPlanet"`
	CreatedPlayer component.PlayerComponent `json:"createdPlayer"`
}

var DebugClaimPlanet = cardinal.NewMessageType[DebugClaimPlanetMsg, DebugClaimPlanetReply]("debug-claim-planet")

func (msg DebugClaimPlanetMsg) Validate() error {
	// Check that LocationHashFrom and LocationHashTo is 64 characters long
	if len(msg.LocationHash) != 64 {
		return fmt.Errorf("location hash length was not 64 chars: %s", msg.LocationHash)
	}

	return nil
}
