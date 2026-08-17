using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Players;
using ProBasketballManager.Domain.Tactics;
using ProBasketballManager.Domain.Teams;

namespace ProBasketballManager.Domain.Matches
{
    public sealed class MatchSimulator
    {
        // ---- Pace -------------------------------------------------------
        // Possessions generated per period, counting BOTH teams. FIBA rules give
        // 40 minutes of regulation, and roughly 75 possessions per team, so a
        // regulation period should produce about 38 combined possessions.
        private const int MinimumPeriodPossessions = 34;
        private const int MaximumPeriodPossessions = 42;
        private const int MinimumOvertimePossessions = 17;
        private const int MaximumOvertimePossessions = 21;

        // ---- Shot efficiency --------------------------------------------
        // Baseline conversion rate per zone for an average player guarded by an average defender under neutral tactics.
        private const double AtRimBaseChance = 0.560;
        private const double PaintBaseChance = 0.430;
        private const double MidRangeBaseChance = 0.383;
        private const double CornerThreeBaseChance = 0.352;
        private const double AboveBreakThreeBaseChance = 0.328;

        private const double MinimumShotChance = 0.25;
        private const double MaximumShotChance = 0.68;

        // ---- Talent sensitivity -----------------------------------------
        // How much one point of attribute rating moves a single shot. These are deliberately small: the effect compounds over ~75 possessions, so large
        // values make roster quality overwhelm every other factor in the game.
        private const double AtRimOffenseWeight = 0.0017;
        private const double AtRimDefenseWeight = 0.0014;
        private const double PaintOffenseWeight = 0.0016;
        private const double PaintDefenseWeight = 0.0011;
        private const double MidRangeOffenseWeight = 0.0015;
        private const double MidRangeDefenseWeight = 0.0010;
        private const double ThreePointOffenseWeight = 0.0014;
        private const double ThreePointDefenseWeight = 0.0009;

        private const double AverageAttributeRating = 10.5;

        private readonly IRandomSource _random;

        public MatchSimulator(IRandomSource random)
        {
            _random = random;
        }

        public MatchResult Simulate(Team homeTeam, Team awayTeam)
        {
            return Simulate(homeTeam, awayTeam, TeamRotation.CreateDefault(homeTeam), TeamRotation.CreateDefault(awayTeam), TeamTactics.Default, TeamTactics.Default);
        }

        public MatchResult Simulate(Team homeTeam, Team awayTeam, TeamTactics homeTactics, TeamTactics awayTactics)
        {
            return Simulate(homeTeam, awayTeam, TeamRotation.CreateDefault(homeTeam), TeamRotation.CreateDefault(awayTeam), homeTactics, awayTactics);
        }

        public MatchResult Simulate(Team homeTeam, Team awayTeam, TeamRotation homeRotation, TeamRotation awayRotation, TeamTactics homeTactics, TeamTactics awayTactics)
        {
            if (homeTeam.Id == awayTeam.Id)
            {
                throw new ArgumentException("A team cannot play against itself.");
            }

            if (homeRotation.Team.Id != homeTeam.Id)
            {
                throw new ArgumentException("The home rotation does not belong to the home team.");
            }

            if (awayRotation.Team.Id != awayTeam.Id)
            {
                throw new ArgumentException("The away rotation does not belong to the away team.");
            }

            var homePeriodScores = new List<int>();
            var awayPeriodScores = new List<int>();
            var events = new List<MatchEvent>();

            var homeStats = CreateStatBuilders(homeTeam, homeRotation);
            var awayStats = CreateStatBuilders(awayTeam, awayRotation);

            var homeRotationRuntime = new RotationRuntime(homeRotation);
            var awayRotationRuntime = new RotationRuntime(awayRotation);

            var homeScore = 0;
            var awayScore = 0;

            for (var periodNumber = 1; periodNumber <= 4; periodNumber++)
            {
                var homeScoreBeforePeriod = homeScore;
                var awayScoreBeforePeriod = awayScore;

                SimulatePeriod(homeTeam, awayTeam, homeRotation, awayRotation, homeRotationRuntime, awayRotationRuntime, homeTactics, awayTactics, periodNumber, 600, events, homeStats, awayStats, ref homeScore, ref awayScore);

                homePeriodScores.Add(homeScore - homeScoreBeforePeriod);
                awayPeriodScores.Add(awayScore - awayScoreBeforePeriod);
            }

            var overtimePeriodNumber = 5;

            while (homeScore == awayScore)
            {
                var homeScoreBeforePeriod = homeScore;
                var awayScoreBeforePeriod = awayScore;

                SimulatePeriod(homeTeam, awayTeam, homeRotation, awayRotation, homeRotationRuntime, awayRotationRuntime, homeTactics, awayTactics, overtimePeriodNumber, 300, events, homeStats, awayStats, ref homeScore, ref awayScore);

                homePeriodScores.Add(homeScore - homeScoreBeforePeriod);
                awayPeriodScores.Add(awayScore - awayScoreBeforePeriod);

                overtimePeriodNumber++;
            }

            return new MatchResult(
                homeTeam,
                awayTeam,
                homeRotation,
                awayRotation,
                homePeriodScores,
                awayPeriodScores,
                events,
                BuildBoxScores(homeTeam, homeStats),
                BuildBoxScores(awayTeam, awayStats)
            );
        }

