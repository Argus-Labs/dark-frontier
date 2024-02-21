package system

import "pkg.world.dev/world-engine/cardinal"

func MetricSystem(wCtx cardinal.WorldContext) error {
	log := wCtx.Logger()
	search, err := wCtx.NewSearch(cardinal.All())
	if err != nil {
		return err
	}
	count, err := search.Count(wCtx)
	if err != nil {
		return err
	}
	log.Info().Int("entity_amount", count).Msg("metrics")
	return nil
}
