package utils

import (
	"fmt"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/rs/zerolog/log"
	"os"
	"pkg.world.dev/world-engine/cardinal"
	"pkg.world.dev/world-engine/cardinal/shard"
	"strconv"
	"time"
)

// NewProdWorld should be the only way to run the game in production
func NewProdWorld(addr string, password string) *cardinal.World {
	if addr == "" || password == "" {
		panic("redis addr or password is empty string")
	}

	adapter := setupAdapter()
	options := getOptions(adapter)
	world, err := cardinal.NewWorld(options...)
	if err != nil {
		panic(err)
	}

	return world
}

// NewDevWorld is the recommended way of running the game for development
// where you are going to need use Retool to inspect the state.
// NOTE(1): You will need to have Redis running in `EnvRedisAddr` for this to work.
// NOTE(2): In prod, your Redis should have a password loaded from env var so don't use this.
func NewDevWorld(addr string) *cardinal.World {
	if addr == "" {
		panic("redis addr is empty string")
	}

	adapter := setupAdapter()
	options := getOptions(adapter)
	world, err := cardinal.NewMockWorld(options...)
	if err != nil {
		panic(err)
	}

	return world
}

func setupAdapter() shard.Adapter {
	baseShardAddr := os.Getenv("BASE_SHARD_ADDR")
	shardSequencerAddr := os.Getenv("SHARD_SEQUENCER_ADDR")
	cfg := shard.AdapterConfig{
		ShardSequencerAddr: shardSequencerAddr,
		EVMBaseShardAddr:   baseShardAddr,
	}
	adapter, err := shard.NewAdapter(cfg)
	if err != nil {
		panic(err)
	}
	return adapter
}

func getOptions(adapter shard.Adapter) []cardinal.WorldOption {
	tickRateInTime := time.Duration(1000/game.WorldConstants.TickRate) * time.Millisecond
	options := []cardinal.WorldOption{
		cardinal.WithReceiptHistorySize(500),
		cardinal.WithAdapter(adapter),
		cardinal.WithTickChannel(time.Tick(tickRateInTime)),
	}
	return options
}

func SetConstantsFromEnv() error {
	// Set TIMER
	gameTimer := os.Getenv("TIMER")
	if gameTimer == "" {
		log.Info().Msg("TIMER was not set, defaulting InstanceTimer to two weeks")
		game.WorldConstants.InstanceTimer = 1209600
	}
	gameTimerInt, err := strconv.Atoi(gameTimer)
	if err != nil {
		return err
	}
	if gameTimerInt <= 0 {
		return fmt.Errorf("TIMER was set to an invalid value: %d", gameTimerInt)
	}
	game.WorldConstants.InstanceTimer = gameTimerInt

	// Set InstanceName
	instanceName := os.Getenv("CARDINAL_NAMESPACE")
	if instanceName == "" {
		log.Info().Msg("CARDINAL_NAMESPACE was not set, defaulting InstanceName to dark-frontier")
		game.WorldConstants.InstanceName = "dark-frontier"
	}
	game.WorldConstants.InstanceName = instanceName

	// Set World Radius
	radius := os.Getenv("WORLD_RADIUS")
	if radius == "" {
		log.Info().Msg("WORLD_RADIUS was not set, defaulting RadiusMax to 2000")
		game.WorldConstants.RadiusMax = 2000
	}
	radiusInt, err := strconv.Atoi(radius)
	if err != nil {
		return err
	}
	if radiusInt <= 0 {
		return fmt.Errorf("WORLD_RADIUS was set to an invalid value: %d", radiusInt)
	}
	game.WorldConstants.RadiusMax = int64(radiusInt)

	return nil
}
