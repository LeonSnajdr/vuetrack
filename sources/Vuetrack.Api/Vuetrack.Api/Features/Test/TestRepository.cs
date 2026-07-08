using Samhammer.DependencyInjection.Attributes;
using Samhammer.Mongo;
using Samhammer.Mongo.Abstractions;

namespace Vuetrack.Api.Features.Test;

[Inject]
public class TestRepository(ILogger<TestRepository> logger, IMongoDbConnector connector) : BaseRepositoryMongo<TestModel>(logger, connector), ITestRepository;

public interface ITestRepository : IBaseRepositoryMongo<TestModel>;
