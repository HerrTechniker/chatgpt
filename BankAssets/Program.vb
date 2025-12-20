Imports System
Imports System.IO
Imports System.Windows.Forms

Namespace BankAssets
    Friend Module Program
        <STAThread>
        Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            Dim settingsService = New Services.SettingsService(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
            Dim dbPath = settingsService.LoadDatabasePath()

            If String.IsNullOrWhiteSpace(dbPath) Then
                Using setupForm As New Forms.SetupForm(settingsService)
                    If setupForm.ShowDialog() <> DialogResult.OK Then
                        Return
                    End If
                End Using
            End If

            Dim database = New Data.Database(settingsService)
            database.Initialize()

            Application.Run(New Forms.MainForm(database, settingsService))
        End Sub
    End Module
End Namespace
