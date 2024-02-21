package query

import (
	"pkg.world.dev/world-engine/cardinal"
)

type CurrentTickMsg struct{}

type CurrentTickReply struct {
	CurrentTick uint64 `json:"currentTick"`
}

func CurrentTick(wCtx cardinal.WorldContext, req *CurrentTickMsg) (*CurrentTickReply, error) {
	return &CurrentTickReply{
		wCtx.CurrentTick(),
	}, nil
}
