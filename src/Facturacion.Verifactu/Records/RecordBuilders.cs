namespace Facturacion.Verifactu.Records;

/// <summary>Registro fiscal listo para persistir, ya validado.</summary>
public sealed record FiscalRecordDraft(
    Guid TenantId,
    Guid CompanyId,
    Guid InvoiceId,
    bool IsAnulacion,
    string IssuerNif,
    string InvoiceNumber,
    DateOnly IssueDate,
    string InvoiceType,
    decimal Total,
    string Hash,
    string PreviousHash,
    Guid? PreviousRecordId,
    string XmlPayload,
    string SoftwareName,
    string SoftwareVersion,
    string InstallationId,
    Guid ChainId);

public interface IRecordBuilder
{
    bool IsAvailable { get; }

    FiscalRecordDraft Build(RecordBuildContext context);
}

public sealed record RecordBuildContext(
    Guid TenantId,
    Guid CompanyId,
    Guid InvoiceId,
    Validation.FiscalData Data,
    Guid? PreviousRecordId,
    string PreviousHash,
    string Hash,
    string XmlPayload,
    string SoftwareName,
    string SoftwareVersion,
    string InstallationId,
    Guid ChainId);

public sealed class AltaRecordBuilder : IRecordBuilder
{
    public bool IsAvailable => true;

    public FiscalRecordDraft Build(RecordBuildContext c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new FiscalRecordDraft(
            c.TenantId, c.CompanyId, c.InvoiceId,
            IsAnulacion: false,
            c.Data.IssuerNif, c.Data.InvoiceNumber, c.Data.IssueDate, c.Data.InvoiceType,
            c.Data.Total, c.Hash, c.PreviousHash, c.PreviousRecordId, c.XmlPayload,
            c.SoftwareName, c.SoftwareVersion, c.InstallationId, c.ChainId);
    }
}

public sealed class AnulacionRecordBuilder : IRecordBuilder
{
    public bool IsAvailable => true;

    public FiscalRecordDraft Build(RecordBuildContext c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new FiscalRecordDraft(
            c.TenantId, c.CompanyId, c.InvoiceId,
            IsAnulacion: true,
            c.Data.IssuerNif, c.Data.InvoiceNumber, c.Data.IssueDate, c.Data.InvoiceType,
            c.Data.Total, c.Hash, c.PreviousHash, c.PreviousRecordId, c.XmlPayload,
            c.SoftwareName, c.SoftwareVersion, c.InstallationId, c.ChainId);
    }
}