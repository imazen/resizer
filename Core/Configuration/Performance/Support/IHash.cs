namespace ImageResizer.Configuration.Performance
{
    internal interface IHash
    {
        uint ComputeHash(uint value);
        IHash GetNext();
    }
}