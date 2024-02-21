package query

import (
	"errors"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"pkg.world.dev/world-engine/cardinal"
)

type ConstantMsg struct {
	ConstantLabel string `json:"constantLabel"`
}

type ConstantReply struct {
	Constants any `json:"constants"`
}

func Constants(_ cardinal.WorldContext, req *ConstantMsg) (*ConstantReply, error) {
	// Handle all constants query
	if req.ConstantLabel == game.AllConstantsLabel {
		// Create a map of all constants and set it to be the value of result
		constants := make(map[string]any)
		for _, c := range game.ExposedConstants {
			constants[c.Label] = c.Value
		}
		return &ConstantReply{constants}, nil
	}

	// Handle single constant query
	for _, constant := range game.ExposedConstants {
		if constant.Label == req.ConstantLabel {
			return &ConstantReply{constant.Value}, nil
		}
	}

	return &ConstantReply{nil}, errors.New("constant not found")
}
