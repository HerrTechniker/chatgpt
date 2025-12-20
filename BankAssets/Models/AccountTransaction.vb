Namespace BankAssets.Models
    Public Class AccountTransaction
        Public Property Id As Integer
        Public Property AccountId As Integer
        Public Property Amount As Decimal
        Public Property Purpose As String = ""
        Public Property BookingDate As DateTime
        Public Property Counterparty As String = ""
    End Class
End Namespace
