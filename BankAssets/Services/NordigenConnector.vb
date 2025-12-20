Namespace BankAssets.Services
    Public Class NordigenConnector
        Implements IBankConnector

        Public Function GetAccounts(credentials As String) As List(Of Models.Account) Implements IBankConnector.GetAccounts
            Throw New NotSupportedException("Nordigen-Demo ist noch nicht implementiert. Bitte API-Zugangsdaten konfigurieren und den Connector ergänzen.")
        End Function

        Public Function GetTransactions(credentials As String, account As Models.Account) As List(Of Models.AccountTransaction) Implements IBankConnector.GetTransactions
            Throw New NotSupportedException("Nordigen-Demo ist noch nicht implementiert. Bitte API-Zugangsdaten konfigurieren und den Connector ergänzen.")
        End Function
    End Class
End Namespace
