namespace DADProject;

public class Slot
{
    public static readonly int Bottom = -1;
    private int currentValue;
    private int writeTimestamp;
    private int readTimestamp;

    public Slot(int currentValue, int writeTimestamp, int readTimestamp)
    {
        this.CurrentValue = currentValue;
        this.WriteTimestamp = writeTimestamp;
        this.ReadTimestamp = readTimestamp;
    }

    public int CurrentValue
    {
        get { return currentValue; }
        set { currentValue = value; }
    }

    public int WriteTimestamp
    {
        get { return writeTimestamp; }
        set { writeTimestamp = value; }
    }

    public int ReadTimestamp
    {
        get { return readTimestamp; }
        set { readTimestamp = value; }
    }
}