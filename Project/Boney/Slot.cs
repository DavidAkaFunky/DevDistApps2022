namespace DADProject;

public class Slot
{
    public static readonly int Null = -1;

    public Slot(int currentValue, int writeTimestamp, int readTimestamp)
    {
        this.CurrentValue = currentValue;
        this.WriteTimestamp = writeTimestamp;
        this.ReadTimestamp = readTimestamp;
    }

    public int CurrentValue { get; set; }

    public int WriteTimestamp { get; set; }

    public int ReadTimestamp { get; set; }
}