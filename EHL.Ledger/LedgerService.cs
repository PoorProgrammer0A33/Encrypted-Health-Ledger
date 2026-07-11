using Oracle.ManagedDataAccess.Client;

namespace EHL.Ledger;

public class LedgerService
{
    private readonly string _connectionString;

    public LedgerService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void LogEvent(string eventType, string ciphertextHash)
    {
        using var connection = new OracleConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO ehl_audit_log (event_type, ciphertext_hash)
            VALUES (:eventType, :ciphertextHash)";

        command.Parameters.Add(new OracleParameter("eventType", eventType));
        command.Parameters.Add(new OracleParameter("ciphertextHash", ciphertextHash));

        command.ExecuteNonQuery();
    }
}