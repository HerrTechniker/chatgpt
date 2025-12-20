Imports System.Drawing
Imports System.Windows.Forms

Namespace BankAssets.Forms
    Public Class AccountCard
        Inherits Panel

        Private ReadOnly _titleLabel As Label
        Private ReadOnly _ibanLabel As Label
        Private ReadOnly _balanceLabel As Label
        Private ReadOnly _iconBox As Panel

        Public Sub New()
            DoubleBuffered = True
            BackColor = Theme.SurfaceColor
            Height = 90
            Width = 480
            Padding = New Padding(12)
            Cursor = Cursors.Hand

            _iconBox = New Panel With {
                .BackColor = Theme.AccentColor,
                .Width = 36,
                .Height = 36,
                .Left = 12,
                .Top = 12
            }
            _iconBox.Region = New Region(New Drawing2D.GraphicsPath(New PointF() {
                New PointF(18, 0),
                New PointF(36, 18),
                New PointF(18, 36),
                New PointF(0, 18)
            }, New Byte() {
                Drawing2D.PathPointType.Start,
                Drawing2D.PathPointType.Line,
                Drawing2D.PathPointType.Line,
                Drawing2D.PathPointType.Line
            }))

            _titleLabel = New Label With {
                .AutoSize = False,
                .Left = 60,
                .Top = 12,
                .Width = 260,
                .Height = 22,
                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                .ForeColor = Theme.TextColor
            }

            _ibanLabel = New Label With {
                .AutoSize = False,
                .Left = 60,
                .Top = 38,
                .Width = 260,
                .Height = 18,
                .Font = New Font("Segoe UI", 9, FontStyle.Regular),
                .ForeColor = Theme.MutedTextColor
            }

            _balanceLabel = New Label With {
                .AutoSize = False,
                .Left = 330,
                .Top = 12,
                .Width = 130,
                .Height = 22,
                .TextAlign = ContentAlignment.MiddleRight,
                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                .ForeColor = Theme.TextColor
            }

            Controls.Add(_iconBox)
            Controls.Add(_titleLabel)
            Controls.Add(_ibanLabel)
            Controls.Add(_balanceLabel)

            For Each control As Control In Controls
                AddHandler control.Click, AddressOf HandleCardClick
            Next
        End Sub

        Public Property AccountName As String
            Get
                Return _titleLabel.Text
            End Get
            Set(value As String)
                _titleLabel.Text = value
            End Set
        End Property

        Public Property AccountIban As String
            Get
                Return _ibanLabel.Text
            End Get
            Set(value As String)
                _ibanLabel.Text = value
            End Set
        End Property

        Public Property AccountBalance As String
            Get
                Return _balanceLabel.Text
            End Get
            Set(value As String)
                _balanceLabel.Text = value
            End Set
        End Property

        Private Sub HandleCardClick(sender As Object, e As EventArgs)
            OnClick(e)
        End Sub
    End Class
End Namespace
