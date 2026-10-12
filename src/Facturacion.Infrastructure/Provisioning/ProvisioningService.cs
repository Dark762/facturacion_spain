using Facturacion.Application.Provisioning;
using Facturacion.Domain.Entities;
using Facturacion.Infrastructure.Identity;
using Facturacion.Infrastructure.Multitenancy;
using Facturacion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Facturacion.Infrastructure.Provisioning;

public sealed class ProvisioningService : IProvisioningService
{
    private readonly BillingDbContext _billing;
    private readonly Facturacion.Infrastructure.Billing.SelfServiceDbContext _self;
    private readonly AuthDbContext _auth;
    private readonly UserManager<ApplicationUser> _users;
    private readonly TenantContext _tenant;

    public ProvisioningService(BillingDbContext billing, AuthDbContext auth,
        UserManager<ApplicationUser> users, TenantContext tenant,
        Facturacion.Infrastructure.Billing.SelfServiceDbContext self)
    {
        _billing = billing; _auth = auth; _users = users; _tenant = tenant; _self = self;
    }

    // Se invoca desde un orquestador de pagos de confianza, NUNCA directamente desde una ruta pública.
    // La intención de registro es el ancla persistente de idempotencia.
    // Dos DbContext pueden dejar escrituras parciales: los reintentos reparan las relaciones faltantes.
    public async Task<ProvisioningResult> ProvisionAsync(ProvisioningRequest r, CancellationToken ct = default)
    {
        Check(r.RegistrationId != Guid.Empty && !string.IsNullOrWhiteSpace(r.UserId), "Registro inválido.");
        Check(!string.IsNullOrWhiteSpace(r.TenantName) && !string.IsNullOrWhiteSpace(r.CompanyName)
              && !string.IsNullOrWhiteSpace(r.TaxId), "Datos fiscales incompletos.");

        // SelfServiceDbContext no tiene filtro tenant y almacena RegistrationIntent.
        var intent = await _self.RegistrationIntents.SingleOrDefaultAsync(x => x.Id == r.RegistrationId, ct)
            ?? throw new InvalidOperationException("Registro inexistente.");
        Check(intent.UserId == r.UserId, "El registro no corresponde al usuario.");
        var user = await _users.FindByIdAsync(r.UserId);
        Check(user is not null && user.IsActive && user.EmailConfirmed, "Usuario no verificado o inactivo.");

        // CLAIM ATOMICO. La tabla ProvisioningOperations tiene RegistrationId como
        // clave primaria, asi que el primer INSERT que llega es el que gana y
        // fija el TenantId. Cualquier reintento concurrente recibe la clave de
        // unicidad y reanuda ese mismo tenant en lugar de crear otro.
        var claim = await ClaimAsync(r.RegistrationId, ct);
        if (claim.TenantId is null)
            throw new InvalidOperationException(
                "La operación de aprovisionamiento no tiene tenant asignado.");

        // Seguidor: otro proceso posee esta operacion. No se repiten las
        // escrituras (producirian colisiones de clave); se espera a que termine.
        if (!claim.OwnedByCaller)
        {
            for (var wait = 0; wait < 40; wait++)
            {
                var snapshot = await _self.ProvisioningOperations.AsNoTracking()
                    .FirstAsync(x => x.RegistrationId == r.RegistrationId, ct);

                if (snapshot.Status == Facturacion.Infrastructure.Billing.ProvisioningOperationStates.Completed
                    && snapshot.CompanyId is not null)
                {
                    _tenant.SetTenant(snapshot.TenantId!.Value);
                    var done = await _billing.Companys.IgnoreQueryFilters()
                        .AsNoTracking().FirstOrDefaultAsync(x => x.Id == snapshot.CompanyId, ct);
                    if (done is not null)
                        return new ProvisioningResult(snapshot.TenantId.Value, done.Id, true);
                }

                await Task.Delay(150, ct);
            }

            throw new InvalidOperationException(
                "El aprovisionamiento está en curso y no ha finalizado a tiempo.");
        }

        if (intent.TenantId is null)
        {
            intent.TenantId = claim.TenantId;
            intent.Status = "Provisioning";
            await _self.SaveChangesAsync(ct);
        }
        var tenantId = claim.TenantId.Value;
        _tenant.SetTenant(tenantId);
        var existingTenant = await _billing.Tenants.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == tenantId, ct);
        if (!existingTenant)
        {
            _billing.Tenants.Add(new Tenant { Id = tenantId, Name = r.TenantName.Trim() });
            await _billing.SaveChangesAsync(ct);
        }
        var company = await _billing.Companys.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Nif == r.TaxId.Trim(), ct);
        if (company is null)
        {
            company = new Company { TenantId = tenantId, Nif = r.TaxId.Trim(),
                LegalName = r.CompanyName.Trim(), Address = r.Address.Trim(),
                PostalCode = r.PostalCode.Trim(), City = r.City.Trim(), Province = r.Province.Trim() };
            _billing.Companys.Add(company);
            await _billing.SaveChangesAsync(ct);
        }
        var tenantUser = await _auth.TenantUsers.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.UserId == r.UserId, ct);
        if (tenantUser is null)
            _auth.TenantUsers.Add(new TenantUser { TenantId = tenantId, UserId = r.UserId });
        else tenantUser.IsActive = true;
        var companyUser = await _auth.CompanyUsers.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.CompanyId == company.Id && x.UserId == r.UserId, ct);
        if (companyUser is null)
            _auth.CompanyUsers.Add(new CompanyUser { TenantId = tenantId,
                CompanyId = company.Id, UserId = r.UserId, Role = "Admin" });
        else { companyUser.Role = "Admin"; companyUser.IsActive = true; }
        await _auth.SaveChangesAsync(ct);

        claim.CompanyId = company.Id;
        claim.Status = Facturacion.Infrastructure.Billing.ProvisioningOperationStates.Completed;
        claim.UpdatedAtUtc = DateTime.UtcNow;
        await _self.SaveChangesAsync(ct);

        return new ProvisioningResult(tenantId, company.Id, existingTenant);
    }

    /// <summary>
    /// Reclama la operacion de aprovisionamiento. El INSERT condicional es lo
    /// que serializa a los emisores concurrentes: si la clave ya existe, este
    /// proceso NO es el dueno (OwnedByCaller = false) y debe esperar a que el
    /// propietario termine en lugar de repetir las escrituras.
    /// </summary>
    private async Task<Facturacion.Infrastructure.Billing.ProvisioningOperation> ClaimAsync(
        Guid registrationId, CancellationToken ct)
    {
        var existing = await _self.ProvisioningOperations
            .FirstOrDefaultAsync(x => x.RegistrationId == registrationId, ct);
        if (existing is not null)
        {
            existing.OwnedByCaller = false;
            return existing;
        }

        var candidate = new Facturacion.Infrastructure.Billing.ProvisioningOperation
        {
            RegistrationId = registrationId,
            TenantId = Guid.NewGuid(),
            Status = Facturacion.Infrastructure.Billing.ProvisioningOperationStates.Claimed,
            OwnedByCaller = true
        };

        _self.ProvisioningOperations.Add(candidate);
        try
        {
            await _self.SaveChangesAsync(ct);
            return candidate;
        }
        catch (Exception ex) when (IsDuplicateKey(ex))
        {
            // Perdimos la carrera: otro proceso ya fijo su TenantId.
            _self.Entry(candidate).State = EntityState.Detached;
            var winner = await _self.ProvisioningOperations
                .FirstOrDefaultAsync(x => x.RegistrationId == registrationId, ct);
            if (winner is null)
                throw new InvalidOperationException(
                    "No se pudo reclamar la operación de aprovisionamiento.");
            winner.OwnedByCaller = false;
            return winner;
        }
    }

    /// <summary>
    /// Clave duplicada. Se compara por nombre de tipo para no acoplar este
    /// servicio a un proveedor concreto.
    /// </summary>
    private static bool IsDuplicateKey(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            var type = current.GetType().FullName ?? string.Empty;
            var message = current.Message;

            if (type.Contains("MySql") && message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
                return true;
            if (type.Contains("Npgsql") && message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                return true;
            if (type.Contains("SqlException") && message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // SOLO invocar desde un proceso administrativo de confianza, NUNCA desde una ruta pública.
    // Ambos DbContext usan conexiones distintas; esto NO es una transacción distribuida.
    public async Task<BootstrapTenantResponse> BootstrapAsync(BootstrapTenantRequest r, CancellationToken ct)
    {
        Check(!string.IsNullOrWhiteSpace(r.TenantName), "Nombre del tenant obligatorio.");
        Check(!string.IsNullOrWhiteSpace(r.LegalName) && !string.IsNullOrWhiteSpace(r.Nif), "Empresa incompleta.");
        Check(!string.IsNullOrWhiteSpace(r.AdminEmail) && !string.IsNullOrWhiteSpace(r.AdminPassword), "Credenciales incompletas.");
        var email = r.AdminEmail.Trim();
        Check(await _users.FindByEmailAsync(email) is null, "El correo ya está registrado. Usa un usuario nuevo para bootstrap.");

        var tenant = new Tenant { Name = r.TenantName.Trim() };
        _tenant.SetTenant(tenant.Id);
        var company = new Company { TenantId = tenant.Id, Nif = r.Nif.Trim(),
            LegalName = r.LegalName.Trim(), Address = r.Address.Trim(),
            PostalCode = r.PostalCode.Trim(), City = r.City.Trim(), Province = r.Province.Trim() };

        // Guardar Billing primero para no dejar membresías apuntando a una empresa inexistente.
        // En un fallo posterior puede quedar un tenant huérfano: revisar/reparar manualmente.
        _billing.Tenants.Add(tenant);
        _billing.Companys.Add(company);
        await _billing.SaveChangesAsync(ct);

        var user = new ApplicationUser { UserName = email, Email = email,
            DisplayName = r.AdminDisplayName.Trim(), EmailConfirmed = false, IsActive = true };
        var created = await _users.CreateAsync(user, r.AdminPassword);
        if (!created.Succeeded)
            throw new InvalidOperationException("No se pudo crear el administrador: " +
                string.Join("; ", created.Errors.Select(x => x.Description)));

        _auth.TenantUsers.Add(new TenantUser { TenantId = tenant.Id, UserId = user.Id });
        _auth.CompanyUsers.Add(new CompanyUser { TenantId = tenant.Id,
            CompanyId = company.Id, UserId = user.Id, Role = "Admin" });
        await _auth.SaveChangesAsync(ct);
        return new BootstrapTenantResponse(tenant.Id, company.Id, user.Id);
    }

    public async Task<CompanyResponse> CreateCompanyAsync(Guid tenantId, CreateCompanyRequest r, CancellationToken ct)
    {
        Check(_tenant.TenantId == tenantId, "Tenant no autorizado.");
        Check(!string.IsNullOrWhiteSpace(r.Nif) && !string.IsNullOrWhiteSpace(r.LegalName), "NIF y razón social obligatorios.");
        var company = new Company { TenantId = tenantId, Nif = r.Nif.Trim(), LegalName = r.LegalName.Trim(),
            Address = r.Address.Trim(), PostalCode = r.PostalCode.Trim(), City = r.City.Trim(),
            Province = r.Province.Trim(), TradeName = r.TradeName, Email = r.Email };
        _billing.Companys.Add(company);
        await _billing.SaveChangesAsync(ct);
        return new CompanyResponse(company.Id, tenantId, company.Nif, company.LegalName);
    }

    public async Task<string> AddTenantUserAsync(Guid tenantId, CreateTenantUserRequest r, CancellationToken ct)
    {
        Check(_tenant.TenantId == tenantId, "Tenant no autorizado.");
        Check(!string.IsNullOrWhiteSpace(r.Email), "Correo obligatorio.");
        var email = r.Email.Trim();
        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email,
                DisplayName = r.DisplayName.Trim(), EmailConfirmed = false };
            var result = await _users.CreateAsync(user, r.Password);
            if (!result.Succeeded) throw new InvalidOperationException(
                "No se pudo crear el usuario: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        // No alterar contraseñas de usuarios existentes; tampoco revelar detalles a clientes no autorizados.
        var membership = await _auth.TenantUsers.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.UserId == user.Id, ct);
        if (membership is null) _auth.TenantUsers.Add(new TenantUser { TenantId = tenantId, UserId = user.Id });
        else membership.IsActive = true;
        await _auth.SaveChangesAsync(ct);
        return user.Id;
    }

    public async Task AssignCompanyUserAsync(Guid tenantId, Guid companyId, AssignCompanyUserRequest r, CancellationToken ct)
    {
        Check(_tenant.TenantId == tenantId, "Tenant no autorizado.");
        Check(r.Role is "Reader" or "Editor" or "Admin", "Rol inválido.");
        Check(await _billing.Companys.AnyAsync(x => x.Id == companyId && x.IsActive, ct), "Empresa inexistente o inactiva.");
        var user = await _users.FindByEmailAsync(r.Email.Trim());
        Check(user is not null && user.IsActive, "Usuario inexistente o inactivo.");
        Check(await _auth.TenantUsers.AnyAsync(x => x.TenantId == tenantId && x.UserId == user!.Id && x.IsActive, ct),
            "El usuario no pertenece al tenant.");
        var member = await _auth.CompanyUsers.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.CompanyId == companyId && x.UserId == user!.Id, ct);
        if (member is null) _auth.CompanyUsers.Add(new CompanyUser { TenantId = tenantId,
            CompanyId = companyId, UserId = user!.Id, Role = r.Role });
        else { member.Role = r.Role; member.IsActive = true; }
        await _auth.SaveChangesAsync(ct);
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
}
