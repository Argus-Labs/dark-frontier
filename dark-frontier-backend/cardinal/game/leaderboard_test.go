package game

import (
	"context"
	"github.com/alicebob/miniredis/v2"
	"github.com/redis/go-redis/v9"
	"github.com/stretchr/testify/assert"
	"testing"
)

func setupMockRedis() (*miniredis.Miniredis, *redis.Client) {
	mr, _ := miniredis.Run()
	options := &redis.Options{
		Addr: mr.Addr(),
	}
	client := redis.NewClient(options)
	return mr, client
}

func TestAddPlayerToLeaderboard(t *testing.T) {
	ctx := context.TODO()

	mr, client := setupMockRedis()
	defer mr.Close()
	LeaderboardClient = client

	player := Player{PersonaTag: "Alice", Score: 1000}
	err := AddPlayerToLeaderboard(ctx, player)

	assert.Nil(t, err, "Error adding player to leaderboard")

	rank, score, err := GetPlayerRankAndScore(ctx, "Alice")
	assert.Nil(t, err, "Error getting player rank and score")
	assert.Equal(t, int64(1), rank, "Rank mismatch")
	assert.Equal(t, 1000.0, score, "Score mismatch")
}
func TestSetPlayerScore(t *testing.T) {
	ctx := context.TODO()

	mr, client := setupMockRedis()
	defer mr.Close()
	LeaderboardClient = client

	// Adding "Alice" to the leaderboardKey
	player := Player{PersonaTag: "Alice", Score: 1000}
	err := AddPlayerToLeaderboard(ctx, player)
	assert.Nil(t, err, "Error adding player to leaderboard")

	// Update "Alice"'s score
	err = SetScore(ctx, "Alice", 1200)
	assert.Nil(t, err, "Error updating player score")

	// Get "Alice"'s rank and score
	rank, score, err := GetPlayerRankAndScore(ctx, "Alice")
	assert.Nil(t, err, "Error getting player rank and score")
	assert.Equal(t, int64(1), rank, "Rank mismatch")
	assert.Equal(t, 1200.0, score, "Score mismatch")
}

func TestGetPlayersInRankRange(t *testing.T) {
	ctx := context.TODO()

	mr, client := setupMockRedis()
	defer mr.Close()
	LeaderboardClient = client

	// Adding players to the leaderboard
	player1 := Player{PersonaTag: "Alice", Score: 1000}
	err := AddPlayerToLeaderboard(ctx, player1)
	assert.Nil(t, err, "Error adding player to leaderboard")

	player2 := Player{PersonaTag: "Bob", Score: 750}
	err = AddPlayerToLeaderboard(ctx, player2)
	assert.Nil(t, err, "Error adding player to leaderboard")

	player3 := Player{PersonaTag: "Charlie", Score: 1200}
	err = AddPlayerToLeaderboard(ctx, player3)
	assert.Nil(t, err, "Error adding player to leaderboard")

	// Get players in rank range
	players, err := GetPlayersInRankRange(ctx, 0, 2)
	assert.Nil(t, err, "Error getting players in rank range")

	expected := []RankedPlayer{
		{
			Player: Player{
				PersonaTag: "Charlie",
				Score:      1200,
			},
			Rank: 1,
		},
		{
			Player: Player{
				PersonaTag: "Alice",
				Score:      1000,
			},
			Rank: 2,
		},
		{
			Player: Player{
				PersonaTag: "Bob",
				Score:      750,
			},
			Rank: 3,
		},
	}

	assert.ElementsMatch(t, expected, players, "Leaderboard mismatch")
}

