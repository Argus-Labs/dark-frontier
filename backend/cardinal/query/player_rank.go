package query

import (
	"context"
	"errors"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/redis/go-redis/v9"
	"pkg.world.dev/world-engine/cardinal"
)

type PlayerRankMsg struct {
	PersonaTag string `json:"personaTag"`
}

type PlayerRankReply struct {
	Rank  int64   `json:"rank"`
	Score float64 `json:"score"`
}

func PlayerRank(wCtx cardinal.WorldContext, req *PlayerRankMsg) (*PlayerRankReply, error) {
	rank, score, err := game.GetPlayerRankAndScore(context.Background(), req.PersonaTag)
	if err != nil {
		if !errors.Is(err, redis.Nil) {
			wCtx.Logger().Warn().Msgf("error reading player rank %v", err)
		}
		return &PlayerRankReply{Rank: 99999, Score: 0}, nil
	}

	return &PlayerRankReply{
		Rank:  rank,
		Score: score,
	}, nil
}
