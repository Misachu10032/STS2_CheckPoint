namespace CheckPoint.Checkpoints;

public class RunCheckpoint
{
    public int      Floor   { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
