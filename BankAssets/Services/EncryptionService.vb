Imports System.Security.Cryptography
Imports System.Text

Namespace BankAssets.Services
    Public Class EncryptionService
        Private ReadOnly _key As Byte()

        Public Sub New(key As Byte())
            _key = key
        End Sub

        Public Function Encrypt(plainText As String) As String
            If String.IsNullOrEmpty(plainText) Then
                Return ""
            End If

            Dim nonce(11) As Byte
            RandomNumberGenerator.Fill(nonce)

            Dim plainBytes = Encoding.UTF8.GetBytes(plainText)
            Dim cipherBytes(plainBytes.Length - 1) As Byte
            Dim tag(15) As Byte

            Using aes As New AesGcm(_key)
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag)
            End Using

            Dim payload = New Byte(nonce.Length + tag.Length + cipherBytes.Length - 1) {}
            Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length)
            Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length)
            Buffer.BlockCopy(cipherBytes, 0, payload, nonce.Length + tag.Length, cipherBytes.Length)

            Return Convert.ToBase64String(payload)
        End Function

        Public Function Decrypt(cipherText As String) As String
            If String.IsNullOrEmpty(cipherText) Then
                Return ""
            End If

            Dim payload = Convert.FromBase64String(cipherText)
            Dim nonce(11) As Byte
            Dim tag(15) As Byte
            Dim cipherBytes(payload.Length - nonce.Length - tag.Length - 1) As Byte

            Buffer.BlockCopy(payload, 0, nonce, 0, nonce.Length)
            Buffer.BlockCopy(payload, nonce.Length, tag, 0, tag.Length)
            Buffer.BlockCopy(payload, nonce.Length + tag.Length, cipherBytes, 0, cipherBytes.Length)

            Dim plainBytes(cipherBytes.Length - 1) As Byte
            Using aes As New AesGcm(_key)
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes)
            End Using

            Return Encoding.UTF8.GetString(plainBytes)
        End Function
    End Class
End Namespace
