package utils

import (
	"fmt"
	"github.com/argus-labs/darkfrontier-backend/cardinal/game"
	"math"
	"strconv"

	"github.com/ericlagergren/decimal"
)

var (
	ZeroDec = decimal.New(0, 0)
	OneDec  = decimal.New(1, 0)
)

// Saturate returns 0 if value is less than 0, 1 if value is greater than 1, and value otherwise
func Saturate(value *decimal.Big) *decimal.Big {
	if value.Cmp(ZeroDec) == -1 {
		// if value is less than 0, return 0
		return ZeroDec
	} else if value.Cmp(OneDec) == 1 {
		// if value is greater than 1, return 1
		return OneDec
	} else {
		// otherwise return value
		return value
	}
}

// NormalizedRefillAge returns the normalized refill age of a planet at a given time
func NormalizedRefillAge(
	normalizedRefillStartingAge *decimal.Big,
	refillStartTick *decimal.Big,
	currentTick *decimal.Big,
	energyRefillPeriod *decimal.Big,
) *decimal.Big {
	timeDelta := new(decimal.Big).Sub(currentTick, refillStartTick)
	return Saturate(new(decimal.Big).Add(normalizedRefillStartingAge, new(decimal.Big).Quo(timeDelta, energyRefillPeriod)))
}

// ShipArrivalTick returns the travel time of a ship
func ShipArrivalTick(
	distance *decimal.Big,
	speed *decimal.Big,
	currentTick int64,
) int64 {
	// Warn: precision loss
	return DecToInt64(new(decimal.Big).Quo(distance, speed)) + currentTick
}

// EnergyLevel returns the energy level of a planet at a given time
func EnergyLevel(
	energyCapacity *decimal.Big,
	normalizedRefillAge *decimal.Big,
) *decimal.Big {
	t := normalizedRefillAge
	// makes the energy curve more exponential
	t = energyCurve(energyCurve(t))
	return new(decimal.Big).Mul(Saturate(t), energyCapacity)
}

func energyCurve(
	t *decimal.Big,
) *decimal.Big {
	// t = t * t * (3.0 - (2.0 * t));
	tp := new(decimal.Big).Mul(t, t)
	return new(decimal.Big).Mul(tp, new(decimal.Big).Sub(decimal.New(3, 0), new(decimal.Big).Mul(decimal.New(2, 0), t)))
}

func InvEnergyCurve(
	t *decimal.Big,
) *decimal.Big {
	// 0.5 - sin(arcsin(1-2t)/3)
	var asinv, sinv decimal.Big
	ctx := decimal.Context{
		Precision: 100,
	}

	arg := new(decimal.Big).Sub(decimal.New(1, 0), new(decimal.Big).Mul(decimal.New(2, 0), t))
	ctx.Asin(&asinv, arg)
	ctx.Sin(&sinv, new(decimal.Big).Quo(&asinv, decimal.New(3, 0)))
	return new(decimal.Big).Sub(StrToDec("0.5"), &sinv)
}

// EnergyOnEmbark calculates the energy that will be on the ship when it embarks
// do note, that the energy on arrival might be different due to the defense debuff
func EnergyOnEmbark(energySent *decimal.Big, sourcePlanetMaxEnergy *decimal.Big, distance *decimal.Big, planetRange *decimal.Big) *decimal.Big {
	flatCost := new(decimal.Big).Mul(sourcePlanetMaxEnergy, StrToDec("0.05"))
	costPerDistance := new(decimal.Big).Quo(new(decimal.Big).Mul(sourcePlanetMaxEnergy, StrToDec("0.95")), planetRange)
	totalTravelCost := new(decimal.Big).Add(new(decimal.Big).Mul(distance, costPerDistance), flatCost)
	result := new(decimal.Big).Sub(energySent, totalTravelCost)
	return result
}

// EnergyOnArrivalAtFriendlyPlanet calculates the energy that will be added to friendly planet when a ship arrive
// do note that this is currently just an identity function and is only here for readability
func EnergyOnArrivalAtFriendlyPlanet(energyOnEmbark *decimal.Big) *decimal.Big {
	return energyOnEmbark
}

// EnergyAfterDefenseDebuff calculates the energy that will be subtracted from enemy or unclaimed planet when a ship arrives
// which takes into account the enemy planet's defense debuff
func EnergyAfterDefenseDebuff(energyOnEmbark *decimal.Big, destinationPlanetDefense *decimal.Big) *decimal.Big {
	temp := new(decimal.Big).Quo(energyOnEmbark, destinationPlanetDefense)
	return new(decimal.Big).Mul(temp, StrToDec("100"))
}

// GetSpaceArea Note(Scott): Client equivalent
func GetSpaceArea(perlin int64) game.SpaceConstant {
	spaceConstants := game.SpaceConstant{}
	if perlin < game.WorldConstants.SpacePerlinThresholds[0] { // if perlin < 15
		spaceConstants = *game.SpaceConstants[0] // NebulaSpaceConstants
	} else if perlin < game.WorldConstants.SpacePerlinThresholds[1] { // if perlin < 17
		spaceConstants = *game.SpaceConstants[1] // SafeSpaceConstants
	} else {
		spaceConstants = *game.SpaceConstants[2] // DeepSpaceConstants
	}

	return spaceConstants
}

