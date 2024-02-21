package tx

import (
	"errors"
	"fmt"
	"pkg.world.dev/world-engine/cardinal"
)

type SetConstantMsg struct {
	ConstantName string `json:"constantName"`
	Value        any    `json:"value"`
}

type SetConstantReply struct {
	Success bool `json:"success"`
}

var SetConstant = cardinal.NewMessageType[SetConstantMsg, SetConstantReply]("set-constant")

func (msg SetConstantMsg) ValidateRadius() error {
	value, ok := msg.Value.(float64)
	if !ok {
		return errors.New("msg.Value is not a float64")
	}
	// Check that the new radius is valid
	if value < 0 {
		return fmt.Errorf("new radius was less than 0: %f", value)
	}
	return nil
}
