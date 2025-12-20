Imports Microsoft.Data.Sqlite

Namespace BankAssets.Repositories
    Public Class TransactionRepository
        Private ReadOnly _database As Data.Database

        Public Sub New(database As Data.Database)
            _database = database
        End Sub

        Public Function GetTransactions(accountId As Integer, fromDate As DateTime?, toDate As DateTime?) As List(Of Models.AccountTransaction)
            Dim results As New List(Of Models.AccountTransaction)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "SELECT id, account_id, amount, purpose, booking_date, counterparty "
                command.CommandText &= "FROM transactions WHERE account_id = $accountId"
                command.Parameters.AddWithValue("$accountId", accountId)

                If fromDate.HasValue Then
                    command.CommandText &= " AND booking_date >= $fromDate"
                    command.Parameters.AddWithValue("$fromDate", fromDate.Value.ToString("O"))
                End If
                If toDate.HasValue Then
                    command.CommandText &= " AND booking_date <= $toDate"
                    command.Parameters.AddWithValue("$toDate", toDate.Value.ToString("O"))
                End If
                command.CommandText &= " ORDER BY booking_date DESC"

                Using reader = command.ExecuteReader()
                    While reader.Read()
                        results.Add(New Models.AccountTransaction With {
                            .Id = reader.GetInt32(0),
                            .AccountId = reader.GetInt32(1),
                            .Amount = Convert.ToDecimal(reader.GetDouble(2)),
                            .Purpose = reader.GetString(3),
                            .BookingDate = DateTime.Parse(reader.GetString(4)),
                            .Counterparty = reader.GetString(5)
                        })
                    End While
                End Using
            End Using
            Return results
        End Function

        Public Sub AddTransaction(transaction As Models.AccountTransaction)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "INSERT INTO transactions (account_id, amount, purpose, booking_date, counterparty) "
                command.CommandText &= "VALUES ($accountId, $amount, $purpose, $bookingDate, $counterparty)"
                command.Parameters.AddWithValue("$accountId", transaction.AccountId)
                command.Parameters.AddWithValue("$amount", transaction.Amount)
                command.Parameters.AddWithValue("$purpose", transaction.Purpose)
                command.Parameters.AddWithValue("$bookingDate", transaction.BookingDate.ToString("O"))
                command.Parameters.AddWithValue("$counterparty", transaction.Counterparty)
                command.ExecuteNonQuery()
            End Using
        End Sub

        Public Function GetMonthlyBalances(accountId As Integer) As Dictionary(Of DateTime, Decimal)
            Dim balances As New Dictionary(Of DateTime, Decimal)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = ""
                command.CommandText &= "SELECT strftime('%Y-%m-01', booking_date) AS month_start, SUM(amount) "
                command.CommandText &= "FROM transactions WHERE account_id = $accountId "
                command.CommandText &= "GROUP BY month_start ORDER BY month_start ASC"
                command.Parameters.AddWithValue("$accountId", accountId)

                Using reader = command.ExecuteReader()
                    Dim runningTotal As Decimal = 0
                    While reader.Read()
                        Dim month = DateTime.Parse(reader.GetString(0))
                        runningTotal += Convert.ToDecimal(reader.GetDouble(1))
                        balances(month) = runningTotal
                    End While
                End Using
            End Using

            Return balances
        End Function
    End Class
End Namespace
