Imports Microsoft.Data.Sqlite

Namespace BankAssets.Repositories
    Public Class BankRepository
        Private ReadOnly _database As Data.Database

        Public Sub New(database As Data.Database)
            _database = database
        End Sub

        Public Function GetAllBanks() As List(Of Models.Bank)
            Dim results As New List(Of Models.Bank)
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "SELECT id, name, api_type FROM banks ORDER BY name ASC"
                Using reader = command.ExecuteReader()
                    While reader.Read()
                        results.Add(New Models.Bank With {
                            .Id = reader.GetInt32(0),
                            .Name = reader.GetString(1),
                            .ApiType = reader.GetString(2)
                        })
                    End While
                End Using
            End Using

            Return results
        End Function

        Public Function AddBank(bank As Models.Bank) As Integer
            Using connection = _database.CreateConnection()
                connection.Open()
                Dim command = connection.CreateCommand()
                command.CommandText = "INSERT INTO banks (name, api_type) VALUES ($name, $apiType); SELECT last_insert_rowid();"
                command.Parameters.AddWithValue("$name", bank.Name)
                command.Parameters.AddWithValue("$apiType", bank.ApiType)
                Return Convert.ToInt32(command.ExecuteScalar())
            End Using
        End Function
    End Class
End Namespace
