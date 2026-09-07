using Xunit;

namespace KopiKala.Tests.E2E.Infrastructure;

[CollectionDefinition("E2E Test Collection", DisableParallelization = true)]
public class E2ECollectionFixture : ICollectionFixture<KopiKalaServerFixture>
{
}