        private void SimulatePeriod(Team homeTeam, Team awayTeam, TeamRotation homeRotation, TeamRotation awayRotation, RotationRuntime homeRotationRuntime, RotationRuntime awayRotationRuntime, TeamTactics homeTactics, TeamTactics awayTactics, int periodNumber, int periodLengthSeconds, List<MatchEvent> events, Dictionary<int, PlayerBoxScoreBuilder> homeStats, Dictionary<int, PlayerBoxScoreBuilder> awayStats, ref int homeScore, ref int awayScore)
        {
            var basePossessions = periodNumber <= 4
                ? _random.NextInt(MinimumPeriodPossessions, MaximumPeriodPossessions)
                : _random.NextInt(MinimumOvertimePossessions, MaximumOvertimePossessions);

            var averagePace = (homeTactics.Pace + awayTactics.Pace) / 2.0;
            var paceMultiplier = 0.85 + ((averagePace / 100.0) * 0.30);

            var totalPossessions = Math.Max(10, (int)Math.Round(basePossessions * paceMultiplier));
            var possessionDuration = periodLengthSeconds / (double)totalPossessions;

            var homeStarts = _random.NextDouble() < 0.5;

            for (var possessionIndex = 0; possessionIndex < totalPossessions; possessionIndex++)
            {
                var possessionStartSeconds = Math.Max(0, periodLengthSeconds - (int)Math.Round((possessionIndex / (double)totalPossessions) * periodLengthSeconds));
                var secondsRemaining = Math.Max(0, periodLengthSeconds - (int)Math.Round(((possessionIndex + 1) / (double)totalPossessions) * periodLengthSeconds));

                var homePlayers = homeRotationRuntime.GetOnCourtPlayers(periodNumber, periodLengthSeconds, possessionStartSeconds);
                var awayPlayers = awayRotationRuntime.GetOnCourtPlayers(periodNumber, periodLengthSeconds, possessionStartSeconds);

                AddPlayingTime(homePlayers, possessionDuration, homeStats);
                AddPlayingTime(awayPlayers, possessionDuration, awayStats);

                homeRotationRuntime.AddPlayingTime(homePlayers, possessionDuration);
                awayRotationRuntime.AddPlayingTime(awayPlayers, possessionDuration);

                var homePossession = homeStarts ? possessionIndex % 2 == 0 : possessionIndex % 2 != 0;

                var attackingTeam = homePossession ? homeTeam : awayTeam;
                var defendingTeam = homePossession ? awayTeam : homeTeam;

                var attackingPlayers = homePossession ? homePlayers : awayPlayers;
                var defendingPlayers = homePossession ? awayPlayers : homePlayers;

                var attackingStats = homePossession ? homeStats : awayStats;
                var defendingStats = homePossession ? awayStats : homeStats;

                var attackingTactics = homePossession ? homeTactics : awayTactics;
                var defendingTactics = homePossession ? awayTactics : homeTactics;

                var attackingRotation = homePossession ? homeRotation : awayRotation;

                SimulatePossession(attackingTeam, defendingTeam, attackingPlayers, defendingPlayers, attackingRotation, attackingTactics, defendingTactics, homePossession, attackingStats, defendingStats, periodNumber, secondsRemaining, events, ref homeScore, ref awayScore);
            }
        }

        private void SimulatePossession(Team attackingTeam, Team defendingTeam, IReadOnlyList<Player> attackingPlayers, IReadOnlyList<Player> defendingPlayers, TeamRotation attackingRotation, TeamTactics attackingTactics, TeamTactics defendingTactics, bool homePossession, Dictionary<int, PlayerBoxScoreBuilder> attackingStats, Dictionary<int, PlayerBoxScoreBuilder> defendingStats, int periodNumber, int secondsRemaining, List<MatchEvent> events, ref int homeScore, ref int awayScore)
        {
            var actionTime = secondsRemaining;

            var ballHandler = SelectBallHandler(attackingPlayers, attackingRotation);

            if (SimulateTurnover(ballHandler, attackingTeam, defendingTeam, defendingPlayers, defendingTactics, attackingStats, defendingStats, periodNumber, ref actionTime, events, homeScore, awayScore))
            {
                return;
            }

            const int maximumShotSequences = 4;

            for (var shotSequence = 0; shotSequence < maximumShotSequences; shotSequence++)
            {
                var shot = shotSequence == 0
     ? SelectShot(attackingPlayers, ballHandler, attackingRotation, attackingTactics)
     : SelectShotAfterOffensiveRebound(attackingPlayers, attackingRotation, attackingTactics);

                var defender = SelectShotDefender(shot.Shooter, defendingPlayers, shot.Zone);

                if (SimulateShootingFoul(shot, defender, attackingTeam, defendingTactics, homePossession, attackingStats, defendingStats, periodNumber, ref actionTime, events, ref homeScore, ref awayScore))
                {
                    return;
                }

                var made = SimulateFieldGoal(shot, defender, defendingTactics, homePossession);

                RecordFieldGoalAttempt(attackingStats[shot.Shooter.Id], shot.Zone, made);

                if (made)
                {
                    var points = IsThreePointZone(shot.Zone) ? 3 : 2;

                    AddPoints(homePossession, points, ref homeScore, ref awayScore);

                    var assister = TryCreateAssist(shot, attackingPlayers, attackingTactics, attackingStats);

                    AddEvent(
                        events,
                        periodNumber,
                        ref actionTime,
                        IsThreePointZone(shot.Zone) ? MatchEventType.MadeThreePointer : MatchEventType.MadeTwoPointer,
                        attackingTeam,
                        shot.Shooter,
                        assister,
                        homeScore,
                        awayScore,
                        shot.Action,
                        shot.Zone
                    );

                    return;
                }

                AddEvent(
                    events,
                    periodNumber,
                    ref actionTime,
                    IsThreePointZone(shot.Zone) ? MatchEventType.MissedThreePointer : MatchEventType.MissedTwoPointer,
                    attackingTeam,
                    shot.Shooter,
                    null,
                    homeScore,
                    awayScore,
                    shot.Action,
                    shot.Zone
                );

                var allowOffensiveRebound = shotSequence < maximumShotSequences - 1;

                if (!SimulateRebound(attackingTeam, defendingTeam, attackingPlayers, defendingPlayers, attackingStats, defendingStats, allowOffensiveRebound, periodNumber, ref actionTime, events, homeScore, awayScore))
                {
                    return;
                }
            }
        }

