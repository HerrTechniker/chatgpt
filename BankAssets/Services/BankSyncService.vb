Namespace BankAssets.Services
    Public Class BankSyncService
        Public Function GetConnector(apiType As String) As IBankConnector
            Throw New NotSupportedException($"No connector registered for api type '{apiType}'.")
        End Function
    End Class
End Namespace
