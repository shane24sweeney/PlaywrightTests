// Run every test in parallel. Each test gets its own fixture instance, so per-test state
// (Page, Context, StartPage) is never shared between threads.
// The number of workers is set by NumberOfTestWorkers in .runsettings.
[assembly: Parallelizable(ParallelScope.All)]
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