        private ShotSelection SelectShot(IReadOnlyList<Player> attackingPlayers, Player ballHandler, TeamRotation rotation, TeamTactics tactics)
        {
            var primaryScorer = attackingPlayers.FirstOrDefault(player => player.Id == rotation.PrimaryScorer.Id);

            if (primaryScorer != null && _random.NextDouble() < 0.28)
            {
                return CreatePrimaryScorerShot(primaryScorer, tactics);
            }

            var action = SelectOffensiveAction(ballHandler, attackingPlayers, tactics);

            return action switch
            {
                OffensiveActionType.Drive => CreateDriveShot(ballHandler),
                OffensiveActionType.PostUp => CreatePostUpShot(attackingPlayers, rotation),
                OffensiveActionType.PullUp => CreatePullUpShot(ballHandler, tactics),
                OffensiveActionType.SpotUp => CreateSpotUpShot(attackingPlayers, rotation),
                _ => CreateDriveShot(ballHandler)
            };
        }

        private ShotSelection SelectShotAfterOffensiveRebound(IReadOnlyList<Player> attackingPlayers, TeamRotation rotation, TeamTactics tactics)
        {
            var kickOutChance = 0.15 + (tactics.ThreePointShare * 0.55) + (((tactics.BallMovement - 50) / 50.0) * 0.08);
            kickOutChance = Clamp(kickOutChance, 0.12, 0.65);

            if (_random.NextDouble() < kickOutChance)
            {
                return CreateSpotUpShot(attackingPlayers, rotation);
            }

            var shooter = SelectWeightedPlayer(attackingPlayers, player =>
            {
                var weight = (double)(player.Attributes.OffensiveRebounding + player.Attributes.Finishing);

                if (player.Id == rotation.PrimaryScorer.Id)
                {
                    weight *= 1.35;
                }

                return weight;
            });

            var zone = _random.NextDouble() < 0.75 ? ShotZone.AtRim : ShotZone.Paint;

            return new ShotSelection(shooter, OffensiveActionType.PostUp, zone);
        }

        private OffensiveActionType SelectOffensiveAction(Player ballHandler, IReadOnlyList<Player> attackingPlayers, TeamTactics tactics)
        {
            var driveWeight = 1.0;
            var postUpWeight = 1.0;
            var pullUpWeight = 1.0;
            var spotUpWeight = 1.0;

            switch (ballHandler.Position)
            {
                case PlayerPosition.PointGuard:
                    driveWeight = 4.0;
                    pullUpWeight = 3.0;
                    spotUpWeight = 2.0;
                    postUpWeight = 0.3;
                    break;

                case PlayerPosition.ShootingGuard:
                    driveWeight = 3.0;
                    pullUpWeight = 3.0;
                    spotUpWeight = 3.0;
                    postUpWeight = 0.5;
                    break;

                case PlayerPosition.SmallForward:
                    driveWeight = 2.5;
                    pullUpWeight = 2.0;
                    spotUpWeight = 2.0;
                    postUpWeight = 1.5;
                    break;

                case PlayerPosition.PowerForward:
                    driveWeight = 1.5;
                    pullUpWeight = 1.0;
                    spotUpWeight = 1.2;
                    postUpWeight = 4.0;
                    break;

                case PlayerPosition.Center:
                    driveWeight = 0.8;
                    pullUpWeight = 0.3;
                    spotUpWeight = 0.5;
                    postUpWeight = 6.0;
                    break;
            }

            driveWeight *= 0.75 + ballHandler.Attributes.Finishing / 20.0;
            pullUpWeight *= 0.75 + ballHandler.Attributes.MidRange / 20.0;

            var teamThreePoint = Average(attackingPlayers, player => player.Attributes.ThreePoint);
            spotUpWeight *= 0.75 + teamThreePoint / 20.0;

            var postPlayers = attackingPlayers.Where(player => player.Position == PlayerPosition.PowerForward || player.Position == PlayerPosition.Center).ToList();

            if (postPlayers.Count > 0)
            {
                var postQuality = postPlayers.Average(player => player.Attributes.Finishing + player.Attributes.Strength) / 2.0;
                postUpWeight *= 0.75 + postQuality / 20.0;
            }

            var rimFactor = Clamp(tactics.RimShare / 0.45, 0.35, 2.25);
            var midRangeFactor = Clamp(tactics.MidRangeShare / 0.20, 0.35, 2.25);
            var threePointFactor = Clamp(tactics.ThreePointShare / 0.35, 0.35, 2.25);

            driveWeight *= rimFactor;
            postUpWeight *= rimFactor;

            pullUpWeight *= (midRangeFactor * 0.70) + (threePointFactor * 0.30);
            spotUpWeight *= threePointFactor;

            var ballMovementBias = (tactics.BallMovement - 50) / 50.0;

            spotUpWeight *= 1.0 + (ballMovementBias * 0.35);
            driveWeight *= 1.0 - (ballMovementBias * 0.15);
            pullUpWeight *= 1.0 - (ballMovementBias * 0.10);

            var totalWeight = driveWeight + postUpWeight + pullUpWeight + spotUpWeight;
            var roll = _random.NextDouble() * totalWeight;

            if ((roll -= driveWeight) <= 0)
            {
                return OffensiveActionType.Drive;
            }

            if ((roll -= postUpWeight) <= 0)
            {
                return OffensiveActionType.PostUp;
            }

            if ((roll -= pullUpWeight) <= 0)
            {
                return OffensiveActionType.PullUp;
            }

            return OffensiveActionType.SpotUp;
        }

