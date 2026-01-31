
// ------------------------------------------------------------------------------
// LMU Electronic Bridge - Team Lookup Profile
// 
// Contributor: Haagel-FR for all Loop up Values per Team!
// Description: Provides year-agnostic team lookup tables for ARB, regen, 
// motor maps, and brake migration. Normalizes team names and determines car class.
// ------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LMUElectronicBridge
{
    /// <summary>
    /// Enumeration of car class types for LMU teams.
    /// Determines the energy/regen behavior of the car.
    /// </summary>
    public enum CarClass
    {
        Unknown,    // Car class could not be determined
        LMH,        // Le Mans Hypercar
        LMDH        // Le Mans Daytona Hybrid
    }

    /// <summary>
    /// Provides a unified, year-agnostic lookup profile for LMU teams.
    /// Strips year suffixes from team names and provides properties for ARB settings,
    /// regen levels, electronic motor maps, and brake migration based on car class.
    /// </summary>
    public sealed class TeamLookupProfile
    {
        // ---------------- Public Properties ----------------

        /// <summary>
        /// Original team name string as received from API.
        /// </summary>
        public string RawTeamName { get; }

        /// <summary>
        /// Team name normalized by stripping the year (e.g., "Peugeot TotalEnergies 2024" -> "Peugeot TotalEnergies").
        /// </summary>
        public string NormalizedTeamName { get; }

        /// <summary>
        /// Car class of the team (LMH, LMDH, or Unknown).
        /// Determines regen and motor map behavior.
        /// </summary>
        public CarClass CarClass { get; }

        /// <summary>
        /// Returns the front ARB lookup for the team.
        /// Uses exact match, year-stripped match, or shared default values.
        /// </summary>
        public IReadOnlyList<string> FrontARB => ResolveARB(TeamMappings.FrontARBMap);

        /// <summary>
        /// Returns the rear ARB lookup for the team.
        /// Uses exact match, year-stripped match, or shared default values.
        /// </summary>
        public IReadOnlyList<string> RearARB => ResolveARB(TeamMappings.RearARBMap);

        /// <summary>
        /// Returns a list of available regen levels depending on car class.
        /// LMDH and LMH have different regen stages.
        /// </summary>
        public IReadOnlyList<string> RegenLevels => ResolveRegenLevels();

        /// <summary>
        /// Returns a list of available electric motor maps depending on car class.
        /// </summary>
        public IReadOnlyList<string> ElectronicMotorMaps => ResolveMotorMaps();

        /// <summary>
        /// Returns a list of available brake migration settings depending on car class.
        /// </summary>
        public IReadOnlyList<string> BrakeMigration => ResolveBrakeMigration();

        // In TeamLookupProfile.cs hinzufügen
        /// <summary>
        /// Returns a list of available engine mixture settings.
        /// </summary>
        public IReadOnlyList<string> EngineMixture => new List<string> { "Safety-Car", "Race" };

        // ---------------- Constructor ----------------

        /// <summary>
        /// Constructs a TeamLookupProfile for a given raw team name.
        /// Normalizes the team name and resolves car class automatically.
        /// </summary>
        /// <param name="teamName">The raw team name string.</param>
        public TeamLookupProfile(string teamName)
        {
            RawTeamName = teamName ?? string.Empty;
            NormalizedTeamName = NormalizeTeamName(RawTeamName);
            CarClass = ResolveCarClass(RawTeamName);
        }

        // ---------------- Helper Methods ----------------

        /// <summary>
        /// Normalizes a team name by stripping trailing year (e.g., "Team X 2024" -> "Team X").
        /// </summary>
        /// <param name="teamName">Raw team name.</param>
        /// <returns>Normalized team name without year.</returns>
        private static string NormalizeTeamName(string teamName)
        {
            if (string.IsNullOrWhiteSpace(teamName)) return string.Empty;
            return Regex.Replace(teamName.Trim(), @"\s20\d{2}$", "");
        }

        /// <summary>
        /// Determines the car class of a team based on lookup sets.
        /// </summary>
        /// <param name="teamName">Raw or year-stripped team name.</param>
        /// <returns>CarClass enum value.</returns>
        private static CarClass ResolveCarClass(string teamName)
        {
            if (TeamMappings.LMU_LMDH.Contains(teamName)) return CarClass.LMDH;
            if (TeamMappings.LMU_LMH.Contains(teamName)) return CarClass.LMH;
            return CarClass.Unknown;
        }

        /// <summary>
        /// Resolves ARB list for front or rear suspension.
        /// Uses exact team match, year-stripped match, or shared defaults if necessary.
        /// </summary>
        /// <param name="arbMap">Dictionary mapping team names to ARB setups.</param>
        /// <returns>List of ARB positions/settings.</returns>
        private IReadOnlyList<string> ResolveARB(Dictionary<string, List<string>> arbMap)
        {
            // 1️⃣ Exact match
            if (arbMap.TryGetValue(RawTeamName, out var direct)) return direct;

            // 2️⃣ Match normalized name (without year)
            var normalizedMatch = arbMap.FirstOrDefault(k =>
                NormalizeTeamName(k.Key).Equals(NormalizedTeamName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(normalizedMatch.Key)) return normalizedMatch.Value;

            // 3️⃣ Shared default (common teams without specific mapping)
            if (TeamMappings.SharedTeams.Contains(NormalizedTeamName))
                return TeamMappings.SharedAntisway.ToList();

            // 4️⃣ Fallback single value if nothing matches
            return new List<string> { "N/A" };
        }

        /// <summary>
        /// Resolves the regen level list depending on car class.
        /// </summary>
        /// <returns>List of regen stages as strings.</returns>
        private IReadOnlyList<string> ResolveRegenLevels()
        {
            if (CarClass == CarClass.LMDH)
                return new List<string> { "Off", "17kW", "34kW", "51kW", "68kW", "85kW", "102kW", "119kW", "136kW", "153kW", "170kW" };
            if (CarClass == CarClass.LMH)
                return new List<string> { "Off", "20kW", "40kW", "60kW", "80kW", "100kW", "120kW", "140kW", "160kW", "180kW", "200kW" };
            // Default for unknown cars
            return new List<string> { "N/A", "N/A" };
        }
        /// <summary>
        /// Resolves the electric motor map list depending on car class.
        /// </summary>
        /// <returns>List of motor power levels.</returns>

        private IReadOnlyList<string> ResolveMotorMaps()
        {
            if (CarClass == CarClass.LMDH)
                return new List<string> { "Off", "10kW", "20kW", "30kW", "40kW", "50kW" };
            if (CarClass == CarClass.LMH)
                return new List<string> { "Off", "20kW", "40kW", "60kW", "80kW", "100kW", "120kW", "140kW", "160kW", "180kW", "200kW" };
            return new List<string> { "Safety-car", "Race" };
        }

        /// <summary>
        /// Resolves the brake migration list depending on car class.
        /// </summary>
        /// <returns>List of brake migration stages as strings.</returns>
        private IReadOnlyList<string> ResolveBrakeMigration()
        {
            // Applies to both LMDH and LMH cars
            if (TeamMappings.LMU_LMDH.Contains(RawTeamName) || TeamMappings.LMU_LMH.Contains(RawTeamName))
                return new List<string> { "2.5% F", "2.0% F", "1.5% F", "1.0% F", "0.5% F", "Disabled" };

            // Default for unknown cars
            return new List<string> { "N/A", "N/A" };
        }

        // ---------------- Team Mappings (Year-Stripped) ----------------
        private static class TeamMappings
        {
            // Shared antisway bar positions for common teams without specific mapping
            public static readonly string[] SharedAntisway =
                { "Detached", "P1", "P2", "P3", "P4", "P5", "P6", "P8", "P9", "P10", "P11", "P12", "P13", "P14", "P15" };

            public static readonly string[] SharedTeams =
            {
                "Glickenhaus Racing",
                "Peugeot TotalEnergies",
                "Hertz Team Jota",
                "Porsche Penske Motorsport",
                "Proton Competition",
                "Toyota Gazoo Racing"
            };

            // Teams categorized as LMDH
            public static readonly HashSet<string> LMU_LMDH = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Alpine Endurance Team",
                "BMW M Team WRT",
                "Action Express Racing",
                "Cadillac Racing",
                "Whelen Cadillac Racing",
                "Cadillac Hertz Team Jota",
                "Cadillac Whelen",
                "Cadillac WTR",
                "Hertz Team Jota",
                "Porsche Penske Motorsport",
                "Proton Competition",
                "Lamborghini Iron Lynx"
            };

            // Teams categorized as LMH
            public static readonly HashSet<string> LMU_LMH = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Ferrari AF Corse",
                "AF Corse",
                "Peugeot TotalEnergies",
                "Toyota Gazoo Racing",
                "Isotta TIPO6"
            };

            // Front ARB mappings per team
            public static readonly Dictionary<string, List<string>> FrontARBMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Alpine Endurance Team", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12", "P13", "P14", "P15", "P16" } },
                { "Aston Martin THOR Team", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11" } },
                { "BMW M Team WRT", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Action Express Racing", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Cadillac Racing", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Whelen Cadillac Racing", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Cadillac Hertz Team Jota", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Cadillac Whelen", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Cadillac WTR", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Ferrari AF Corse", new List<string> { "Detached", "A-P1", "A-P2", "A-P3", "A-P4", "A-P5", "B-P1", "B-P2", "B-P3", "B-P4", "B-P5", "C-P1", "C-P2", "C-P3", "C-P4", "C-P5", "D-P1", "D-P2", "D-P3", "D-P4", "D-P5", "E-P1", "E-P2", "E-P3", "E-P4", "E-P5" } },
                { "AF Corse", new List<string> { "Detached", "A-P1", "A-P2", "A-P3", "A-P4", "A-P5", "B-P1", "B-P2", "B-P3", "B-P4", "B-P5", "C-P1", "C-P2", "C-P3", "C-P4", "C-P5", "D-P1", "D-P2", "D-P3", "D-P4", "D-P5", "E-P1", "E-P2", "E-P3", "E-P4", "E-P5" } },
                { "Lamborghini Iron Lynx", new List<string> { "Detached", "14.5-TK 0deg", "14.5-TK 30deg", "14.5-TK 45deg", "14.5-TK 60deg", "14.5-TK 90deg", "16-TK 0deg", "16-TK 30deg", "16-TK 45deg", "16-TK 60deg", "16-TK 90deg", "17.5-TK 0deg", "17.5-TK 30deg", "17.5-TK 45deg", "17.5-TK 60deg", "17.5-TK 90deg", "20.5-TK 0deg", "20.5-TK 30deg", "20.5-TK 45deg", "20.5-TK 60deg", "20.5-TK 90deg" } },
                { "Isotta TIPO6", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5", "P6", "P7" } }
            };

            // Rear ARB mappings per team
            public static readonly Dictionary<string, List<string>> RearARBMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Alpine Endurance Team", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12" } },
                { "Aston Martin THOR Team", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11" } },
                { "BMW M Team WRT", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Action Express Racing", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Cadillac Racing", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Whelen Cadillac Racing", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Cadillac Hertz Team Jota", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Cadillac Whelen", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Cadillac WTR", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5" } },
                { "Ferrari AF Corse", new List<string> { "Detached", "A-P1", "A-P2", "A-P3", "A-P4", "A-P5", "B-P1", "B-P2", "B-P3", "B-P4", "B-P5", "C-P1", "C-P2", "C-P3", "C-P4", "C-P5", "D-P1", "D-P2", "D-P3", "D-P4", "D-P5", "E-P1", "E-P2", "E-P3", "E-P4", "E-P5" } },
                { "AF Corse", new List<string> { "Detached", "A-P1", "A-P2", "A-P3", "A-P4", "A-P5", "B-P1", "B-P2", "B-P3", "B-P4", "B-P5", "C-P1", "C-P2", "C-P3", "C-P4", "C-P5", "D-P1", "D-P2", "D-P3", "D-P4", "D-P5", "E-P1", "E-P2", "E-P3", "E-P4", "E-P5" } },
                { "Lamborghini Iron Lynx", new List<string> { "Detached", "14.5-TN 0deg", "14.5-TN 30deg", "14.5-TN 60deg", "14.5-TN 90deg", "16-TK 0deg", "16-TK 30deg", "16-TK 60deg", "16-TK 90deg", "17.5-TK 0deg", "17.5-TK 30deg", "17.5-TK 60deg", "17.5-TK 90deg", "20.5-TK 0deg", "20.5-TK 30deg", "20.5-TK 60deg", "20.5-TK 90deg" } },
                { "Isotta TIPO6", new List<string> { "Detached", "P1", "P2", "P3", "P4", "P5", "P6", "P7" } }
            };
        }
    }
}
