package query

import (
	"context"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"pkg.world.dev/world-engine/cardinal"
)

type PlayerRangeMsg struct {
	Start int64 `json:"start"`
	End   int64 `json:"end"`
}

type PlayerRangeReply struct {
	Players []game.RankedPlayer `json:"players"`
}

func PlayerRange(wCtx cardinal.WorldContext, req *PlayerRangeMsg) (*PlayerRangeReply, error) {
	players, err := game.GetPlayersInRankRange(context.Background(), req.Start, req.End)
	if err != nil {
		wCtx.Logger().Debug().Msgf("error reading player range [%d, %d] %v", req.Start, req.End, err)
		return &PlayerRangeReply{}, err
	}

	return &PlayerRangeReply{Players: players}, nil
}
