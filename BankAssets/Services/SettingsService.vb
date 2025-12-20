Imports System.IO
Imports System.Text.Json

Namespace BankAssets.Services
    Public Class SettingsService
        Private ReadOnly _settingsPath As String

        Public Sub New(settingsPath As String)
            _settingsPath = settingsPath
        End Sub

        Public Function LoadDatabasePath() As String
            If Not File.Exists(_settingsPath) Then
                Return ""
            End If

            Dim json = File.ReadAllText(_settingsPath)
            Dim settings = JsonSerializer.Deserialize(Of AppSettings)(json)
            If settings Is Nothing Then
                Return ""
            End If

            Return settings.DatabasePath
        End Function

        Public Sub SaveDatabasePath(path As String)
            Dim settings = New AppSettings With {
                .DatabasePath = path
            }
            Dim json = JsonSerializer.Serialize(settings, New JsonSerializerOptions With {.WriteIndented = True})
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath))
            File.WriteAllText(_settingsPath, json)
        End Sub

        Private Class AppSettings
            Public Property DatabasePath As String = ""
        End Class
    End Class
End Namespace
