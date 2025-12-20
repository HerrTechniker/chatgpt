Imports System.Globalization
Imports System.Windows.Forms

Namespace BankAssets.Forms
    Public Class TransactionEntryForm
        Inherits Form

        Private ReadOnly _account As Models.Account
        Private ReadOnly _amountBox As TextBox
        Private ReadOnly _purposeBox As TextBox
        Private ReadOnly _counterpartyBox As TextBox
        Private ReadOnly _datePicker As DateTimePicker

        Public Sub New(account As Models.Account)
            _account = account

            Text = $"Transaktion - {_account.Name}"
            Width = 420
            Height = 280
            StartPosition = FormStartPosition.CenterParent
            KeyPreview = True
            AddHandler KeyDown, AddressOf OnFormKeyDown

            Theme.Apply(Me)

            Dim amountLabel = New Label With {.Text = "Betrag", .Left = 20, .Top = 20, .AutoSize = True}
            _amountBox = New TextBox With {.Left = 150, .Top = 16, .Width = 200}

            Dim purposeLabel = New Label With {.Text = "Verwendungszweck", .Left = 20, .Top = 60, .AutoSize = True}
            _purposeBox = New TextBox With {.Left = 150, .Top = 56, .Width = 200}

            Dim counterpartyLabel = New Label With {.Text = "Gegenpartei", .Left = 20, .Top = 100, .AutoSize = True}
            _counterpartyBox = New TextBox With {.Left = 150, .Top = 96, .Width = 200}

            Dim dateLabel = New Label With {.Text = "Datum", .Left = 20, .Top = 140, .AutoSize = True}
            _datePicker = New DateTimePicker With {.Left = 150, .Top = 136, .Width = 200, .Format = DateTimePickerFormat.Short}

            Dim saveButton = New Button With {.Text = "Speichern", .Left = 190, .Top = 180, .Width = 90, .Height = 30, .DialogResult = DialogResult.OK}
            AddHandler saveButton.Click, AddressOf OnSave

            Dim cancelButton = New Button With {.Text = "Abbrechen", .Left = 290, .Top = 180, .Width = 90, .Height = 30, .DialogResult = DialogResult.Cancel}

            Controls.Add(amountLabel)
            Controls.Add(_amountBox)
            Controls.Add(purposeLabel)
            Controls.Add(_purposeBox)
            Controls.Add(counterpartyLabel)
            Controls.Add(_counterpartyBox)
            Controls.Add(dateLabel)
            Controls.Add(_datePicker)
            Controls.Add(saveButton)
            Controls.Add(cancelButton)
        End Sub

        Public Function GetTransaction() As Models.AccountTransaction
            Dim amountValue As Decimal
            Decimal.TryParse(_amountBox.Text, NumberStyles.Number, CultureInfo.GetCultureInfo("de-DE"), amountValue)

            Return New Models.AccountTransaction With {
                .AccountId = _account.Id,
                .Amount = amountValue,
                .Purpose = _purposeBox.Text,
                .Counterparty = _counterpartyBox.Text,
                .BookingDate = _datePicker.Value.Date
            }
        End Function

        Private Sub OnSave(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(_amountBox.Text) OrElse String.IsNullOrWhiteSpace(_purposeBox.Text) Then
                MessageBox.Show("Bitte Betrag und Verwendungszweck angeben.", "Fehlende Angaben", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                DialogResult = DialogResult.None
            End If
        End Sub

        Private Sub OnFormKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Escape Then
                DialogResult = DialogResult.Cancel
                Close()
            End If
        End Sub
    End Class
End Namespace
