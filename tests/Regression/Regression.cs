namespace Bastion.Core.Tests.Regression;

/// <summary>
/// The four regression procedures, runnable as one suite.
/// </summary>
/// <remarks>
/// <para>
/// docs/prototype/VALIDATION.md § Regression lists four things to do <b>before changing the march
/// curve, Formation Strength, run percentages, tower power, Overload, or the resolver</b>. They are
/// tagged so the whole set runs as one command:
/// </para>
/// <code>
/// dotnet test tests/Bastion.Core.Tests.csproj --filter Category=Regression
/// </code>
/// <para>
/// Steps 1-3 are the reason the core is engine-free: none of them can run inside a scene tree, and
/// step 3 runs thirty thousand hands.
/// </para>
/// </remarks>
internal static class Regression
{
    /// <summary>xUnit trait name. Pair with <see cref="Category"/>.</summary>
    internal const string Trait = "Category";

    /// <summary>xUnit trait value selecting the whole regression suite.</summary>
    internal const string Category = "Regression";

    /// <summary>
    /// Where a procedure's committed baseline lives.
    /// </summary>
    /// <remarks>
    /// Baselines are <b>checked in and regenerated deliberately</b>, never rewritten by a failing
    /// run. A suite that healed itself on failure would report that nothing had changed, every time,
    /// which is the one thing a regression gate must not do.
    /// </remarks>
    internal static string BaselinePath(string fileName) => Path.Combine(
        Bastion.Core.Config.TuningLoader.FindRepositoryRoot(),
        "tests", "Regression", "baselines", fileName);

    /// <summary>Set <c>BASTION_REGEN_BASELINES=1</c> to rewrite the committed baselines.</summary>
    /// <remarks>
    /// An explicit, deliberate act - and the run still fails afterwards, so a regeneration cannot be
    /// mistaken for a pass.
    /// </remarks>
    internal static bool Regenerating =>
        Environment.GetEnvironmentVariable("BASTION_REGEN_BASELINES") == "1";
}
