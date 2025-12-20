Imports System.IO
Imports System.Security.Cryptography
Imports System.Text.Json

Namespace BankAssets.Services
    Public Class SettingsService
        Private ReadOnly _settingsPath As String

        Public Sub New(settingsPath As String)
            _settingsPath = settingsPath
        End Sub

        Public Function LoadDatabasePath() As String
            Dim settings = LoadSettings()
            If settings Is Nothing Then
                Return ""
            End If

            Return settings.DatabasePath
        End Function

        Public Sub SaveDatabasePath(path As String)
            Dim settings = LoadSettings()
            If settings Is Nothing Then
                settings = New AppSettings()
            End If

            settings.DatabasePath = path
            SaveSettings(settings)
        End Sub

        Public Function GetEncryptionKey() As Byte()
            Dim settings = LoadSettings()
            If settings Is Nothing Then
                settings = New AppSettings()
            End If

            If String.IsNullOrWhiteSpace(settings.EncryptionKey) Then
                Dim key(31) As Byte
                RandomNumberGenerator.Fill(key)
                settings.EncryptionKey = ProtectKey(key)
                SaveSettings(settings)
                Return key
            End If

            Return UnprotectKey(settings.EncryptionKey)
        End Function

        Private Function LoadSettings() As AppSettings?
            If Not File.Exists(_settingsPath) Then
                Return Nothing
            End If

            Dim json = File.ReadAllText(_settingsPath)
            Return JsonSerializer.Deserialize(Of AppSettings)(json)
        End Function

        Private Sub SaveSettings(settings As AppSettings)
            Dim json = JsonSerializer.Serialize(settings, New JsonSerializerOptions With {.WriteIndented = True})
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath))
            File.WriteAllText(_settingsPath, json)
        End Sub

        Private Shared Function ProtectKey(key As Byte()) As String
            Dim protectedBytes = ProtectedData.Protect(key, Nothing, DataProtectionScope.CurrentUser)
            Return Convert.ToBase64String(protectedBytes)
        End Function

        Private Shared Function UnprotectKey(protectedKey As String) As Byte()
            Dim protectedBytes = Convert.FromBase64String(protectedKey)
            Return ProtectedData.Unprotect(protectedBytes, Nothing, DataProtectionScope.CurrentUser)
        End Function

        Private Class AppSettings
            Public Property DatabasePath As String = ""
            Public Property EncryptionKey As String = ""
        End Class
    End Class
End Namespace