        private ShotSelection CreatePrimaryScorerShot(Player scorer, TeamTactics tactics)
        {
            if (scorer.Position == PlayerPosition.Center || scorer.Position == PlayerPosition.PowerForward)
            {
                if (scorer.Attributes.Finishing + scorer.Attributes.Strength >= scorer.Attributes.ThreePoint + scorer.Attributes.MidRange)
                {
                    var zone = _random.NextDouble() < 0.60 ? ShotZone.AtRim : ShotZone.Paint;

                    return new ShotSelection(scorer, OffensiveActionType.PostUp, zone);
                }
            }

            if (scorer.Attributes.ThreePoint > scorer.Attributes.Finishing && tactics.ThreePointShare >= tactics.RimShare)
            {
                var zone = _random.NextDouble() < 0.25 ? ShotZone.CornerThree : ShotZone.AboveBreakThree;

                return new ShotSelection(scorer, OffensiveActionType.SpotUp, zone);
            }

            if (scorer.Attributes.MidRange > scorer.Attributes.Finishing)
            {
                return new ShotSelection(scorer, OffensiveActionType.PullUp, ShotZone.MidRange);
            }

            return CreateDriveShot(scorer);
        }

        private ShotSelection CreateDriveShot(Player ballHandler)
        {
            var atRimChance = 0.62 + ((ballHandler.Attributes.Speed - 10.5) * 0.015);
            atRimChance = Clamp(atRimChance, 0.45, 0.80);

            var zone = _random.NextDouble() < atRimChance ? ShotZone.AtRim : ShotZone.Paint;

            return new ShotSelection(ballHandler, OffensiveActionType.Drive, zone);
        }

        private ShotSelection CreatePostUpShot(IReadOnlyList<Player> attackingPlayers, TeamRotation rotation)
        {
            var postPlayers = attackingPlayers.Where(player => player.Position == PlayerPosition.PowerForward || player.Position == PlayerPosition.Center).ToList();

            var candidates = postPlayers.Count > 0 ? postPlayers : attackingPlayers.ToList();
            var shooter = SelectWeightedPlayer(candidates, player =>
            {
                var weight = player.Attributes.Strength + player.Attributes.Finishing * 1.5;

                if (player.Id == rotation.PrimaryScorer.Id)
                {
                    weight *= 1.75;
                }

                return weight;
            });

            var zone = _random.NextDouble() < 0.52 ? ShotZone.AtRim : ShotZone.Paint;

            return new ShotSelection(shooter, OffensiveActionType.PostUp, zone);
        }

        private ShotSelection CreatePullUpShot(Player ballHandler, TeamTactics tactics)
        {
            var playerBias = ballHandler.Attributes.ThreePoint - ballHandler.Attributes.MidRange;

            var tacticalThreeBias = tactics.ThreePointShare - tactics.MidRangeShare;

            var aboveBreakThreeChance = 0.30 + (playerBias * 0.025) + (tacticalThreeBias * 0.35);
            aboveBreakThreeChance = Clamp(aboveBreakThreeChance, 0.08, 0.72);

            var zone = _random.NextDouble() < aboveBreakThreeChance ? ShotZone.AboveBreakThree : ShotZone.MidRange;

            return new ShotSelection(ballHandler, OffensiveActionType.PullUp, zone);
        }

        private ShotSelection CreateSpotUpShot(IReadOnlyList<Player> attackingPlayers, TeamRotation rotation)
        {
            var shooter = SelectWeightedPlayer(attackingPlayers, player =>
            {
                var weight = player.Attributes.ThreePoint * 1.75 + player.Attributes.BasketballIq * 0.25;

                if (player.Id == rotation.PrimaryScorer.Id)
                {
                    weight *= 1.50;
                }

                return weight;
            });

            var zone = _random.NextDouble() < 0.30 ? ShotZone.CornerThree : ShotZone.AboveBreakThree;

            return new ShotSelection(shooter, OffensiveActionType.SpotUp, zone);
        }

