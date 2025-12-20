Imports System.Globalization
Imports System.Linq
Imports Microsoft.Data.Sqlite

Namespace BankAssets.Repositories
    Public Class TransactionRepository
        Private ReadOnly _database As Data.Database
        Private ReadOnly _encryptionService As Services.EncryptionService

        Public Sub New(database As Data.Database)
            _database = database
            _encryptionService = database.EncryptionService
        End Sub

        Public Function GetTransactions(accountId As Integer, fromDate As DateTime?, toDate As DateTime?) As List(Of Models.AccountTransaction)
            Dim results As New List(Of Models.AccountTransaction)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "SELECT id, account_id, amount, purpose, booking_date, counterparty "
                command.CommandText &= "FROM transactions WHERE account_id = $accountId"
                command.Parameters.AddWithValue("$accountId", accountId)

                Using reader = command.ExecuteReader()
                    While reader.Read()
                        Dim decryptedAmount = Decimal.Parse(_encryptionService.Decrypt(reader.GetString(2)), CultureInfo.InvariantCulture)
                        Dim decryptedPurpose = _encryptionService.Decrypt(reader.GetString(3))
                        Dim decryptedBookingDate = DateTime.Parse(_encryptionService.Decrypt(reader.GetString(4)))
                        Dim decryptedCounterparty = _encryptionService.Decrypt(reader.GetString(5))
                        results.Add(New Models.AccountTransaction With {
                            .Id = reader.GetInt32(0),
                            .AccountId = reader.GetInt32(1),
                            .Amount = decryptedAmount,
                            .Purpose = decryptedPurpose,
                            .BookingDate = decryptedBookingDate,
                            .Counterparty = decryptedCounterparty
                        })
                    End While
                End Using
            End Using
            Dim filtered = results
            If fromDate.HasValue Then
                filtered = filtered.Where(Function(t) t.BookingDate.Date >= fromDate.Value.Date).ToList()
            End If
            If toDate.HasValue Then
                filtered = filtered.Where(Function(t) t.BookingDate.Date <= toDate.Value.Date).ToList()
            End If
            Return filtered.OrderByDescending(Function(t) t.BookingDate).ToList()
        End Function

        Public Sub AddTransaction(transaction As Models.AccountTransaction)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "INSERT INTO transactions (account_id, amount, purpose, booking_date, counterparty) "
                command.CommandText &= "VALUES ($accountId, $amount, $purpose, $bookingDate, $counterparty)"
                command.Parameters.AddWithValue("$accountId", transaction.AccountId)
                command.Parameters.AddWithValue("$amount", _encryptionService.Encrypt(transaction.Amount.ToString(CultureInfo.InvariantCulture)))
                command.Parameters.AddWithValue("$purpose", _encryptionService.Encrypt(transaction.Purpose))
                command.Parameters.AddWithValue("$bookingDate", _encryptionService.Encrypt(transaction.BookingDate.ToString("O")))
                command.Parameters.AddWithValue("$counterparty", _encryptionService.Encrypt(transaction.Counterparty))
                command.ExecuteNonQuery()
            End Using
        End Sub

        Public Function GetMonthlyBalances(accountId As Integer) As Dictionary(Of DateTime, Decimal)
            Dim balances As New Dictionary(Of DateTime, Decimal)
            Dim transactions = GetTransactions(accountId, Nothing, Nothing)
            Dim grouped = transactions.GroupBy(Function(t) New DateTime(t.BookingDate.Year, t.BookingDate.Month, 1)) _
                .OrderBy(Function(g) g.Key)

            Dim runningTotal As Decimal = 0
            For Each group In grouped
                runningTotal += group.Sum(Function(t) t.Amount)
                balances(group.Key) = runningTotal
            Next

            Return balances
        End Function
    End Class
End Namespace
