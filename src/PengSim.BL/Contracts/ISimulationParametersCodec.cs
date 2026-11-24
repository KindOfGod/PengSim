namespace PengSim.BL.Contracts;

/// <summary>Converts simulation parameters to a short text code and back.</summary>
public interface ISimulationParametersCodec
{
    /// <summary>Encodes the given parameters into a code.</summary>
    string Encode(ISimulationParameters parameters);

    /// <summary>Decodes a code created by <see cref="Encode"/> back into parameters.</summary>
    ISimulationParameters Decode(string code);
}