        private bool SimulateFieldGoal(ShotSelection shot, Player defender, TeamTactics defendingTactics, bool homePossession)
        {
            var chance = CalculateShotChance(shot, defender, defendingTactics);

            if (homePossession)
            {
                chance += 0.01;
            }

            chance = Clamp(chance, MinimumShotChance, MaximumShotChance);

            return _random.NextDouble() < chance;
        }

        private static double CalculateShotChance(ShotSelection shot, Player defender, TeamTactics defendingTactics)
        {
            var perimeterModifier = GetPerimeterDefenseModifier(defendingTactics);
            var interiorModifier = GetInteriorDefenseModifier(defendingTactics);

            switch (shot.Zone)
            {
                case ShotZone.AtRim:
                    {
                        var offense = shot.Shooter.Attributes.Finishing * 0.70 + shot.Shooter.Attributes.Strength * 0.15 + shot.Shooter.Attributes.Speed * 0.15;
                        var defense = defender.Attributes.InteriorDefense * 0.75 + defender.Attributes.Strength * 0.25 + interiorModifier;

                        return AtRimBaseChance
                            + ((offense - AverageAttributeRating) * AtRimOffenseWeight)
                            - ((defense - AverageAttributeRating) * AtRimDefenseWeight);
                    }

                case ShotZone.Paint:
                    {
                        var offense = shot.Shooter.Attributes.Finishing * 0.55 + shot.Shooter.Attributes.MidRange * 0.30 + shot.Shooter.Attributes.Strength * 0.15;
                        var defense = defender.Attributes.InteriorDefense * 0.70 + defender.Attributes.PerimeterDefense * 0.30 + interiorModifier;

                        return PaintBaseChance
                            + ((offense - AverageAttributeRating) * PaintOffenseWeight)
                            - ((defense - AverageAttributeRating) * PaintDefenseWeight);
                    }

                case ShotZone.MidRange:
                    {
                        var offense = shot.Shooter.Attributes.MidRange * 0.80 + shot.Shooter.Attributes.BasketballIq * 0.20;
                        var defense = defender.Attributes.PerimeterDefense * 0.70 + defender.Attributes.InteriorDefense * 0.30 + (perimeterModifier * 0.65) + (interiorModifier * 0.35);

                        return MidRangeBaseChance
                            + ((offense - AverageAttributeRating) * MidRangeOffenseWeight)
                            - ((defense - AverageAttributeRating) * MidRangeDefenseWeight);
                    }

                case ShotZone.CornerThree:
                    {
                        var offense = shot.Shooter.Attributes.ThreePoint * 0.85 + shot.Shooter.Attributes.BasketballIq * 0.15;
                        var defense = defender.Attributes.PerimeterDefense + perimeterModifier;

                        return CornerThreeBaseChance
                            + ((offense - AverageAttributeRating) * ThreePointOffenseWeight)
                            - ((defense - AverageAttributeRating) * ThreePointDefenseWeight);
                    }

                case ShotZone.AboveBreakThree:
                    {
                        var offense = shot.Shooter.Attributes.ThreePoint * 0.85 + shot.Shooter.Attributes.BallHandling * 0.15;
                        var defense = defender.Attributes.PerimeterDefense + perimeterModifier;

                        return AboveBreakThreeBaseChance
                            + ((offense - AverageAttributeRating) * ThreePointOffenseWeight)
                            - ((defense - AverageAttributeRating) * ThreePointDefenseWeight);
                    }

                default:
                    return 0.45;
            }
        }

        private Player SelectShotDefender(Player shooter, IReadOnlyList<Player> defendingPlayers, ShotZone zone)
        {
            var positionMatchedPlayers = defendingPlayers.OrderBy(player => Math.Abs((int)player.Position - (int)shooter.Position)).ToList();

            var closestPositionDistance = Math.Abs((int)positionMatchedPlayers[0].Position - (int)shooter.Position);

            var closestMatches = positionMatchedPlayers.Where(player => Math.Abs((int)player.Position - (int)shooter.Position) == closestPositionDistance).ToList();

            return IsInteriorZone(zone)
                ? closestMatches.OrderByDescending(player => player.Attributes.InteriorDefense).First()
                : closestMatches.OrderByDescending(player => player.Attributes.PerimeterDefense).First();
        }

        private bool SimulateTurnover(Player ballHandler, Team attackingTeam, Team defendingTeam, IReadOnlyList<Player> defendingPlayers, TeamTactics defendingTactics, Dictionary<int, PlayerBoxScoreBuilder> attackingStats, Dictionary<int, PlayerBoxScoreBuilder> defendingStats, int periodNumber, ref int actionTime, List<MatchEvent> events, int homeScore, int awayScore)
        {
            var ballSecurity = (ballHandler.Attributes.BallHandling + ballHandler.Attributes.Passing) / 2.0;

            var defensivePressure = Average(defendingPlayers, player => player.Attributes.PerimeterDefense);
            defensivePressure += GetPerimeterDefenseModifier(defendingTactics);

            var turnoverChance = 0.145 + ((defensivePressure - ballSecurity) * 0.003);
            turnoverChance = Clamp(turnoverChance, 0.07, 0.22);

            if (_random.NextDouble() >= turnoverChance)
            {
                return false;
            }

            attackingStats[ballHandler.Id].RecordTurnover();

            var stealChance = 0.62 + ((defensivePressure - 10.5) * 0.015);
            stealChance = Clamp(stealChance, 0.45, 0.80);

            if (_random.NextDouble() < stealChance)
            {
                var defender = SelectWeightedPlayer(defendingPlayers, player => player.Attributes.PerimeterDefense * 1.5 + player.Attributes.Speed * 0.25);

                defendingStats[defender.Id].RecordSteal();

                AddEvent(events, periodNumber, ref actionTime, MatchEventType.Steal, defendingTeam, defender, ballHandler, homeScore, awayScore);

                return true;
            }

            AddEvent(events, periodNumber, ref actionTime, MatchEventType.Turnover, attackingTeam, ballHandler, null, homeScore, awayScore);

            return true;
        }

