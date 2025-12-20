Namespace BankAssets.Services
    Public Interface IBankConnector
        Function GetAccounts(credentials As String) As List(Of Models.Account)
        Function GetTransactions(credentials As String, account As Models.Account) As List(Of Models.AccountTransaction)
    End Interface
End Namespace
