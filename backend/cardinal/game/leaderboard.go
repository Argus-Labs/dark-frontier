package game

import (
	"context"
	"fmt"
	"github.com/redis/go-redis/v9"
)

type Player struct {
	PersonaTag string `json:"personaTag"`
	Score      int    `json:"score"`
}

type RankedPlayer struct {
	Player
	Rank int `json:"rank"`
}

const leaderboardKey = "leaderboardKey"

var LeaderboardClient *redis.Client

func AddPlayerToLeaderboard(ctx context.Context, player Player) error {
	z := &redis.Z{
		Score:  float64(player.Score),
		Member: player.PersonaTag,
	}

	_, err := LeaderboardClient.ZAdd(ctx, leaderboardKey, *z).Result()
	return err
}

func IncrementScore(ctx context.Context, personaTag string, amount int) error {
	_, err := LeaderboardClient.ZIncrBy(ctx, leaderboardKey, float64(amount), personaTag).Result()
	return err
}

func DecrementScore(ctx context.Context, personaTag string, amount int) error {
	// Use a negative amount to decrement the score
	_, err := LeaderboardClient.ZIncrBy(ctx, leaderboardKey, float64(-amount), personaTag).Result()
	return err
}

func SetScore(ctx context.Context, personaTag string, newScore int) error {
	// Get the current score
	currentScore, err := LeaderboardClient.ZScore(ctx, leaderboardKey, personaTag).Result()
	if err != nil {
		return err
	}

	// Calculate the difference between the new score and the current score
	scoreDifference := float64(newScore) - currentScore

	// Increment the member's score to reach the desired value
	_, err = LeaderboardClient.ZIncrBy(ctx, leaderboardKey, scoreDifference, personaTag).Result()
	return err
}

func GetPlayerRankAndScore(ctx context.Context, personaTag string) (int64, float64, error) {
	rank, err := LeaderboardClient.ZRevRank(ctx, leaderboardKey, personaTag).Result()
	if err != nil {
		return -1, 0, fmt.Errorf("player %s not found in leaderboard err: %v", personaTag, err)
	}

	score, err := LeaderboardClient.ZScore(ctx, leaderboardKey, personaTag).Result()
	if err != nil {
		return -1, 0, err
	}

	return rank + 1, score, nil // Adding 1 to the rank since it's 0-based
}

func GetPlayersInRankRange(ctx context.Context, startRank, endRank int64) ([]RankedPlayer, error) {
	leaderboard, err := LeaderboardClient.ZRevRangeWithScores(ctx, leaderboardKey, startRank, endRank).Result()
	if err != nil {
		return nil, err
	}

	players := make([]RankedPlayer, len(leaderboard))
	for i, z := range leaderboard {
		players[i] = RankedPlayer{
			Player: Player{
				PersonaTag: z.Member.(string),
				Score:      int(z.Score),
			},
			Rank: i + 1,
		}
	}

	return players, nil
}
