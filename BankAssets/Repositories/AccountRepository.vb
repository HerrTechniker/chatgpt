Imports System.Globalization
Imports System.Linq
Imports Microsoft.Data.Sqlite

Namespace BankAssets.Repositories
    Public Class AccountRepository
        Private ReadOnly _database As Data.Database
        Private ReadOnly _encryptionService As Services.EncryptionService

        Public Sub New(database As Data.Database)
            _database = database
            _encryptionService = database.EncryptionService
        End Sub

        Public Function GetAccountsWithBank() As List(Of (Models.Account, Models.Bank))
            Dim results As New List(Of (Models.Account, Models.Bank))
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = ""
                command.CommandText &= "SELECT a.id, a.bank_id, a.name, a.iban, a.current_balance, a.last_synced_at, "
                command.CommandText &= "b.id, b.name, b.api_type "
                command.CommandText &= "FROM accounts a "
                command.CommandText &= "INNER JOIN banks b ON b.id = a.bank_id"

                Using reader = command.ExecuteReader()
                    While reader.Read()
                        Dim decryptedAccountName = _encryptionService.Decrypt(reader.GetString(2))
                        Dim decryptedIban = _encryptionService.Decrypt(reader.GetString(3))
                        Dim decryptedBalance = Decimal.Parse(_encryptionService.Decrypt(reader.GetString(4)), CultureInfo.InvariantCulture)
                        Dim account = New Models.Account With {
                            .Id = reader.GetInt32(0),
                            .BankId = reader.GetInt32(1),
                            .Name = decryptedAccountName,
                            .Iban = decryptedIban,
                            .CurrentBalance = decryptedBalance
                        }
                        If Not reader.IsDBNull(5) Then
                            account.LastSyncedAt = DateTime.Parse(reader.GetString(5))
                        End If

                        Dim decryptedBankName = _encryptionService.Decrypt(reader.GetString(7))
                        Dim bank = New Models.Bank With {
                            .Id = reader.GetInt32(6),
                            .Name = decryptedBankName,
                            .ApiType = reader.GetString(8)
                        }
                        results.Add((account, bank))
                    End While
                End Using
            End Using

            Return results.OrderBy(Function(entry) entry.Item2.Name).ThenBy(Function(entry) entry.Item1.Name).ToList()
        End Function

        Public Function AddAccount(account As Models.Account) As Integer
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "INSERT INTO accounts (bank_id, name, iban, current_balance, last_synced_at) "
                command.CommandText &= "VALUES ($bankId, $name, $iban, $balance, $lastSynced); SELECT last_insert_rowid();"
                command.Parameters.AddWithValue("$bankId", account.BankId)
                command.Parameters.AddWithValue("$name", _encryptionService.Encrypt(account.Name))
                command.Parameters.AddWithValue("$iban", _encryptionService.Encrypt(account.Iban))
                command.Parameters.AddWithValue("$balance", _encryptionService.Encrypt(account.CurrentBalance.ToString(CultureInfo.InvariantCulture)))
                command.Parameters.AddWithValue("$lastSynced", If(account.LastSyncedAt.HasValue, account.LastSyncedAt.Value.ToString("O"), CType(DBNull.Value, Object)))
                Return Convert.ToInt32(command.ExecuteScalar())
            End Using
        End Function

        Public Sub UpdateBalance(accountId As Integer, balance As Decimal)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "UPDATE accounts SET current_balance = $balance WHERE id = $id"
                command.Parameters.AddWithValue("$balance", _encryptionService.Encrypt(balance.ToString(CultureInfo.InvariantCulture)))
                command.Parameters.AddWithValue("$id", accountId)
                command.ExecuteNonQuery()
            End Using
        End Sub

        Public Sub UpdateAccount(accountId As Integer, name As String, iban As String)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "UPDATE accounts SET name = $name, iban = $iban WHERE id = $id"
                command.Parameters.AddWithValue("$name", _encryptionService.Encrypt(name))
                command.Parameters.AddWithValue("$iban", _encryptionService.Encrypt(iban))
                command.Parameters.AddWithValue("$id", accountId)
                command.ExecuteNonQuery()
            End Using
        End Sub

        Public Sub DeleteAccount(accountId As Integer)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "DELETE FROM accounts WHERE id = $id"
                command.Parameters.AddWithValue("$id", accountId)
                command.ExecuteNonQuery()
            End Using
        End Sub

        Public Function CountAccountsForBank(bankId As Integer) As Integer
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "SELECT COUNT(*) FROM accounts WHERE bank_id = $bankId"
                command.Parameters.AddWithValue("$bankId", bankId)
                Return Convert.ToInt32(command.ExecuteScalar())
            End Using
        End Function

        Public Function GetAccountsForBank(bankId As Integer) As List(Of Models.Account)
            Dim results As New List(Of Models.Account)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "SELECT id, bank_id, name, iban, current_balance, last_synced_at FROM accounts WHERE bank_id = $bankId"
                command.Parameters.AddWithValue("$bankId", bankId)
                Using reader = command.ExecuteReader()
                    While reader.Read()
                        Dim account = New Models.Account With {
                            .Id = reader.GetInt32(0),
                            .BankId = reader.GetInt32(1),
                            .Name = _encryptionService.Decrypt(reader.GetString(2)),
                            .Iban = _encryptionService.Decrypt(reader.GetString(3)),
                            .CurrentBalance = Decimal.Parse(_encryptionService.Decrypt(reader.GetString(4)), CultureInfo.InvariantCulture)
                        }
                        If Not reader.IsDBNull(5) Then
                            account.LastSyncedAt = DateTime.Parse(reader.GetString(5))
                        End If
                        results.Add(account)
                    End While
                End Using
            End Using
            Return results
        End Function
    End Class
End Namespace