        private bool SimulateShootingFoul(ShotSelection shot, Player defender, Team attackingTeam, TeamTactics defendingTactics, bool homePossession, Dictionary<int, PlayerBoxScoreBuilder> attackingStats, Dictionary<int, PlayerBoxScoreBuilder> defendingStats, int periodNumber, ref int actionTime, List<MatchEvent> events, ref int homeScore, ref int awayScore)
        {
            var foulChance = shot.Zone switch
            {
                ShotZone.AtRim => 0.162,
                ShotZone.Paint => 0.117,
                ShotZone.MidRange => 0.072,
                ShotZone.CornerThree => 0.045,
                ShotZone.AboveBreakThree => 0.054,
                _ => 0.072
            };

            var perimeterAggression = Math.Max(0, (defendingTactics.PerimeterPressure - 50) / 50.0);
            var paintAggression = Math.Max(0, (defendingTactics.ProtectPaint - 50) / 50.0);

            if (IsThreePointZone(shot.Zone) || shot.Zone == ShotZone.MidRange)
            {
                foulChance += perimeterAggression * 0.025;
            }

            if (IsInteriorZone(shot.Zone))
            {
                foulChance += paintAggression * 0.035;
            }

            if (_random.NextDouble() >= foulChance)
            {
                return false;
            }

            defendingStats[defender.Id].RecordPersonalFoul();

            AddEvent(events, periodNumber, ref actionTime, MatchEventType.ShootingFoul, attackingTeam, defender, shot.Shooter, homeScore, awayScore, shot.Action, shot.Zone);

            var freeThrows = IsThreePointZone(shot.Zone) ? 3 : 2;

            for (var attempt = 0; attempt < freeThrows; attempt++)
            {
                var makeChance = 0.755 + ((shot.Shooter.Attributes.FreeThrow - AverageAttributeRating) * 0.012);
                makeChance = Clamp(makeChance, 0.45, 0.95);

                var made = _random.NextDouble() < makeChance;

                attackingStats[shot.Shooter.Id].RecordFreeThrowAttempt(made);

                if (made)
                {
                    AddPoints(homePossession, 1, ref homeScore, ref awayScore);
                }

                AddEvent(events, periodNumber, ref actionTime, made ? MatchEventType.MadeFreeThrow : MatchEventType.MissedFreeThrow, attackingTeam, shot.Shooter, null, homeScore, awayScore);
            }

            return true;
        }

        private bool SimulateRebound(Team attackingTeam, Team defendingTeam, IReadOnlyList<Player> attackingPlayers, IReadOnlyList<Player> defendingPlayers, Dictionary<int, PlayerBoxScoreBuilder> attackingStats, Dictionary<int, PlayerBoxScoreBuilder> defendingStats, bool allowOffensiveRebound, int periodNumber, ref int actionTime, List<MatchEvent> events, int homeScore, int awayScore)
        {
            var offensiveRebounding = Average(attackingPlayers, player => player.Attributes.OffensiveRebounding);
            var defensiveRebounding = Average(defendingPlayers, player => player.Attributes.DefensiveRebounding);

            var offensiveReboundChance = 0.25 + ((offensiveRebounding - defensiveRebounding) * 0.006);
            offensiveReboundChance = Clamp(offensiveReboundChance, 0.15, 0.40);

            var offensiveRebound = allowOffensiveRebound && _random.NextDouble() < offensiveReboundChance;

            if (offensiveRebound)
            {
                var rebounder = SelectWeightedPlayer(attackingPlayers, player => player.Attributes.OffensiveRebounding);

                attackingStats[rebounder.Id].RecordOffensiveRebound();

                AddEvent(events, periodNumber, ref actionTime, MatchEventType.OffensiveRebound, attackingTeam, rebounder, null, homeScore, awayScore);

                return true;
            }

            var defensiveRebounder = SelectWeightedPlayer(defendingPlayers, player => player.Attributes.DefensiveRebounding);

            defendingStats[defensiveRebounder.Id].RecordDefensiveRebound();

            AddEvent(events, periodNumber, ref actionTime, MatchEventType.DefensiveRebound, defendingTeam, defensiveRebounder, null, homeScore, awayScore);

            return false;
        }

