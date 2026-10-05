using System.Data;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;

namespace AlDawahPharma.Infrastructure.Repositories;

public class ExpiryAlertRepository : IExpiryAlertRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;

    public ExpiryAlertRepository(IOracleConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ExpiryAlertDto>> GetAllAsync(string? status = null)
    {
        var list = new List<ExpiryAlertDto>();
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();

        var sql = "SELECT ea.AlertID, ea.MedicineID, m.MedicineName, m.BatchNumber, m.ExpiryDate, " +
                  "m.QuantityInStock, ea.AlertDate, ea.AlertStatus, ea.NotificationSent " +
                  "FROM ExpiryAlert ea " +
                  "JOIN Medicine m ON ea.MedicineID = m.MedicineID " +
                  "WHERE 1=1 ";

        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += "AND LOWER(ea.AlertStatus) = LOWER(:p_status) ";
        }

        sql += "ORDER BY ea.AlertID DESC";

        using var cmd = new OracleCommand(sql, conn);
        if (!string.IsNullOrWhiteSpace(status))
        {
            cmd.Parameters.Add(new OracleParameter("p_status", status.Trim()));
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new ExpiryAlertDto
            {
                AlertID = Convert.ToInt64(reader["AlertID"]),
                MedicineID = Convert.ToInt64(reader["MedicineID"]),
                MedicineName = reader["MedicineName"].ToString(),
                BatchNumber = reader["BatchNumber"].ToString(),
                ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]),
                QuantityInStock = Convert.ToInt32(reader["QuantityInStock"]),
                AlertDate = Convert.ToDateTime(reader["AlertDate"]),
                AlertStatus = reader["AlertStatus"].ToString()!,
                NotificationSent = reader["NotificationSent"].ToString()!
            });
        }
        return list;
    }

    public async Task<bool> UpdateNotificationStatusAsync(long alertId, string notificationSent)
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        using var cmd = new OracleCommand(
            "UPDATE ExpiryAlert SET NotificationSent = :p_sent WHERE AlertID = :p_id", conn);
        cmd.Parameters.Add(new OracleParameter("p_sent", notificationSent.Trim().ToUpper() == "Y" ? "Y" : "N"));
        cmd.Parameters.Add(new OracleParameter("p_id", alertId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<int> RefreshAlertsAsync()
    {
        using var conn = (OracleConnection)await _connectionFactory.CreateOpenConnectionAsync();
        // Insert expired or near expiry medicines that don't already have an alert
        var sql = @"
            DECLARE
                v_count NUMBER := 0;
                v_alert_id NUMBER;
            BEGIN
                FOR m IN (
                    SELECT MedicineID, ExpiryDate,
                           CASE WHEN ExpiryDate < SYSDATE THEN 'Expired' ELSE 'Near Expiry' END AS CalculatedStatus
                    FROM Medicine
                    WHERE ExpiryDate <= SYSDATE + 30
                      AND MedicineID NOT IN (SELECT MedicineID FROM ExpiryAlert)
                ) LOOP
                    SELECT NVL(MAX(AlertID), 0) + 1 INTO v_alert_id FROM ExpiryAlert;
                    INSERT INTO ExpiryAlert (AlertID, MedicineID, AlertDate, AlertStatus, NotificationSent)
                    VALUES (v_alert_id, m.MedicineID, SYSDATE, m.CalculatedStatus, 'N');
                    v_count := v_count + 1;
                END LOOP;
                COMMIT;
                :p_out := v_count;
            END;";

        using var cmd = new OracleCommand(sql, conn);
        var pOut = new OracleParameter("p_out", OracleDbType.Int32, ParameterDirection.Output);
        cmd.Parameters.Add(pOut);
        await cmd.ExecuteNonQueryAsync();

        return Convert.ToInt32(pOut.Value.ToString());
    }
}
