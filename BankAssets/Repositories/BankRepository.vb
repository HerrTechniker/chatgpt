Imports System.Linq
Imports Microsoft.Data.Sqlite

Namespace BankAssets.Repositories
    Public Class BankRepository
        Private ReadOnly _database As Data.Database
        Private ReadOnly _encryptionService As Services.EncryptionService

        Public Sub New(database As Data.Database)
            _database = database
            _encryptionService = database.EncryptionService
        End Sub

        Public Function GetAllBanks() As List(Of Models.Bank)
            Dim results As New List(Of Models.Bank)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "SELECT id, name, api_type FROM banks"
                Using reader = command.ExecuteReader()
                    While reader.Read()
                        Dim decryptedName = _encryptionService.Decrypt(reader.GetString(1))
                        results.Add(New Models.Bank With {
                            .Id = reader.GetInt32(0),
                            .Name = decryptedName,
                            .ApiType = reader.GetString(2)
                        })
                    End While
                End Using
            End Using

            Return results.OrderBy(Function(b) b.Name).ToList()
        End Function

        Public Function AddBank(bank As Models.Bank) As Integer
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "INSERT INTO banks (name, api_type) VALUES ($name, $apiType); SELECT last_insert_rowid();"
                command.Parameters.AddWithValue("$name", _encryptionService.Encrypt(bank.Name))
                command.Parameters.AddWithValue("$apiType", bank.ApiType)
                Return Convert.ToInt32(command.ExecuteScalar())
            End Using
        End Function
    End Class
End Namespace