func SpaceAreaToInt(spaceArea game.SpaceConstant) int64 {
	switch spaceArea.Label {
	case "Nebula":
		return 1
	case "SafeSpace":
		return 2
	case "DeepSpace":
		return 3
	default:
		return 0
	}
}

// GetIntFromHex Converts hex string to int
func GetIntFromHex(hex string) (*decimal.Big, error) {
	result, err := strconv.ParseInt(hex, 16, 64)
	if err != nil {
		return nil, err
	}
	return IntToDec(int(result)), nil
}

func GetPlanetStatsByLocationHash(locationHash string, perlin int64) (*game.PlanetLevelStats, error) {
	// Calculate the int representation of byte 2, 3, 4, 5 locationHash, a hex string
	hashOffset := 2
	planetSpawnInt, err := GetIntFromHex(locationHash[hashOffset : hashOffset+4])
	if err != nil {
		return nil, err
	}
	planetSpawnNoise := new(decimal.Big).Quo(planetSpawnInt, IntToDec(math.MaxUint16))

	hashOffset += 4
	planetLevelInt, err := GetIntFromHex(locationHash[hashOffset : hashOffset+2])
	if err != nil {
		return nil, err
	}
	planetLevelNoise := new(decimal.Big).Quo(planetLevelInt, IntToDec(math.MaxUint8))

	stats, err := GetPlanetStats(planetSpawnNoise, planetLevelNoise, perlin)
	if err != nil {
		return nil, err
	}
	return stats, nil
}

func GetPlanetStats(planetSpawnNoise *decimal.Big, planetLevelNoise *decimal.Big, perlin int64) (*game.PlanetLevelStats, error) {
	// Obtain the space area based on the perlin value
	space := GetSpaceArea(perlin)

	if planetSpawnNoise.Cmp(StrToDec(space.PlanetSpawnThreshold)) > 0 {
		// planet spawn byte is not within the threshold for that spawn area
		return nil, fmt.Errorf("planet spawn byte not within threshhold for spawn area")
	}

	// Obtain the planet level based on the planet level byte
	baseStats, ok := getPlanetBaseStats(planetLevelNoise, space)
	if !ok {
		return nil, fmt.Errorf("got invalid planetLevelNoise for planet generation")
	}
	return GetSpaceAdjustedPlanetStats(baseStats, space), nil
}

func getPlanetBaseStats(planetLevelNoise *decimal.Big, space game.SpaceConstant) (game.PlanetLevelStats, bool) {
	// Obtain the planet level based on the planet level byte
	for i, planetLevelThreshold := range space.PlanetLevelThreshold {
		// Enter if statement if planetLevelNoise and planetLevelThreshold are equal
		if planetLevelNoise.Cmp(StrToDec(planetLevelThreshold)) <= 0 {
			return *game.BasePlanetLevelStats[i], true
		}
	}

	return game.PlanetLevelStats{}, false
}

func GetSpaceAdjustedPlanetStats(planetStats game.PlanetLevelStats, space game.SpaceConstant) *game.PlanetLevelStats {
	// Modify the planet level stats based on the space area
	statBuffMul := StrToDec(space.StatBuffMultiplier)
	defenseDebuffMul := StrToDec(space.DefenseDebuffMultiplier)

	planetStats.EnergyMax = DecToStr(new(decimal.Big).Mul(StrToDec(planetStats.EnergyMax), statBuffMul))
	planetStats.EnergyRefill = DecToStr(new(decimal.Big).Mul(StrToDec(planetStats.EnergyRefill), statBuffMul))
	planetStats.Range = DecToStr(new(decimal.Big).Mul(StrToDec(planetStats.Range), statBuffMul))
	planetStats.Speed = DecToStr(new(decimal.Big).Mul(StrToDec(planetStats.Speed), statBuffMul))
	planetStats.Defense = DecToStr(new(decimal.Big).Mul(StrToDec(planetStats.Defense), defenseDebuffMul))

	return &planetStats
}

func ScaleUpByTickRate(value *decimal.Big) *decimal.Big {
	tickRate := IntToDec(game.WorldConstants.TickRate)
	return new(decimal.Big).Mul(value, tickRate)
}

func ScaleDownByTickRate(value *decimal.Big) *decimal.Big {
	tickRate := IntToDec(game.WorldConstants.TickRate)
	return new(decimal.Big).Quo(value, tickRate)
}

func ScaleDownByTickRateInt(value int64) int64 {
	valueDec := Int64ToDec(value)
	tickRate := IntToDec(game.WorldConstants.TickRate)
	resultDec := new(decimal.Big).Quo(valueDec, tickRate)
	return DecToInt64(resultDec)
}
