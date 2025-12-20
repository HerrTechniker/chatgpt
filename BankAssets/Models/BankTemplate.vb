Namespace BankAssets.Models
    Public Class BankTemplate
        Public Property Name As String = ""
        Public Property ApiType As String = "offline"

        Public Overrides Function ToString() As String
            Return Name
        End Function
    End Class
End Namespace
