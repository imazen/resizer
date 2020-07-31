namespace ImageResizer.Configuration.Performance
{
    interface IHash
    {
        uint ComputeHash(uint value);
        IHash GetNext();
    }
}
