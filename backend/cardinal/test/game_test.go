package utils

import (
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"github.com/argus-labs/darkfrontier-backend/cardinal/utils"
	"github.com/ericlagergren/decimal"
	"gotest.tools/v3/assert"
	"testing"
)

// Check that correct planet level is returned
func TestGetPlanetByLocationHash(t *testing.T) {
	testCases := []struct {
		locationHash   string
		perlin         int64
		expectedPlanet *game.PlanetLevelStats
	}{
		{
			locationHash: "0c00eab1ba08a68b0bc4767349a3f3c295011a54127e4713920318d6fbc6b662",
			perlin:       14,
			expectedPlanet: &game.PlanetLevelStats{
				Level:         1,
				EnergyDefault: "4",
				EnergyMax:     "600",
				EnergyRefill:  "300",
				Range:         "25",
				Speed:         "0.51",
				Defense:       "300",
				Score:         "30",
			},
		},
		// TODO: add more cases.
	}

	for _, tc := range testCases {
		t.Run(tc.locationHash, func(t *testing.T) {
			planet, err := utils.GetPlanetStatsByLocationHash(tc.locationHash, tc.perlin)
			assert.NilError(t, err)
			assert.DeepEqual(t, tc.expectedPlanet, planet)
		})
	}

}

func TestGetSpaceArea(t *testing.T) {
	// Check that correct space area is returned
	// Nebula
	testCases := []struct {
		name                  string
		perlin                int64
		expectedSpaceConstant game.SpaceConstant
	}{
		{
			name:                  "Safe Space",
			perlin:                14,
			expectedSpaceConstant: *game.SpaceConstants[0],
		},
		{
			name:                  "Safe Space",
			perlin:                15,
			expectedSpaceConstant: *game.SpaceConstants[1],
		},
		{
			name:                  "Safe Space",
			perlin:                16,
			expectedSpaceConstant: *game.SpaceConstants[1],
		},
		{
			name:                  "Deep Space",
			perlin:                17,
			expectedSpaceConstant: *game.SpaceConstants[2],
		},
		{
			name:                  "Deep Space",
			perlin:                18,
			expectedSpaceConstant: *game.SpaceConstants[2],
		},
	}
	for _, tc := range testCases {
		t.Run(tc.name, func(t *testing.T) {
			res := utils.GetSpaceArea(tc.perlin)
			assert.Equal(t, tc.expectedSpaceConstant, res)
		})
	}
}

func TestSaturate(t *testing.T) {
	testCases := []struct {
		name     string
		input    *decimal.Big
		expected *decimal.Big
	}{
		{
			name:     "should be 1",
			input:    decimal.New(2, 0),
			expected: utils.OneDec,
		},
		{
			name:     "should be 0",
			input:    decimal.New(-2, 0),
			expected: utils.ZeroDec,
		},
		{
			name:     "should be the value itself",
			input:    decimal.New(3, 10),
			expected: decimal.New(3, 10),
		},
	}

	for _, tc := range testCases {
		t.Run(tc.name, func(t *testing.T) {
			got := utils.Saturate(tc.input)
			comp := got.Cmp(tc.expected)
			assert.Equal(t, comp, 0)
		})
	}
}
