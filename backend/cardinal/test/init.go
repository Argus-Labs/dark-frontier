package utils

import (
	"github.com/consensys/gnark-crypto/ecc"
	"github.com/consensys/gnark/backend/groth16"
)

type NewPlanetInfo struct {
	Level        string
	X            string
	Y            string
	LocationHash string
	Perlin       int64
}

var (
	InitProvingKey = groth16.NewProvingKey(ecc.BN254)
	InitCCS        = groth16.NewCS(ecc.BN254)
	MoveProvingKey = groth16.NewProvingKey(ecc.BN254)
	MoveCCS        = groth16.NewCS(ecc.BN254)

	startRange = NewPlanetInfo{
		Level:        "2",
		X:            "-13",
		Y:            "0",
		LocationHash: "0c00eab1ba08a68b0bc4767349a3f3c295011a54127e4713920318d6fbc6b662",
		Perlin:       14,
	}

	inRange = NewPlanetInfo{
		Level:        "2",
		X:            "-21",
		Y:            "5",
		LocationHash: "23014b713b8c41d5116c6334c9daf595d8a23edaa06a21dee833ca79f3452344",
		Perlin:       16,
	}

	outOfRange = NewPlanetInfo{
		Level:        "7",
		X:            "6",
		Y:            "-21",
		LocationHash: "2b0173c32d6d3c7526dc27aeb69791ea79d19b28571325d4f99d31cd17cb8062",
		Perlin:       18,
	}

	levelTwoPlanet = NewPlanetInfo{
		Level:        "2",
		X:            "3",
		Y:            "-3",
		LocationHash: "0d01f8778431d6f04310bf3f02fb6e85a173624241fc8d8185c528306f57ca68",
		Perlin:       16,
	}

	levelTwoPlanetTwo = NewPlanetInfo{
		Level:        "2",
		X:            "11",
		Y:            "-10",
		LocationHash: "0d00974b8d6df596c0eaf0c8fb15a45dc2da397c3f2d2107661feea02d200e27",
		Perlin:       16,
	}

	levelZeroPlanet = NewPlanetInfo{
		Level:        "0",
		X:            "0",
		Y:            "22",
		LocationHash: "10012a1b9bb877625c03c2d253dc4b18a9171162c10b70870c38fb16b67f1251",
		Perlin:       11,
	}

	levelZeroPlanetTwo = NewPlanetInfo{
		Level:        "0",
		X:            "-29",
		Y:            "28",
		LocationHash: "0b00e8083a5ad331e3e5fc97c6ed06ffbdb2c9fb292974ce57a25cf5a5b6b668",
		Perlin:       14,
	}
)
