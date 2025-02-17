using Dave.Benchmarks.Core.Utilities;

namespace Dave.Benchmarks.Core.Models.Entities;

/// <summary>
/// Base class for all model simulations, containing the instruction file used to run the model.
/// </summary>
public abstract class Simulation
{
    public int Id { get; set; }
    private byte[] InstructionFile { get; set; } = Array.Empty<byte>();
    
    public void SetInstructions(string instructions) => 
        InstructionFile = CompressionUtility.CompressText(instructions);
    
    public string GetInstructions() => 
        CompressionUtility.DecompressToText(InstructionFile);
    
    public void SetCompressedInstructions(byte[] instructions) => 
        InstructionFile = instructions;
}