func TestIncrementScore(t *testing.T) {
	ctx := context.TODO()

	mr, client := setupMockRedis()
	defer mr.Close()
	LeaderboardClient = client

	player := Player{PersonaTag: "Alice", Score: 1000}
	err := AddPlayerToLeaderboard(ctx, player)
	assert.Nil(t, err, "Error adding player to leaderboard")

	err = IncrementScore(ctx, "Alice", 500)
	assert.Nil(t, err, "Error incrementing player's score")

	rank, score, err := GetPlayerRankAndScore(ctx, "Alice")
	assert.Nil(t, err, "Error getting player rank and score")
	assert.Equal(t, int64(1), rank, "Rank mismatch")
	assert.Equal(t, 1500.0, score, "Score mismatch")
}

func TestDecrementScore(t *testing.T) {
	ctx := context.TODO()

	mr, client := setupMockRedis()
	defer mr.Close()
	LeaderboardClient = client

	player := Player{PersonaTag: "Alice", Score: 1000}
	err := AddPlayerToLeaderboard(ctx, player)
	assert.Nil(t, err, "Error adding player to leaderboard")

	err = DecrementScore(ctx, "Alice", 500)
	assert.Nil(t, err, "Error decrementing player's score")

	rank, score, err := GetPlayerRankAndScore(ctx, "Alice")
	assert.Nil(t, err, "Error getting player rank and score")
	assert.Equal(t, int64(1), rank, "Rank mismatch")
	assert.Equal(t, 500.0, score, "Score mismatch")
}

func TestGetPlayerRankAndScoreNotFound(t *testing.T) {
	ctx := context.TODO()

	mr, client := setupMockRedis()
	defer mr.Close()
	LeaderboardClient = client

	_, _, err := GetPlayerRankAndScore(ctx, "NonExistentPlayer")
	assert.Error(t, err, "Expected error for player not found")
	assert.Contains(t, err.Error(), "not found in leaderboard", "Error message mismatch")
}

func TestSetScoreWithNegativeScore(t *testing.T) {
	ctx := context.TODO()

	mr, client := setupMockRedis()
	defer mr.Close()
	LeaderboardClient = client

	player := Player{PersonaTag: "Alice", Score: 1000}
	err := AddPlayerToLeaderboard(ctx, player)
	assert.Nil(t, err, "Error adding player to leaderboard")

	err = SetScore(ctx, "Alice", -500)
	assert.Nil(t, err, "Error setting negative score")

	rank, score, err := GetPlayerRankAndScore(ctx, "Alice")
	assert.Nil(t, err, "Error getting player rank and score")
	assert.Equal(t, int64(1), rank, "Rank mismatch")
	assert.Equal(t, -500.0, score, "Score mismatch")
}

func TestGetPlayersInRankRangeWithOverlap(t *testing.T) {
	ctx := context.TODO()

	mr, client := setupMockRedis()
	defer mr.Close()
	LeaderboardClient = client

	player1 := Player{PersonaTag: "Alice", Score: 1000}
	err := AddPlayerToLeaderboard(ctx, player1)
	assert.Nil(t, err, "Error adding player to leaderboard")

	player2 := Player{PersonaTag: "Bob", Score: 1000}
	err = AddPlayerToLeaderboard(ctx, player2)
	assert.Nil(t, err, "Error adding player to leaderboard")

	player3 := Player{PersonaTag: "Charlie", Score: 1200}
	err = AddPlayerToLeaderboard(ctx, player3)
	assert.Nil(t, err, "Error adding player to leaderboard")

	players, err := GetPlayersInRankRange(ctx, 0, 2)
	assert.Nil(t, err, "Error getting players in rank range")

	// Note: If two players are tied, the player with the alphabetically
	// secondary PersonaTag will have the higher rank.
	expected := []RankedPlayer{
		{
			Player: Player{
				PersonaTag: "Charlie",
				Score:      1200,
			},
			Rank: 1,
		},
		{
			Player: Player{
				PersonaTag: "Bob",
				Score:      1000,
			},
			Rank: 2,
		},
		{
			Player: Player{
				PersonaTag: "Alice",
				Score:      1000,
			},
			Rank: 3,
		},
	}

	assert.ElementsMatch(t, expected, players, "Leaderboard mismatch")
}
