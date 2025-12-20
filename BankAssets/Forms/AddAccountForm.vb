Imports System.Linq
Imports System.Windows.Forms

Namespace BankAssets.Forms
    Public Class AddAccountForm
        Inherits Form

        Private ReadOnly _templates As List(Of Models.BankTemplate)
        Private ReadOnly _bankCombo As ComboBox
        Private ReadOnly _bankNameBox As TextBox
        Private ReadOnly _accountNameBox As TextBox
        Private ReadOnly _ibanBox As TextBox
        Private ReadOnly _credentialsBox As TextBox

        Public Sub New(templates As List(Of Models.BankTemplate))
            _templates = templates

            Text = "Konto hinzufügen"
            Width = 540
            Height = 390
            StartPosition = FormStartPosition.CenterParent
            KeyPreview = True
            AddHandler KeyDown, AddressOf OnFormKeyDown
            FormBorderStyle = FormBorderStyle.FixedSingle
            MaximizeBox = False

            Theme.Apply(Me)

            Dim templateLabel = New Label With {.Text = "Bankvorlage", .Left = 20, .Top = 20, .AutoSize = True}
            _bankCombo = New ComboBox With {.Left = 190, .Top = 16, .Width = 300, .DropDownStyle = ComboBoxStyle.DropDownList}
            _bankCombo.Items.AddRange(_templates.Cast(Of Object).ToArray())
            _bankCombo.Items.Add("Benutzerdefiniert")
            _bankCombo.SelectedIndex = 0
            AddHandler _bankCombo.SelectedIndexChanged, AddressOf OnTemplateChanged

            Dim bankNameLabel = New Label With {.Text = "Bankname", .Left = 20, .Top = 60, .AutoSize = True}
            _bankNameBox = New TextBox With {.Left = 190, .Top = 56, .Width = 300, .Enabled = False}

            Dim accountNameLabel = New Label With {.Text = "Kontoname", .Left = 20, .Top = 100, .AutoSize = True}
            _accountNameBox = New TextBox With {.Left = 190, .Top = 96, .Width = 300}

            Dim ibanLabel = New Label With {.Text = "IBAN", .Left = 20, .Top = 140, .AutoSize = True}
            _ibanBox = New TextBox With {.Left = 190, .Top = 136, .Width = 300}

            Dim credentialsLabel = New Label With {.Text = "API-Zugang (optional)", .Left = 20, .Top = 180, .AutoSize = True}
            _credentialsBox = New TextBox With {.Left = 190, .Top = 176, .Width = 300}

            Dim saveButton = New Button With {.Text = "Speichern", .Left = 260, .Top = 230, .Width = 90, .Height = 30, .DialogResult = DialogResult.OK}
            AddHandler saveButton.Click, AddressOf OnSave

            Dim cancelButton = New Button With {.Text = "Abbrechen", .Left = 360, .Top = 230, .Width = 90, .Height = 30, .DialogResult = DialogResult.Cancel}

            Controls.Add(templateLabel)
            Controls.Add(_bankCombo)
            Controls.Add(bankNameLabel)
            Controls.Add(_bankNameBox)
            Controls.Add(accountNameLabel)
            Controls.Add(_accountNameBox)
            Controls.Add(ibanLabel)
            Controls.Add(_ibanBox)
            Controls.Add(credentialsLabel)
            Controls.Add(_credentialsBox)
            Controls.Add(saveButton)
            Controls.Add(cancelButton)
        End Sub

        Public Function GetSelection() As (String, String, String, String, String)
            Dim template = TryCast(_bankCombo.SelectedItem, Models.BankTemplate)
            Dim bankName = If(template Is Nothing, _bankNameBox.Text, template.Name)
            Dim apiType = If(template Is Nothing, "offline", template.ApiType)

            Return (bankName, apiType, _accountNameBox.Text, _ibanBox.Text, _credentialsBox.Text)
        End Function

        Private Sub OnTemplateChanged(sender As Object, e As EventArgs)
            Dim template = TryCast(_bankCombo.SelectedItem, Models.BankTemplate)
            If template Is Nothing Then
                _bankNameBox.Enabled = True
                _bankNameBox.Text = ""
            Else
                _bankNameBox.Enabled = False
                _bankNameBox.Text = template.Name
            End If
        End Sub

        Private Sub OnSave(sender As Object, e As EventArgs)
            Dim selection = GetSelection()
            If String.IsNullOrWhiteSpace(selection.Item1) OrElse String.IsNullOrWhiteSpace(selection.Item3) OrElse String.IsNullOrWhiteSpace(selection.Item4) Then
                MessageBox.Show("Bitte Bankname, Kontoname und IBAN angeben.", "Fehlende Angaben", MessageBoxButtons.OK, MessageBoxIcon.Warning)
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
