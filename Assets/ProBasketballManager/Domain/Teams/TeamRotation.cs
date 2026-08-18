using System;
using System.Collections.Generic;
using System.Linq;
using ProBasketballManager.Domain.Competitions;
using ProBasketballManager.Domain.Players;

namespace ProBasketballManager.Domain.Teams
{
    public sealed class TeamRotation
    {
        private readonly Dictionary<PlayerPosition, Player> _starters;
        private readonly List<PlayerRotationAssignment> _assignments;

        public Team Team { get; }

        public IReadOnlyDictionary<PlayerPosition, Player> Starters => _starters;

        public IReadOnlyList<PlayerRotationAssignment> Assignments => _assignments;

        public Player PrimaryBallHandler { get; }

        public Player PrimaryScorer { get; }

        public CompetitionRules Rules { get; }

        public int RequiredTotalMinutes => (int)Math.Round(Rules.TotalPlayerMinutesPerGame);

        public TeamRotation(Team team, IDictionary<PlayerPosition, Player> starters, IEnumerable<PlayerRotationAssignment> assignments, Player primaryBallHandler, Player primaryScorer, CompetitionRules rules = null)
        {
            Team = team ?? throw new ArgumentNullException(nameof(team));
            Rules = rules ?? CompetitionRules.Fiba;
            PrimaryBallHandler = primaryBallHandler ?? throw new ArgumentNullException(nameof(primaryBallHandler));
            PrimaryScorer = primaryScorer ?? throw new ArgumentNullException(nameof(primaryScorer));

            _starters = new Dictionary<PlayerPosition, Player>(starters);
            _assignments = assignments.ToList();

            Validate();
        }

        public Player GetStarter(PlayerPosition position)
        {
            return _starters[position];
        }

        public bool IsStarter(int playerId)
        {
            return _starters.Values.Any(player => player.Id == playerId);
        }

        public PlayerRotationAssignment GetAssignment(int playerId)
        {
            return _assignments.Single(assignment => assignment.Player.Id == playerId);
        }

        public static TeamRotation CreateDefault(Team team, CompetitionRules rules = null)
        {
            var effectiveRules = rules ?? CompetitionRules.Fiba;

            var remainingPlayers = team.Players.ToList();
            var starters = new Dictionary<PlayerPosition, Player>();

            foreach (PlayerPosition position in Enum.GetValues(typeof(PlayerPosition)))
            {
                var player = remainingPlayers.FirstOrDefault(candidate => candidate.Position == position);

                if (player == null)
                {
                    throw new InvalidOperationException($"{team.Name} does not have a player available at {position}.");
                }

                starters[position] = player;
                remainingPlayers.Remove(player);
            }

            var totalMinutes = (int)Math.Round(effectiveRules.TotalPlayerMinutesPerGame);
            var starterCount = effectiveRules.PlayersOnCourt;

            var benchCount = Math.Min(5, remainingPlayers.Count);

            var starterShare = (int)Math.Round(totalMinutes * 0.70 / starterCount);
            var benchShare = benchCount == 0 ? 0 : (totalMinutes - (starterShare * starterCount)) / benchCount;

            var assignments = new List<PlayerRotationAssignment>();
            var rotationOrder = 1;

            var allocated = 0;
            var starterIndex = 0;

            foreach (PlayerPosition position in Enum.GetValues(typeof(PlayerPosition)))
            {
                var isLastStarter = starterIndex == starterCount - 1;

                var minutes = isLastStarter ? totalMinutes - allocated - (benchShare * benchCount) : starterShare;

                assignments.Add(new PlayerRotationAssignment(starters[position], minutes, rotationOrder));

                allocated += minutes;
                rotationOrder++;
                starterIndex++;
            }

            for (var index = 0; index < remainingPlayers.Count; index++)
            {
                var targetMinutes = index < benchCount ? benchShare : 0;

                assignments.Add(new PlayerRotationAssignment(remainingPlayers[index], targetMinutes, rotationOrder));
                rotationOrder++;
            }

            var primaryBallHandler = starters[PlayerPosition.PointGuard];

            var primaryScorer = starters.Values
                .OrderByDescending(player => player.Attributes.Finishing + player.Attributes.MidRange + player.Attributes.ThreePoint)
                .First();

            return new TeamRotation(team, starters, assignments, primaryBallHandler, primaryScorer, rules);
        }

        private void Validate()
        {
            var positions = Enum.GetValues(typeof(PlayerPosition)).Cast<PlayerPosition>().ToList();

            foreach (var position in positions)
            {
                if (!_starters.ContainsKey(position))
                {
                    throw new ArgumentException($"No starter has been selected for {position}.");
                }
            }

            var starterIds = _starters.Values.Select(player => player.Id).ToList();

            if (starterIds.Distinct().Count() != 5)
            {
                throw new ArgumentException("The starting five must contain five different players.");
            }

            if (_starters.Values.Any(player => !Team.Players.Any(teamPlayer => teamPlayer.Id == player.Id)))
            {
                throw new ArgumentException("Every starter must belong to the team.");
            }

            if (_assignments.Count != Team.Players.Count)
            {
                throw new ArgumentException("Every player on the roster must have a rotation assignment.");
            }

            var assignmentPlayerIds = _assignments.Select(assignment => assignment.Player.Id).ToList();

            if (assignmentPlayerIds.Distinct().Count() != Team.Players.Count)
            {
                throw new ArgumentException("Every player must have exactly one rotation assignment.");
            }

            var totalMinutes = _assignments.Sum(assignment => assignment.TargetMinutes);

            if (totalMinutes != RequiredTotalMinutes)
            {
                throw new ArgumentException($"Rotation minutes must total {RequiredTotalMinutes} under {Rules.Name} rules. " + $"Current total: {totalMinutes}.");
            }

            var rotationOrders = _assignments.Select(assignment => assignment.RotationOrder).ToList();

            if (rotationOrders.Distinct().Count() != rotationOrders.Count)
            {
                throw new ArgumentException("Every player must have a unique rotation order.");
            }

            if (!Team.Players.Any(player => player.Id == PrimaryBallHandler.Id))
            {
                throw new ArgumentException("The primary ball handler must belong to the team.");
            }

            if (!Team.Players.Any(player => player.Id == PrimaryScorer.Id))
            {
                throw new ArgumentException("The primary scorer must belong to the team.");
            }

            if (GetAssignment(PrimaryBallHandler.Id).TargetMinutes == 0)
            {
                throw new ArgumentException("The primary ball handler must receive playing time.");
            }

            if (GetAssignment(PrimaryScorer.Id).TargetMinutes == 0)
            {
                throw new ArgumentException("The primary scorer must receive playing time.");
            }
        }
    }
}