        private Player TryCreateAssist(ShotSelection shot, IReadOnlyList<Player> attackingPlayers, TeamTactics tactics, Dictionary<int, PlayerBoxScoreBuilder> attackingStats)
        {
            var baseChance = shot.Action switch
            {
                OffensiveActionType.SpotUp => 0.92,
                OffensiveActionType.Drive => 0.58,
                OffensiveActionType.PullUp => 0.42,
                OffensiveActionType.PostUp => 0.38,
                _ => 0.60
            };

            var averagePassing = Average(attackingPlayers, player => player.Attributes.Passing);

            var ballMovementModifier = ((tactics.BallMovement - 50) / 50.0) * 0.18;

            var assistChance = baseChance + ((averagePassing - 10.5) * 0.015) + ballMovementModifier;
            assistChance = Clamp(assistChance, 0.10, 0.92);

            if (_random.NextDouble() >= assistChance)
            {
                return null;
            }

            var possibleAssisters = attackingPlayers.Where(player => player.Id != shot.Shooter.Id).ToList();

            if (possibleAssisters.Count == 0)
            {
                return null;
            }

            var assister = SelectWeightedPlayer(possibleAssisters, player => player.Attributes.Passing * 1.5 + player.Attributes.BasketballIq * 0.5);

            attackingStats[assister.Id].RecordAssist();

            return assister;
        }

        private static double GetPerimeterDefenseModifier(TeamTactics tactics)
        {
            var pressure = ((tactics.PerimeterPressure - 50) / 50.0) * 2.5;
            var paintTradeOff = Math.Max(0, (tactics.ProtectPaint - 50) / 50.0) * 0.75;

            return pressure - paintTradeOff;
        }

        private static double GetInteriorDefenseModifier(TeamTactics tactics)
        {
            var paintProtection = ((tactics.ProtectPaint - 50) / 50.0) * 3.0;
            var perimeterTradeOff = Math.Max(0, (tactics.PerimeterPressure - 50) / 50.0) * 0.75;

            return paintProtection - perimeterTradeOff;
        }

        private static void RecordFieldGoalAttempt(PlayerBoxScoreBuilder stats, ShotZone zone, bool made)
        {
            if (IsThreePointZone(zone))
            {
                stats.RecordThreePointAttempt(made);
                return;
            }

            stats.RecordTwoPointAttempt(made);
        }

        private Player SelectWeightedPlayer(IReadOnlyList<Player> players, Func<Player, double> weightSelector)
        {
            var totalWeight = 0.0;

            foreach (var player in players)
            {
                totalWeight += Math.Max(1.0, weightSelector(player));
            }

            var roll = _random.NextDouble() * totalWeight;

            foreach (var player in players)
            {
                roll -= Math.Max(1.0, weightSelector(player));

                if (roll <= 0)
                {
                    return player;
                }
            }

            return players[players.Count - 1];
        }

        private static bool IsThreePointZone(ShotZone zone)
        {
            return zone == ShotZone.CornerThree || zone == ShotZone.AboveBreakThree;
        }

        private static bool IsInteriorZone(ShotZone zone)
        {
            return zone == ShotZone.AtRim || zone == ShotZone.Paint;
        }

        private static void AddPoints(bool homePossession, int points, ref int homeScore, ref int awayScore)
        {
            if (homePossession)
            {
                homeScore += points;
            }
            else
            {
                awayScore += points;
            }
        }

        private static void AddEvent(List<MatchEvent> events, int periodNumber, ref int secondsRemaining, MatchEventType type, Team team, Player player, Player secondaryPlayer, int homeScore, int awayScore, OffensiveActionType? offensiveAction = null, ShotZone? shotZone = null)
        {
            events.Add(new MatchEvent(periodNumber, secondsRemaining, type, team, player, secondaryPlayer, homeScore, awayScore, offensiveAction, shotZone));

            secondsRemaining = Math.Max(0, secondsRemaining - 2);
        }

        private static void AddPlayingTime(IReadOnlyList<Player> players, double seconds, Dictionary<int, PlayerBoxScoreBuilder> stats)
        {
            foreach (var player in players)
            {
                stats[player.Id].AddPlayingTime(seconds);
            }
        }

        private static Dictionary<int, PlayerBoxScoreBuilder> CreateStatBuilders(Team team, TeamRotation rotation)
        {
            var builders = new Dictionary<int, PlayerBoxScoreBuilder>();

            foreach (var player in team.Players)
            {
                builders[player.Id] = new PlayerBoxScoreBuilder(player, rotation.IsStarter(player.Id));
            }

            return builders;
        }

        private static IReadOnlyList<PlayerBoxScore> BuildBoxScores(Team team, Dictionary<int, PlayerBoxScoreBuilder> stats)
        {
            return team.Players.Select(player => stats[player.Id].Build()).ToList();
        }

        private static double Average(IReadOnlyList<Player> players, Func<Player, int> selector)
        {
            return players.Average(selector);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }


        private sealed class RotationRuntime
        {
            private readonly TeamRotation _rotation;
            private readonly Dictionary<int, double> _secondsPlayed;

            private int _currentSegment = -1;
            private IReadOnlyList<Player> _currentPlayers;

            public RotationRuntime(TeamRotation rotation)
            {
                _rotation = rotation;
                _secondsPlayed = rotation.Team.Players.ToDictionary(player => player.Id, _ => 0.0);
            }

