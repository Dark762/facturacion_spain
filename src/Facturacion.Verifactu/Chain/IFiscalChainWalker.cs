namespace Facturacion.Verifactu.Chain;

/// <summary>Estado del encadenamiento para la siguiente factura de un emisor.</summary>
public sealed record ChainHead(Guid? LastRecordId, string LastHash);

/// <summary>
/// Encadena un registro con el anterior y verifica la continuidad.
/// La FAQ oficial confirma que la huella del registro previo se incrusta en el
/// siguiente, y que un SIF debe comprobarlo al generar un registro nuevo.
/// </summary>
public interface IFiscalChainWalker
{
    ChainHead BuildNextHead(ChainHead current, Guid newRecordId, string newHash);

    /// <summary>
    /// Verifica que un registro encadena correctamente con su anterior.
    /// Devuelve el motivo del fallo, o null si la cadena es coherente.
    /// </summary>
    string? VerifyChaining(string? previousHash, string previousRecordHash, string currentHash);
}

public sealed class FiscalChainWalker : IFiscalChainWalker
{
    public ChainHead BuildNextHead(ChainHead current, Guid newRecordId, string newHash)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentException.ThrowIfNullOrWhiteSpace(newHash);

        return new ChainHead(newRecordId, newHash);
    }

    public string? VerifyChaining(
        string? previousHash,
        string previousRecordHash,
        string currentHash)
    {
        if (string.IsNullOrWhiteSpace(currentHash))
            return "La huella del registro actual esta vacia.";

        if (currentHash.Length != 64)
            return $"La huella del registro actual no mide 64 caracteres hexadecimales: {currentHash.Length}.";

        if (string.IsNullOrWhiteSpace(previousHash))
            return previousRecordHash.Length == 0
                ? null
                : "Hay una huella previa pero el registro no la encadena: la cadena esta rota.";

        if (!string.Equals(previousHash, previousRecordHash, StringComparison.OrdinalIgnoreCase))
            return "La huella encadenada no coincide con la huella real del registro anterior.";

        return null;
    }
}