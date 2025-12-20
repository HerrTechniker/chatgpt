Imports Microsoft.Data.Sqlite

Namespace BankAssets.Repositories
    Public Class AccountRepository
        Private ReadOnly _database As Data.Database

        Public Sub New(database As Data.Database)
            _database = database
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
                command.CommandText &= "INNER JOIN banks b ON b.id = a.bank_id "
                command.CommandText &= "ORDER BY b.name ASC, a.name ASC"

                Using reader = command.ExecuteReader()
                    While reader.Read()
                        Dim account = New Models.Account With {
                            .Id = reader.GetInt32(0),
                            .BankId = reader.GetInt32(1),
                            .Name = reader.GetString(2),
                            .Iban = reader.GetString(3),
                            .CurrentBalance = Convert.ToDecimal(reader.GetDouble(4))
                        }
                        If Not reader.IsDBNull(5) Then
                            account.LastSyncedAt = DateTime.Parse(reader.GetString(5))
                        End If

                        Dim bank = New Models.Bank With {
                            .Id = reader.GetInt32(6),
                            .Name = reader.GetString(7),
                            .ApiType = reader.GetString(8)
                        }
                        results.Add((account, bank))
                    End While
                End Using
            End Using

            Return results
        End Function

        Public Function AddAccount(account As Models.Account) As Integer
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "INSERT INTO accounts (bank_id, name, iban, current_balance, last_synced_at) "
                command.CommandText &= "VALUES ($bankId, $name, $iban, $balance, $lastSynced); SELECT last_insert_rowid();"
                command.Parameters.AddWithValue("$bankId", account.BankId)
                command.Parameters.AddWithValue("$name", account.Name)
                command.Parameters.AddWithValue("$iban", account.Iban)
                command.Parameters.AddWithValue("$balance", account.CurrentBalance)
                command.Parameters.AddWithValue("$lastSynced", If(account.LastSyncedAt.HasValue, account.LastSyncedAt.Value.ToString("O"), CType(DBNull.Value, Object)))
                Return Convert.ToInt32(command.ExecuteScalar())
            End Using
        End Function

        Public Sub UpdateBalance(accountId As Integer, balance As Decimal)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "UPDATE accounts SET current_balance = $balance WHERE id = $id"
                command.Parameters.AddWithValue("$balance", balance)
                command.Parameters.AddWithValue("$id", accountId)
                command.ExecuteNonQuery()
            End Using
        End Sub
    End Class
End Namespace