            public IReadOnlyList<Player> GetOnCourtPlayers(int periodNumber, int periodLengthSeconds, int secondsRemaining)
            {
                if (periodNumber > 4)
                {
                    return GetStarters();
                }

                var elapsedSeconds = periodLengthSeconds - secondsRemaining;
                var segmentInPeriod = Math.Min(4, (int)(elapsedSeconds / 120.0));
                var globalSegment = ((periodNumber - 1) * 5) + segmentInPeriod;

                if (globalSegment != _currentSegment)
                {
                    _currentPlayers = SelectLineup(globalSegment);
                    _currentSegment = globalSegment;
                }

                return _currentPlayers;
            }

            public void AddPlayingTime(IReadOnlyList<Player> players, double seconds)
            {
                foreach (var player in players)
                {
                    _secondsPlayed[player.Id] += seconds;
                }
            }

            private IReadOnlyList<Player> SelectLineup(int segmentIndex)
            {
                if (segmentIndex == 0)
                {
                    return GetStarters();
                }

                var progressAfterSegment = (segmentIndex + 1) / 20.0;

                return _rotation.Assignments
                    .Where(assignment => assignment.TargetMinutes > 0)
                    .OrderByDescending(assignment => GetMinutesDeficit(assignment, progressAfterSegment))
                    .ThenBy(assignment => assignment.RotationOrder)
                    .Take(5)
                    .Select(assignment => assignment.Player)
                    .ToList();
            }

            private double GetMinutesDeficit(PlayerRotationAssignment assignment, double progress)
            {
                var desiredSeconds = assignment.TargetMinutes * 60.0 * progress;

                return desiredSeconds - _secondsPlayed[assignment.Player.Id];
            }

            private IReadOnlyList<Player> GetStarters()
            {
                return Enum.GetValues(typeof(PlayerPosition))
                    .Cast<PlayerPosition>()
                    .Select(position => _rotation.GetStarter(position))
                    .ToList();
            }
        }

        private sealed class ShotSelection
        {
            public Player Shooter { get; }

            public OffensiveActionType Action { get; }

            public ShotZone Zone { get; }

            public ShotSelection(Player shooter, OffensiveActionType action, ShotZone zone)
            {
                Shooter = shooter;
                Action = action;
                Zone = zone;
            }
        }

        private Player SelectBallHandler(IReadOnlyList<Player> players, TeamRotation rotation)
        {
            return SelectWeightedPlayer(players, player =>
            {
                var weight = player.Attributes.BallHandling * 1.5 + player.Attributes.Passing;

                if (player.Id == rotation.PrimaryBallHandler.Id)
                {
                    weight *= 2.25;
                }

                if (player.Id == rotation.PrimaryScorer.Id)
                {
                    weight *= 1.15;
                }

                return weight;
            });
        }

        private sealed class PlayerBoxScoreBuilder
        {
            private Player Player { get; }

            private bool IsStarter { get; }

            private double SecondsPlayed { get; set; }

            private int Points { get; set; }

            private int FieldGoalsMade { get; set; }

            private int FieldGoalsAttempted { get; set; }

            private int ThreePointsMade { get; set; }

            private int ThreePointsAttempted { get; set; }

            private int FreeThrowsMade { get; set; }

            private int FreeThrowsAttempted { get; set; }

            private int OffensiveRebounds { get; set; }

            private int DefensiveRebounds { get; set; }

            private int Assists { get; set; }

            private int Steals { get; set; }

            private int PersonalFouls { get; set; }

            private int Turnovers { get; set; }

            public PlayerBoxScoreBuilder(Player player, bool isStarter)
            {
                Player = player;
                IsStarter = isStarter;
            }

            public void AddPlayingTime(double seconds)
            {
                SecondsPlayed += seconds;
            }

            public void RecordTurnover()
            {
                Turnovers++;
            }

            public void RecordSteal()
            {
                Steals++;
            }

            public void RecordAssist()
            {
                Assists++;
            }

            public void RecordPersonalFoul()
            {
                PersonalFouls++;
            }

            public void RecordOffensiveRebound()
            {
                OffensiveRebounds++;
            }

            public void RecordDefensiveRebound()
            {
                DefensiveRebounds++;
            }

            public void RecordFreeThrowAttempt(bool made)
            {
                FreeThrowsAttempted++;

                if (!made)
                {
                    return;
                }

                FreeThrowsMade++;
                Points++;
            }

            public void RecordTwoPointAttempt(bool made)
            {
                FieldGoalsAttempted++;

                if (!made)
                {
                    return;
                }

                FieldGoalsMade++;
                Points += 2;
            }

            public void RecordThreePointAttempt(bool made)
            {
                FieldGoalsAttempted++;
                ThreePointsAttempted++;

                if (!made)
                {
                    return;
                }

                FieldGoalsMade++;
                ThreePointsMade++;
                Points += 3;
            }

            public PlayerBoxScore Build()
            {
                return new PlayerBoxScore(
                    Player,
                    IsStarter,
                    SecondsPlayed,
                    Points,
                    FieldGoalsMade,
                    FieldGoalsAttempted,
                    ThreePointsMade,
                    ThreePointsAttempted,
                    FreeThrowsMade,
                    FreeThrowsAttempted,
                    OffensiveRebounds,
                    DefensiveRebounds,
                    Assists,
                    Steals,
                    PersonalFouls,
                    Turnovers
                );
            }
        }
    }
}