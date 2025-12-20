Imports System.Drawing
Imports System.Windows.Forms

Namespace BankAssets.Forms
    Public Class TransactionCard
        Inherits Panel

        Private ReadOnly _dateLabel As Label
        Private ReadOnly _counterpartyLabel As Label
        Private ReadOnly _purposeLabel As Label
        Private ReadOnly _amountLabel As Label

        Public Sub New()
            DoubleBuffered = True
            BackColor = Theme.SurfaceColor
            Height = 70
            Width = 560
            Padding = New Padding(12)

            _dateLabel = New Label With {
                .AutoSize = False,
                .Left = 12,
                .Top = 10,
                .Width = 90,
                .Height = 18,
                .Font = New Font("Segoe UI", 9, FontStyle.Bold),
                .ForeColor = Theme.TextColor
            }

            _counterpartyLabel = New Label With {
                .AutoSize = False,
                .Left = 110,
                .Top = 10,
                .Width = 160,
                .Height = 18,
                .Font = New Font("Segoe UI", 9, FontStyle.Regular),
                .ForeColor = Theme.MutedTextColor
            }

            _purposeLabel = New Label With {
                .AutoSize = False,
                .Left = 110,
                .Top = 32,
                .Width = 300,
                .Height = 18,
                .Font = New Font("Segoe UI", 9, FontStyle.Regular),
                .ForeColor = Theme.TextColor
            }

            _amountLabel = New Label With {
                .AutoSize = False,
                .Left = 420,
                .Top = 20,
                .Width = 120,
                .Height = 22,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .ForeColor = Theme.TextColor,
                .TextAlign = ContentAlignment.MiddleRight
            }

            Controls.Add(_dateLabel)
            Controls.Add(_counterpartyLabel)
            Controls.Add(_purposeLabel)
            Controls.Add(_amountLabel)
        End Sub

        Public Property TransactionDate As String
            Get
                Return _dateLabel.Text
            End Get
            Set(value As String)
                _dateLabel.Text = value
            End Set
        End Property

        Public Property Counterparty As String
            Get
                Return _counterpartyLabel.Text
            End Get
            Set(value As String)
                _counterpartyLabel.Text = value
            End Set
        End Property

        Public Property Purpose As String
            Get
                Return _purposeLabel.Text
            End Get
            Set(value As String)
                _purposeLabel.Text = value
            End Set
        End Property

        Public Property Amount As String
            Get
                Return _amountLabel.Text
            End Get
            Set(value As String)
                _amountLabel.Text = value
            End Set
        End Property
    End Class
End Namespace
