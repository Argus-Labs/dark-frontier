package main

import (
	"os"

	"github.com/argus-labs/darkfrontier-backend/cardinal/component"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/query"
	"github.com/argus-labs/darkfrontier-backend/cardinal/system"
	"github.com/argus-labs/darkfrontier-backend/cardinal/tx"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"github.com/redis/go-redis/v9"
	"github.com/rs/zerolog"
	"github.com/rs/zerolog/log"
	"pkg.world.dev/world-engine/cardinal"
)

func main() {
	zerolog.TimeFieldFormat = zerolog.TimeFormatUnix

	EnvRedisAddr := os.Getenv("REDIS_ADDR")
	EnvRedisPassword := os.Getenv("REDIS_PASSWORD")
	mode := os.Getenv("CARDINAL_MODE")

	utils.Must(utils.SetConstantsFromEnv())

	// Start world and register systems
	var world *cardinal.World
	if mode == string(cardinal.RunModeProd) {
		world = utils.NewProdWorld(EnvRedisAddr, EnvRedisPassword)
		utils.Must(cardinal.RegisterSystems(
			world,
			system.SendEnergySystem,
			system.ClaimHomePlanetSystem,
			system.ShipArriveSystem,
			system.SetConstantSystem,
		))
	} else {
		log.Warn().Msg("CARDINAL_MODE was not set to production, defaulting to development")
		world = utils.NewDevWorld(EnvRedisAddr)
		utils.Must(cardinal.RegisterSystems(
			world,
			system.SendEnergySystem,
			system.ClaimHomePlanetSystem,
			system.DebugClaimPlanetSystem,
			system.ShipArriveSystem,
			system.DebugEnergyBoostSystem,
			system.SetConstantSystem,
			system.MetricSystem,
		))
	}

	// Register components
	// NOTE: You must register your components here,
	// otherwise it will show an error when you try to use them in a system.
	utils.Must(cardinal.RegisterComponent[component.PlayerComponent](world))
	utils.Must(cardinal.RegisterComponent[component.PlanetComponent](world))
	utils.Must(cardinal.RegisterComponent[component.ShipComponent](world))
	utils.Must(cardinal.RegisterComponent[component.DefaultsComponent](world))

	// Register transactions
	// NOTE: You must register your transactions here,
	// otherwise it will show an error when you try to use them in a system.
	utils.Must(cardinal.RegisterMessages(
		world,
		tx.SendEnergy,
		tx.ClaimHomePlanet,
		tx.DebugClaimPlanet,
		tx.DebugEnergyBoost,
		tx.SetConstant,
	))

	utils.Must(cardinal.RegisterQuery[query.ConstantMsg, query.ConstantReply](world, "constant", query.Constants))
	utils.Must(cardinal.RegisterQuery[query.CurrentTickMsg, query.CurrentTickReply](world, "current-tick", query.CurrentTick))
	utils.Must(cardinal.RegisterQuery[query.PlanetsMsg, query.PlanetsReply](world, "planets", query.Planets))
	utils.Must(cardinal.RegisterQuery[query.PlayerRangeMsg, query.PlayerRangeReply](world, "player-range", query.PlayerRange))
	utils.Must(cardinal.RegisterQuery[query.PlayerRankMsg, query.PlayerRankReply](world, "player-rank", query.PlayerRank))

	options := &redis.Options{
		Addr:     EnvRedisAddr,
		Password: EnvRedisPassword,
		DB:       0,
	}

	game.LeaderboardClient = redis.NewClient(options)

	utils.Must(world.StartGame())
}
