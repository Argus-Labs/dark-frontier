package system

import (
	"encoding/json"
	"errors"
	"fmt"
	"regexp"

	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"pkg.world.dev/world-engine/cardinal"
)

type SpaceConstantsMsg struct {
	StatBuffMultiplier      string
	ScoreMultiplier         string
	DefenseDebuffMultiplier string
}

type LevelConstantsMsg struct {
	EnergyDefault string
	EnergyMax     string
	EnergyRefill  string
	Range         string
	Speed         string
	Defense       string
	Score         string
}

func SetConstantSystem(wCtx cardinal.WorldContext) error {
	log := wCtx.Logger()

	tx.SetConstant.Each(wCtx, func(t cardinal.TxData[tx.SetConstantMsg]) (result tx.SetConstantReply, err error) {
		txData := t.Msg()
		txSig := t.Tx()
		result.Success = false

		// 1. PRE-CONDITION: Check that the sender is an admin
		if txSig.PersonaTag != "admin" {
			return result, fmt.Errorf("A non-admin tried to set a constant")
		}

		match, _ := regexp.MatchString(`^Level[0-6]Constants$`, txData.ConstantName)
		if match {
			level, err := extractLevelNumber(txData.ConstantName)
			if err != nil {
				return result, err
			}

			// The msg will come in as JSON, we turn it into bytes,
			// then unmarshall those bytes into a Go struct because
			// the type for Value is any and casting from any to a Go struct is not possible
			bz, err := json.Marshal(txData.Value)
			if err != nil {
				return result, err
			}
			var msg LevelConstantsMsg
			err = json.Unmarshal(bz, &msg)
			if err != nil {
				return result, err
			}
			err = findAndUpdatePlanetsByLevel(wCtx, level, msg)
			if err != nil {
				return result, err
			}
			result.Success = true
			return result, nil
		}

		// 2. Set the respective constant after doing any necessary validation
		switch txData.ConstantName {
		case "Radius":
			newRadius := txData.Value.(float64)
			log.Debug().Msgf("Received payload to set radius with new radius: %f", txData.Value)
			if err = txData.ValidateRadius(); err != nil {
				return result, err
			}
			game.WorldConstants.RadiusMax = int64(newRadius)
			result.Success = true
			log.Debug().Msgf("Successfully set the world radius to: %d", game.WorldConstants.RadiusMax)

		case "Timer":
			newTimeRemaining, ok := txData.Value.(float64)
			log.Debug().Msgf("Received payload to set InstanceTimer at with new time remaining: %f", newTimeRemaining)
			if !ok {
				return result, errors.New("new value for InstanceTimer was not an int")
			}
			game.WorldConstants.InstanceTimer = int(newTimeRemaining)
			result.Success = true
			log.Debug().Msgf("Successfully set the instance time remaining to: %d", game.WorldConstants.InstanceTimer)

		case "InstanceName":
			newName, ok := txData.Value.(string)
			log.Debug().Msgf("Received payload to set InstanceName at with new instance name: %s", newName)
			if !ok {
				return result, errors.New("new value for InstanceName was not a string")
			}
			game.WorldConstants.InstanceName = newName
			result.Success = true
			log.Debug().Msgf("Successfully set the instance name to: %s", game.WorldConstants.InstanceName)

		case "NebulaSpaceConstants":
			err = handleSpaceConstantsMsg(wCtx, txData.Value, 0)
			if err != nil {
				return result, err
			}
			result.Success = true

		case "SafeSpaceConstants":
			err = handleSpaceConstantsMsg(wCtx, txData.Value, 1)
			if err != nil {
				return result, err
			}
			result.Success = true

		case "DeepSpaceConstants":
			err = handleSpaceConstantsMsg(wCtx, txData.Value, 2)
			if err != nil {
				return result, err
			}
			result.Success = true

		default:
			return result, fmt.Errorf("recieved invalid request to set an invalid constant with name %s", txData.ConstantName)
		}
		return result, nil
	})
	return nil
}

func handleSpaceConstantsMsg(wCtx cardinal.WorldContext, value any, spaceArea int64) error {
	bz, err := json.Marshal(value)
	if err != nil {
		return err
	}
	var msg SpaceConstantsMsg
	err = json.Unmarshal(bz, &msg)
	if err != nil {
		return err
	}
	err = findAndUpdatePlanetsBySpaceArea(wCtx, spaceArea, msg)
	if err != nil {
		return err
	}
	return nil
}
