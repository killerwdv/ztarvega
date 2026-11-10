namespace ZtarVega.Services
{
    /// <summary>The outcome of trying to launch a game.</summary>
    internal sealed class LaunchResult
    {
        private LaunchResult(LaunchFailure failure, string errorMessage)
        {
            Failure = failure;
            ErrorMessage = errorMessage;
        }

        public bool Succeeded
        {
            get { return Failure == LaunchFailure.None; }
        }

        public LaunchFailure Failure { get; }

        public string ErrorMessage { get; }

        public static LaunchResult Ok()
        {
            return new LaunchResult(LaunchFailure.None, string.Empty);
        }

        public static LaunchResult Fail(LaunchFailure failure, string errorMessage)
        {
            return new LaunchResult(failure, errorMessage);
        }
    }
}