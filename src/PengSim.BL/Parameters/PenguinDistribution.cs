using System.Text.Json.Serialization;

namespace PengSim.BL.Parameters;

/// <summary>How penguins are placed on the map before the first iteration.</summary>
public enum PenguinDistribution
{
    /// <summary>Every playable cell is equally likely.</summary>
    Uniform,
}
