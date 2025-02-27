using System;
using Confluent.Kafka;
using KafkaRetry.Job.Services.Interfaces;

namespace KafkaRetry.Job.Services.Implementations;

public class KafkaService : IKafkaService
{
    private readonly ConfigurationService _configuration;

    public KafkaService(ConfigurationService configuration)
    {
        _configuration = configuration;
    }

    public IConsumer<string, string> BuildKafkaConsumer()
    {
        var bootstrapServers = _configuration.BootstrapServers;
        var groupId = _configuration.GroupId;
        var consumerConfig = CreateConsumerConfig(bootstrapServers, groupId);
        var consumerBuilder = new ConsumerBuilder<string, string>(consumerConfig);

        return consumerBuilder.Build();
    }

    public IProducer<string, string> BuildKafkaProducer()
    {
        var bootstrapServers = _configuration.BootstrapServers;
        var producerConfig = CreateProducerConfig(bootstrapServers);
        var producerBuilder = new ProducerBuilder<string, string>(producerConfig);

        return producerBuilder.Build();
    }

    public IAdminClient BuildAdminClient()
    {
        var bootstrapServers = _configuration.BootstrapServers;
        var adminClientConfig = CreateAdminClientConfig(bootstrapServers);
        var adminClientBuilder = new AdminClientBuilder(adminClientConfig);

        return adminClientBuilder.Build();
    }

    private AdminClientConfig CreateAdminClientConfig(string bootstrapServers)
    {
        return new AdminClientConfig
        {
            BootstrapServers = bootstrapServers,
            SaslUsername = _configuration.SaslUsername ?? string.Empty,
            SaslPassword = _configuration.SaslPassword ?? string.Empty,
            SslCaLocation = _configuration.SslCaLocation ?? string.Empty,
            SaslMechanism = _configuration.SaslMechanism,
            SecurityProtocol = _configuration.SecurityProtocol,
            SslKeystorePassword = _configuration.SslKeystorePassword ?? string.Empty
        };
    }

    private ProducerConfig CreateProducerConfig(string bootstrapServers)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            SaslUsername = _configuration.SaslUsername ?? string.Empty,
            SaslPassword = _configuration.SaslPassword ?? string.Empty,
            SslCaLocation = _configuration.SslCaLocation ?? string.Empty,
            SaslMechanism = _configuration.SaslMechanism,
            SecurityProtocol = _configuration.SecurityProtocol,
            SslKeystorePassword = _configuration.SslKeystorePassword ?? string.Empty,
            EnableIdempotence = _configuration.EnableIdempotence,
            BatchSize = _configuration.BatchSize,
            ClientId = _configuration.ClientId,
            LingerMs = _configuration.LingerMs,
            MessageTimeoutMs = _configuration.MessageTimeoutMs,
            RequestTimeoutMs = _configuration.RequestTimeoutMs,
            MessageMaxBytes = _configuration.MessageMaxBytes
        };

        if (_configuration.Acks is not null)
        {
            producerConfig.Acks = _configuration.Acks;
        }
        return producerConfig;
    }

    private ConsumerConfig CreateConsumerConfig(string bootstrapServers, string groupId)
    {
        return new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = groupId,
            EnableAutoCommit = _configuration.EnableAutoCommit,
            SaslUsername = _configuration.SaslUsername ?? string.Empty,
            SaslPassword = _configuration.SaslPassword ?? string.Empty,
            SslCaLocation = _configuration.SslCaLocation ?? string.Empty,
            SaslMechanism = _configuration.SaslMechanism,
            SecurityProtocol = _configuration.SecurityProtocol,
            SslKeystorePassword = _configuration.SslKeystorePassword ?? string.Empty,
            EnableAutoOffsetStore = _configuration.EnableAutoOffsetStore
        };
    }
        
    public Action<IConsumer<string, string>, ConsumeResult<string, string>> GetConsumerCommitStrategy()
    {
        return _configuration.EnableAutoCommit ?
            (assignedConsumer, result) =>
            {
                assignedConsumer.StoreOffset(result);
            } :
            (assignedConsumer, result) =>
            {
                assignedConsumer.StoreOffset(result);
                assignedConsumer.Commit();
            };
    }
}