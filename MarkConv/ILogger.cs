namespace MarkConv
{
    public interface ILogger
    {
        void Warn(string message);

        void Info(string message);

        void Error(string message);

        int ErrorCount { get; }
    }
}
