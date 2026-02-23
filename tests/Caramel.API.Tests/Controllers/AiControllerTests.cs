namespace Caramel.API.Tests.Controllers;

public class AiControllerTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
  private readonly ApiTestFactory _factory = factory;

  [Fact]
  public async Task GetRootReturnsOkAsync()
  {
    var client = _factory.CreateClient();

    var response = await client.GetAsync("/");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task GetNonExistentEndpointReturnsOkAsync()
  {
    var client = _factory.CreateClient();

    var response = await client.GetAsync("/api/nonexistent");

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public void FactoryCreatesClientSuccessfully()
  {
    var client = _factory.CreateClient();

    Assert.NotNull(client);
    Assert.NotNull(client.BaseAddress);
  }
}
