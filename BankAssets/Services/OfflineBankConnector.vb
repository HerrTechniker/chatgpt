Namespace BankAssets.Services
    Public Class OfflineBankConnector
        Implements IBankConnector

        Public Function GetAccounts(credentials As String) As List(Of Models.Account) Implements IBankConnector.GetAccounts
            Return New List(Of Models.Account)()
        End Function

        Public Function GetTransactions(credentials As String, account As Models.Account) As List(Of Models.AccountTransaction) Implements IBankConnector.GetTransactions
            Return New List(Of Models.AccountTransaction)()
        End Function
    End Class
End Namespace
