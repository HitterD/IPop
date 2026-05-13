using System.Data.Odbc;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using IPop.Modules.Auth.Application.Abstractions;

namespace IPop.Infrastructure.Legacy;

public sealed class LegacySjatrOdbcAdapter(IConfiguration configuration) : ILegacyHrRepository
{
    private readonly string _connStr = configuration.GetConnectionString("SjatrOdbc")
        ?? throw new InvalidOperationException("Connection string 'SjatrOdbc' missing");

    public async Task<LegacyEmployee?> FindByNikAsync(string nik, CancellationToken cancellationToken)
    {
        await using var conn = new OdbcConnection(_connStr);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT NIK_HRIS, NAMA_KARYAWAN, NAMA_DEPARTEMEN, NAMA_JABATAN, LOKASI, NIK_SANTOS, KEYWORD " +
            "FROM M_karyawan WHERE NIK_HRIS = ?";
        cmd.Parameters.Add(new OdbcParameter("nik", nik));
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<LegacyEmployee?> AuthenticateAsync(string nik, string md5Password, CancellationToken cancellationToken)
    {
        var emp = await FindByNikAsync(nik, cancellationToken);
        if (emp is null)
        {
            return null;
        }

        return string.Equals(emp.Md5Keyword, md5Password, StringComparison.OrdinalIgnoreCase) ? emp : null;
    }

    public async IAsyncEnumerable<LegacyEmployee> StreamAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var conn = new OdbcConnection(_connStr);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT NIK_HRIS, NAMA_KARYAWAN, NAMA_DEPARTEMEN, NAMA_JABATAN, LOKASI, NIK_SANTOS, KEYWORD FROM M_karyawan";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return Map(reader);
        }
    }

    private static LegacyEmployee Map(System.Data.Common.DbDataReader r) => new(
        NikHris: r.GetString(0),
        Name: r.IsDBNull(1) ? string.Empty : r.GetString(1),
        Department: r.IsDBNull(2) ? null : r.GetString(2),
        Position: r.IsDBNull(3) ? null : r.GetString(3),
        Location: r.IsDBNull(4) ? null : r.GetString(4),
        NikSantos: r.IsDBNull(5) ? null : r.GetString(5),
        Md5Keyword: r.IsDBNull(6) ? null : r.GetString(6));
}
