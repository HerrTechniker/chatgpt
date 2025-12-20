Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Namespace BankAssets.Forms
    Public Class SetupForm
        Inherits Form

        Private ReadOnly _settingsService As Services.SettingsService
        Private ReadOnly _pathTextBox As TextBox

        Public Sub New(settingsService As Services.SettingsService)
            _settingsService = settingsService

            Text = "Datenbank einrichten"
            Width = 560
            Height = 180
            StartPosition = FormStartPosition.CenterScreen
            KeyPreview = True
            AddHandler KeyDown, AddressOf OnFormKeyDown
            FormBorderStyle = FormBorderStyle.FixedSingle
            MaximizeBox = False

            Theme.Apply(Me)

            Dim instructionLabel = New Label With {
                .Text = "Bitte wählen Sie den Ordner für die SQLite-Datenbank aus.",
                .AutoSize = True,
                .Top = 20,
                .Left = 20
            }

            _pathTextBox = New TextBox With {
                .Left = 20,
                .Top = 55,
                .Width = 400,
                .ReadOnly = True,
                .BackColor = Color.White
            }

            Dim browseButton = New Button With {
                .Text = "Durchsuchen...",
                .Left = 430,
                .Top = 52,
                .Width = 100,
                .Height = 30
            }
            AddHandler browseButton.Click, AddressOf OnBrowseClicked

            Dim saveButton = New Button With {
                .Text = "Speichern",
                .Left = 360,
                .Top = 95,
                .Width = 90,
                .Height = 30,
                .DialogResult = DialogResult.OK
            }
            AddHandler saveButton.Click, AddressOf OnSaveClicked

            Dim cancelButton = New Button With {
                .Text = "Abbrechen",
                .Left = 450,
                .Top = 95,
                .Width = 90,
                .Height = 30,
                .DialogResult = DialogResult.Cancel
            }

            Controls.Add(instructionLabel)
            Controls.Add(_pathTextBox)
            Controls.Add(browseButton)
            Controls.Add(saveButton)
            Controls.Add(cancelButton)
        End Sub

        Private Sub OnBrowseClicked(sender As Object, e As EventArgs)
            Using dialog = New FolderBrowserDialog()
                dialog.Description = "Ordner für die Datenbank auswählen"
                If dialog.ShowDialog() = DialogResult.OK Then
                    _pathTextBox.Text = Path.Combine(dialog.SelectedPath, "bank-assets.db")
                End If
            End Using
        End Sub

        Private Sub OnSaveClicked(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(_pathTextBox.Text) Then
                MessageBox.Show("Bitte wählen Sie einen Ordner aus.", "Fehlende Angabe", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                DialogResult = DialogResult.None
                Return
            End If

            _settingsService.SaveDatabasePath(_pathTextBox.Text)
        End Sub

        Private Sub OnFormKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Escape Then
                DialogResult = DialogResult.Cancel
                Close()
            End If
        End Sub
    End Class
End Namespace
