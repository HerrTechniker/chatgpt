Namespace BankAssets.Services
    Public Class BankSyncService
        Private ReadOnly _connectors As Dictionary(Of String, IBankConnector)

        Public Sub New()
            _connectors = New Dictionary(Of String, IBankConnector)(StringComparer.OrdinalIgnoreCase) From {
                {"offline", New OfflineBankConnector()},
                {"nordigen", New NordigenConnector()}
            }
        End Sub

        Public Function GetConnector(apiType As String) As IBankConnector
            If _connectors.ContainsKey(apiType) Then
                Return _connectors(apiType)
            End If

            Throw New NotSupportedException($"No connector registered for api type '{apiType}'.")
        End Function
    End Class
End Namespace
