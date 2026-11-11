using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Infrastructure.Services.Pagamentos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GatumsGourmet.UnitTests.Infrastructure.Services.Pagamentos;

public class MercadoPagoWebhookSignatureValidatorTests
{
    private const string Segredo = "segredo-de-teste-falso";
    private const string RequestId = "req-123";
    private const string DataId = "ORD01ABCDEF";

    private static readonly DateTime Agora = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    private static MercadoPagoWebhookSignatureValidator CriarValidator(string? segredo, string ambiente = "Production")
    {
        var values = new Dictionary<string, string?>();
        if (segredo is not null) values["MercadoPago:WebhookSecret"] = segredo;
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(ambiente);

        var clock = new Mock<IClock>();
        clock.SetupGet(c => c.UtcNow).Returns(Agora);

        return new MercadoPagoWebhookSignatureValidator(
            config, env.Object, clock.Object, NullLogger<MercadoPagoWebhookSignatureValidator>.Instance);
    }

    private static string Assinar(string ts, string requestId = RequestId, string dataId = DataId, string segredo = Segredo)
    {
        var manifesto = $"id:{dataId.ToLowerInvariant()};request-id:{requestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(segredo));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifesto))).ToLowerInvariant();
    }

    private static string TsAgora(TimeSpan? offset = null) =>
        new DateTimeOffset(Agora + (offset ?? TimeSpan.Zero)).ToUnixTimeMilliseconds().ToString();

    [Fact]
    public void AssinaturaValida_RetornaTrue()
    {
        var ts = TsAgora();
        var header = $"ts={ts},v1={Assinar(ts)}";

        CriarValidator(Segredo).IsValid(header, RequestId, DataId).Should().BeTrue();
    }

    [Fact]
    public void AssinaturaAdulterada_RetornaFalse()
    {
        var ts = TsAgora();
        var header = $"ts={ts},v1={Assinar(ts, segredo: "outro-segredo")}";

        CriarValidator(Segredo).IsValid(header, RequestId, DataId).Should().BeFalse();
    }

    [Fact]
    public void DataIdAdulterado_RetornaFalse()
    {
        var ts = TsAgora();
        var header = $"ts={ts},v1={Assinar(ts)}";

        CriarValidator(Segredo).IsValid(header, RequestId, "OUTRO-ID").Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("lixo")]
    [InlineData("ts=abc,v1=ff")]
    public void HeaderAusenteOuInvalido_RetornaFalse(string? header)
    {
        CriarValidator(Segredo).IsValid(header, RequestId, DataId).Should().BeFalse();
    }

    [Theory]
    [InlineData(-6)]
    [InlineData(6)]
    public void TsForaDaTolerancia_RetornaFalse(int minutos)
    {
        var ts = TsAgora(TimeSpan.FromMinutes(minutos));
        var header = $"ts={ts},v1={Assinar(ts)}";

        CriarValidator(Segredo).IsValid(header, RequestId, DataId).Should().BeFalse();
    }

    [Fact]
    public void SemSegredoEmDevelopment_RetornaTrue()
    {
        CriarValidator(null, "Development").IsValid(null, null, null).Should().BeTrue();
    }

    [Fact]
    public void SemSegredoForaDeDevelopment_RetornaFalse()
    {
        CriarValidator(null, "Production").IsValid("ts=1,v1=ff", RequestId, DataId).Should().BeFalse();
    }

    [Fact]
    public void HeaderNoFormatoOficial_FazParseSemLancar()
    {
        // Exemplo da documentação oficial: só valida o parse (segredo real desconhecido).
        const string header = "ts=1742505638683,v1=ced36ab6d33566bb1e16c125819b8d840d6b8ef136b0b9127c76064466f5229b";

        var act = () => CriarValidator(Segredo).IsValid(header, RequestId, DataId);

        act.Should().NotThrow();
        act().Should().BeFalse(); // ts muito antigo e assinatura não confere
    }
}
