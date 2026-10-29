using Facturacion.Application.Billing;

namespace Facturacion.Integration.Test;

public sealed class RegistrationTypesTests
{
    [Theory]
    [InlineData(FiscalHolderType.Individual, "Individual")]
    [InlineData(FiscalHolderType.LegalEntity, "LegalEntity")]
    public void Fiscal_holder_values_remain_compatible(FiscalHolderType holder, string stored)
    {
        Assert.Equal(stored, holder.ToStorageValue());
        Assert.True(stored.Is(holder));
        Assert.True(RegistrationValues.IsValidFiscalHolder(stored));
    }

    [Theory]
    [InlineData(RegistrationStatus.PendingEmail, "PendingEmail")]
    [InlineData(RegistrationStatus.EmailVerified, "EmailVerified")]
    [InlineData(RegistrationStatus.CheckoutCreated, "CheckoutCreated")]
    public void Registration_status_values_remain_compatible(RegistrationStatus status, string stored)
    {
        Assert.Equal(stored, status.ToStorageValue());
        Assert.True(stored.Is(status));
    }

    [Fact]
    public void Registration_steps_keep_the_existing_order()
    {
        Assert.Equal(0, (int)RegistrationStep.Account);
        Assert.Equal(1, (int)RegistrationStep.EmailVerification);
        Assert.Equal(2, (int)RegistrationStep.CompanyDetails);
        Assert.Equal(3, (int)RegistrationStep.PlanSelection);
    }

    [Fact]
    public void Password_rule_is_shared_by_server_validation()
    {
        Assert.Equal(12, RegistrationRules.MinimumPasswordLength);
        Assert.Equal(6, RegistrationRules.VerificationCodeLength);
        var errors = RegistrationValidation.Validate(new RegisterSaasRequest(
            "test@example.com", new string('A', RegistrationRules.MinimumPasswordLength - 1), "Prueba"));
        Assert.Contains(nameof(RegisterSaasRequest.Password), errors.Keys);
    }
}
