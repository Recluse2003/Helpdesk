// Disables parallel test execution. This makes tests run slower, but prevents race conditions when integration tests
// share a database.
[assembly: CollectionBehavior(DisableTestParallelization = true)]