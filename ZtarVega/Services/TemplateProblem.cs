namespace ZtarVega.Services
{
    /// <summary>What is wrong with a web address template, if anything.</summary>
    internal enum TemplateProblem
    {
        None,
        Empty,
        UnsupportedScheme,
        NoPlaceholder
    }
}