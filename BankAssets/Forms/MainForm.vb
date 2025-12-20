Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting

Namespace BankAssets.Forms
    Public Class MainForm
        Inherits Form

        Private ReadOnly _database As Data.Database
        Private ReadOnly _settingsService As Services.SettingsService
        Private ReadOnly _accountRepository As Repositories.AccountRepository
        Private ReadOnly _bankRepository As Repositories.BankRepository
        Private ReadOnly _listView As ListView
        Private ReadOnly _chart As Chart

        Public Sub New(database As Data.Database, settingsService As Services.SettingsService)
            _database = database
            _settingsService = settingsService
            _accountRepository = New Repositories.AccountRepository(database)
            _bankRepository = New Repositories.BankRepository(database)

            Text = "Vermögensübersicht"
            Width = 1000
            Height = 700
            StartPosition = FormStartPosition.CenterScreen

            Dim addAccountButton = New Button With {
                .Text = "Offline-Konto hinzufügen",
                .Top = 15,
                .Left = 20,
                .Width = 200
            }
            AddHandler addAccountButton.Click, AddressOf OnAddOfflineAccount

            _listView = New ListView With {
                .View = View.Details,
                .FullRowSelect = True,
                .Left = 20,
                .Top = 55,
                .Width = 520,
                .Height = 580
            }
            _listView.Columns.Add("Konto", 180)
            _listView.Columns.Add("IBAN", 180)
            _listView.Columns.Add("Saldo", 120)
            AddHandler _listView.DoubleClick, AddressOf OnAccountDoubleClick

            _chart = New Chart With {
                .Left = 560,
                .Top = 55,
                .Width = 400,
                .Height = 400
            }
            _chart.ChartAreas.Add(New ChartArea("Assets"))
            Dim series = New Series("Banks") With {
                .ChartType = SeriesChartType.Pie
            }
            _chart.Series.Add(series)

            Controls.Add(addAccountButton)
            Controls.Add(_listView)
            Controls.Add(_chart)

            LoadAccounts()
        End Sub

        Private Sub LoadAccounts()
            _listView.Items.Clear()
            _listView.Groups.Clear()
            Dim accounts = _accountRepository.GetAccountsWithBank()

            Dim totalsByBank As New Dictionary(Of String, Decimal)

            For Each entry In accounts
                Dim account = entry.Item1
                Dim bank = entry.Item2
                Dim group As ListViewGroup

                If Not _listView.Groups.ContainsKey(bank.Name) Then
                    group = New ListViewGroup(bank.Name, HorizontalAlignment.Left) With {
                        .Name = bank.Name
                    }
                    _listView.Groups.Add(group)
                Else
                    group = _listView.Groups(bank.Name)
                End If

                Dim item = New ListViewItem(account.Name, group)
                item.SubItems.Add(account.Iban)
                item.SubItems.Add(account.CurrentBalance.ToString("C", CultureInfo.GetCultureInfo("de-DE")))
                item.Tag = account
                _listView.Items.Add(item)

                If Not totalsByBank.ContainsKey(bank.Name) Then
                    totalsByBank(bank.Name) = 0
                End If
                totalsByBank(bank.Name) += account.CurrentBalance
            Next

            Dim series = _chart.Series("Banks")
            series.Points.Clear()
            For Each entry In totalsByBank
                Dim point = series.Points.AddY(entry.Value)
                point.LegendText = entry.Key
                point.Label = entry.Value.ToString("C", CultureInfo.GetCultureInfo("de-DE"))
            Next
        End Sub

        Private Sub OnAccountDoubleClick(sender As Object, e As EventArgs)
            If _listView.SelectedItems.Count = 0 Then
                Return
            End If

            Dim account = CType(_listView.SelectedItems(0).Tag, Models.Account)
            Using detailForm As New AccountDetailForm(_database, account)
                detailForm.ShowDialog()
            End Using

            LoadAccounts()
        End Sub

        Private Sub OnAddOfflineAccount(sender As Object, e As EventArgs)
            Dim bankName = Microsoft.VisualBasic.Interaction.InputBox("Bankname", "Neue Bank")
            If String.IsNullOrWhiteSpace(bankName) Then
                Return
            End If

            Dim accountName = Microsoft.VisualBasic.Interaction.InputBox("Kontoname", "Neues Konto")
            If String.IsNullOrWhiteSpace(accountName) Then
                Return
            End If

            Dim iban = Microsoft.VisualBasic.Interaction.InputBox("IBAN", "Neues Konto")
            If String.IsNullOrWhiteSpace(iban) Then
                Return
            End If

            Dim bankId As Integer
            Dim existingBank = _bankRepository.GetAllBanks().FirstOrDefault(Function(b) b.Name.Equals(bankName, StringComparison.OrdinalIgnoreCase))
            If existingBank Is Nothing Then
                bankId = _bankRepository.AddBank(New Models.Bank With {
                    .Name = bankName,
                    .ApiType = "offline"
                })
            Else
                bankId = existingBank.Id
            End If

            _accountRepository.AddAccount(New Models.Account With {
                .BankId = bankId,
                .Name = accountName,
                .Iban = iban,
                .CurrentBalance = 0
            })

            LoadAccounts()
        End Sub
    End Class
End Namespace
