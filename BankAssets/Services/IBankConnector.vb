Namespace BankAssets.Services
    Public Interface IBankConnector
        Function GetAccounts() As List(Of Models.Account)
        Function GetTransactions(account As Models.Account) As List(Of Models.AccountTransaction)
    End Interface
End Namespace
