Namespace BankAssets.Models
    Public Class Account
        Public Property Id As Integer
        Public Property BankId As Integer
        Public Property Name As String = ""
        Public Property Iban As String = ""
        Public Property CurrentBalance As Decimal
        Public Property LastSyncedAt As DateTime?
    End Class
End Namespace
