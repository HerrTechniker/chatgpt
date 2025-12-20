Imports System.IO
Imports Microsoft.Data.Sqlite

Namespace BankAssets.Data
    Public Class Database
        Private ReadOnly _settingsService As Services.SettingsService
        Private ReadOnly _encryptionService As Services.EncryptionService

        Public Sub New(settingsService As Services.SettingsService)
            _settingsService = settingsService
            _encryptionService = New Services.EncryptionService(settingsService.GetEncryptionKey())
        End Sub

        Public ReadOnly Property EncryptionService As Services.EncryptionService
            Get
                Return _encryptionService
            End Get
        End Property

        Public Function CreateConnection() As SqliteConnection
            Dim dbPath = _settingsService.LoadDatabasePath()
            Dim connectionString = New SqliteConnectionStringBuilder With {
                .DataSource = dbPath
            }
            Return New SqliteConnection(connectionString.ToString())
        End Function

        Public Sub Initialize()
            Dim dbPath = _settingsService.LoadDatabasePath()
            If String.IsNullOrWhiteSpace(dbPath) Then
                Throw New InvalidOperationException("Database path is not configured.")
            End If

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath))
            Using connection = CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = """
                CREATE TABLE IF NOT EXISTS banks (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    api_type TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS accounts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    bank_id INTEGER NOT NULL,
                    name TEXT NOT NULL,
                    iban TEXT NOT NULL,
                    current_balance TEXT NOT NULL,
                    last_synced_at TEXT NULL,
                    FOREIGN KEY(bank_id) REFERENCES banks(id)
                );
                CREATE TABLE IF NOT EXISTS transactions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    account_id INTEGER NOT NULL,
                    amount TEXT NOT NULL,
                    purpose TEXT NOT NULL,
                    booking_date TEXT NOT NULL,
                    counterparty TEXT NOT NULL,
                    FOREIGN KEY(account_id) REFERENCES accounts(id)
                );
                """
                command.ExecuteNonQuery()
            End Using
        End Sub
    End Class
End Namespace
