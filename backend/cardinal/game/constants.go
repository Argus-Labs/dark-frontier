package game

type Constant struct {
	Label string
	Value any
}

type WorldConstant struct {
	MiMCSeedWord          string
	PerlinSeedWord        string
	MiMCNumRounds         int
	PerlinNumRounds       int
	XMirror               int
	YMirror               int
	Scale                 int
	CircuitArtifactUUID   string
	RadiusMax             int64
	SpacePerlinThresholds []int64
	InstanceName          string
	InstanceTimer         int
	TickRate              int
}

type PlanetLevelStats struct {
	Level         int64
	EnergyDefault string // decimal
	EnergyMax     string // decimal
	EnergyRefill  string // decimal
	Range         string // decimal
	Speed         string // decimal
	Defense       string // decimal
	Score         string // decimal
}

type SpaceConstant struct {
	Label                   string
	PlanetSpawnThreshold    string     // decimal
	PlanetLevelThreshold    [11]string // decimal
	StatBuffMultiplier      string     // decimal
	DefenseDebuffMultiplier string     // decimal
	ScoreMultiplier         string     // decimal
}

var (
	AllConstantsLabel = "all"
	// ExposedConstants If you want the constant to be queryable through `query_constant`,
	// make sure to add the constant to the list of exposed constants
	// Stored as a pointer because we update these constants via
	// tx/game/set-constant
	ExposedConstants = []Constant{
		{
			Label: "world",
			Value: &WorldConstants,
		},
		{
			Label: "space",
			Value: &SpaceConstants,
		},
		{
			Label: "base_planet_level_stats",
			Value: &BasePlanetLevelStats,
		},
	}

	WorldConstants = WorldConstant{
		MiMCSeedWord:          "1",
		PerlinSeedWord:        "1",
		MiMCNumRounds:         110,
		PerlinNumRounds:       4,
		XMirror:               0,
		YMirror:               0,
		Scale:                 256,
		CircuitArtifactUUID:   "084d8871-4cdb-46ab-b541-298cde6f9236",
		RadiusMax:             0, // Set in SetConstantsFromEnv()
		SpacePerlinThresholds: []int64{15, 17},
		InstanceName:          "", // Set in SetConstantsFromEnv()
		InstanceTimer:         0,  // Set in SetConstantsFromEnv()
		TickRate:              2,  // Ticks per second
	}

	SpaceConstants = [3]*SpaceConstant{
		&NebulaSpaceConstants,
		&SafeSpaceConstants,
		&DeepSpaceConstants,
	}

	BasePlanetLevelStats = [11]*PlanetLevelStats{
		&PlanetLevel0Stats,
		&PlanetLevel1Stats,
		&PlanetLevel2Stats,
		&PlanetLevel3Stats,
		&PlanetLevel4Stats,
		&PlanetLevel5Stats,
		&PlanetLevel6Stats,
		&PlanetLevel7Stats,
		&PlanetLevel8Stats,
		&PlanetLevel9Stats,
		&PlanetLevel10Stats,
	}

	NebulaSpaceConstants = SpaceConstant{
		Label:                   "Nebula",
		PlanetSpawnThreshold:    "0.0018",
		PlanetLevelThreshold:    [11]string{"0.4", "0.72", "0.95", "1", "-1", "-1", "-1", "-1", "-1", "-1", "-1"},
		StatBuffMultiplier:      "1",
		ScoreMultiplier:         "1",
		DefenseDebuffMultiplier: "1",
	}

	SafeSpaceConstants = SpaceConstant{
		Label:                   "SafeSpace",
		PlanetSpawnThreshold:    "0.001",
		PlanetLevelThreshold:    [11]string{"-1", "0.15", "0.5", "0.79", "0.99", "1", "-1", "-1", "-1", "-1", "-1"},
		StatBuffMultiplier:      "1.1",
		ScoreMultiplier:         "2",
		DefenseDebuffMultiplier: "0.8",
	}

	DeepSpaceConstants = SpaceConstant{
		Label:                   "DeepSpace",
		PlanetSpawnThreshold:    "0.0008",
		PlanetLevelThreshold:    [11]string{"-1", "-1", "0.15", "0.49", "0.84", "0.99", "1", "-1", "-1", "-1", "-1"},
		StatBuffMultiplier:      "1.3",
		ScoreMultiplier:         "3",
		DefenseDebuffMultiplier: "0.6",
	}

	PlanetLevel0Stats = PlanetLevelStats{
		Level:         0,
		EnergyDefault: "0",
		EnergyMax:     "100",
		EnergyRefill:  "30",
		Range:         "40",
		Speed:         "1",
		Defense:       "500",
		Score:         "10",
	}

	PlanetLevel1Stats = PlanetLevelStats{
		Level:         1,
		EnergyDefault: "0",
		EnergyMax:     "400",
		EnergyRefill:  "120",
		Range:         "50",
		Speed:         "0.5",
		Defense:       "100",
		Score:         "30",
	}

	PlanetLevel2Stats = PlanetLevelStats{
		Level:         2,
		EnergyDefault: "20",
		EnergyMax:     "1200",
		EnergyRefill:  "300",
		Range:         "60",
		Speed:         "0.25",
		Defense:       "95",
		Score:         "80",
	}

	PlanetLevel3Stats = PlanetLevelStats{
		Level:         3,
		EnergyDefault: "140",
		EnergyMax:     "3000",
		EnergyRefill:  "600",
		Range:         "80",
		Speed:         "0.15",
		Defense:       "90",
		Score:         "150",
	}

	PlanetLevel4Stats = PlanetLevelStats{
		Level:         4,
		EnergyDefault: "1000",
		EnergyMax:     "8000",
		EnergyRefill:  "900",
		Range:         "100",
		Speed:         "0.1",
		Defense:       "85",
		Score:         "250",
	}

	PlanetLevel5Stats = PlanetLevelStats{
		Level:         5,
		EnergyDefault: "4000",
		EnergyMax:     "20000",
		EnergyRefill:  "1800",
		Range:         "140",
		Speed:         "0.09",
		Defense:       "80",
		Score:         "400",
	}

	PlanetLevel6Stats = PlanetLevelStats{
		Level:         6,
		EnergyDefault: "10000",
		EnergyMax:     "50000",
		EnergyRefill:  "3600",
		Range:         "200",
		Speed:         "0.08",
		Defense:       "70",
		Score:         "800",
	}

	PlanetLevel7Stats = PlanetLevelStats{
		Level:         7,
		EnergyDefault: "35000",
		EnergyMax:     "500000",
		EnergyRefill:  "21600",
		Range:         "55",
		Speed:         "0.57",
		Defense:       "200",
		Score:         "0",
	}

	PlanetLevel8Stats = PlanetLevelStats{
		Level:         8,
		EnergyDefault: "56000",
		EnergyMax:     "700000",
		EnergyRefill:  "28800",
		Range:         "60",
		Speed:         "0.58",
		Defense:       "150",
		Score:         "0",
	}

	PlanetLevel9Stats = PlanetLevelStats{
		Level:         9,
		EnergyDefault: "72000",
		EnergyMax:     "800000",
		EnergyRefill:  "36000",
		Range:         "65",
		Speed:         "0.59",
		Defense:       "150",
		Score:         "0",
	}

	PlanetLevel10Stats = PlanetLevelStats{
		Level:         10,
		EnergyDefault: "100000",
		EnergyMax:     "1000000",
		EnergyRefill:  "43200",
		Range:         "70",
		Speed:         "0.6",
		Defense:       "100",
		Score:         "0",
	}
)
