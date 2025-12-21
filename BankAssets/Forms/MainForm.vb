Imports System.Globalization
Imports System.Drawing
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
        Private ReadOnly _bankSyncService As Services.BankSyncService
        Private ReadOnly _accountsPanel As FlowLayoutPanel
        Private ReadOnly _chart As Chart

        Public Sub New(database As Data.Database, settingsService As Services.SettingsService)
            _database = database
            _settingsService = settingsService
            _accountRepository = New Repositories.AccountRepository(database)
            _bankRepository = New Repositories.BankRepository(database)
            _bankSyncService = New Services.BankSyncService()

            Text = "Vermögensübersicht"
            Width = 1000
            Height = 700
            StartPosition = FormStartPosition.CenterScreen
            FormBorderStyle = FormBorderStyle.FixedSingle
            MaximizeBox = False

            Dim addAccountButton = New Button With {
                .Text = "Offline-Konto hinzufügen",
                .Top = 15,
                .Left = 20,
                .Width = 200,
                .Height = 30
            }
            AddHandler addAccountButton.Click, AddressOf OnAddOfflineAccount

            Dim syncButton = New Button With {
                .Text = "Online-Sync starten",
                .Top = 15,
                .Left = 240,
                .Width = 200,
                .Height = 30
            }
            AddHandler syncButton.Click, AddressOf OnSyncAccounts

            _accountsPanel = New FlowLayoutPanel With {
                .Left = 20,
                .Top = 55,
                .Width = 520,
                .Height = 580,
                .AutoScroll = True,
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False
            }

            _chart = New Chart With {
                .Left = 560,
                .Top = 55,
                .Width = 400,
                .Height = 400
            }
            Dim assetsArea = New ChartArea("Assets")
            assetsArea.BackColor = Theme.BackgroundColor
            _chart.ChartAreas.Add(assetsArea)
            Dim legend = New Legend("BanksLegend") With {
                .Docking = Docking.Bottom,
                .BackColor = Theme.BackgroundColor,
                .ForeColor = Theme.TextColor
            }
            _chart.Legends.Add(legend)
            Dim series = New Series("Banks") With {
                .ChartType = SeriesChartType.Pie
            }
            _chart.Series.Add(series)

            Controls.Add(addAccountButton)
            Controls.Add(syncButton)
            Controls.Add(_accountsPanel)
            Controls.Add(_chart)

            Theme.Apply(Me)

            _chart.BackColor = Theme.BackgroundColor
            _chart.ChartAreas("Assets").BackColor = Theme.BackgroundColor

            LoadAccounts()
        End Sub

        Private Sub LoadAccounts()
            _accountsPanel.Controls.Clear()
            Dim accounts = _accountRepository.GetAccountsWithBank()

            Dim totalsByBank As New Dictionary(Of String, Decimal)

            Dim bankGroups = accounts.GroupBy(Function(entry) entry.Item2.Name).OrderBy(Function(group) group.Key)
            For Each bankGroup In bankGroups
                Dim header = New Label With {
                    .Text = bankGroup.Key,
                    .AutoSize = False,
                    .Width = _accountsPanel.Width - 25,
                    .Height = 28,
                    .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                    .ForeColor = Theme.TextColor,
                    .BackColor = Theme.BackgroundColor
                }
                _accountsPanel.Controls.Add(header)

                For Each entry In bankGroup.OrderBy(Function(e) e.Item1.Name)
                    Dim account = entry.Item1
                    Dim bank = entry.Item2
                    Dim card = New AccountCard With {
                        .Width = _accountsPanel.Width - 25,
                        .AccountName = account.Name,
                        .AccountIban = account.Iban,
                        .AccountBalance = account.CurrentBalance.ToString("C", CultureInfo.GetCultureInfo("de-DE")),
                        .Tag = account
                    }
                    AddHandler card.Click, AddressOf OnAccountButtonClick
                    _accountsPanel.Controls.Add(card)

                    If Not totalsByBank.ContainsKey(bank.Name) Then
                        totalsByBank(bank.Name) = 0
                    End If
                    totalsByBank(bank.Name) += account.CurrentBalance
                Next
            Next

            Dim series = _chart.Series("Banks")
            series.Points.Clear()
            series.Legend = "BanksLegend"
            series.IsValueShownAsLabel = False
            For Each entry In totalsByBank
                Dim pointIndex = series.Points.AddY(entry.Value)
                Dim point = series.Points(pointIndex)
                point.LegendText = entry.Key
                point.Label = entry.Value.ToString("C", CultureInfo.GetCultureInfo("de-DE"))
            Next
        End Sub

        Private Sub OnAccountButtonClick(sender As Object, e As EventArgs)
            Dim card = TryCast(sender, AccountCard)
            If card Is Nothing Then
                Return
            End If

            Dim account = TryCast(card.Tag, Models.Account)
            If account Is Nothing Then
                Return
            End If
            Using detailForm As New AccountDetailForm(_database, account)
                detailForm.ShowDialog()
            End Using

            LoadAccounts()
        End Sub

        Private Sub OnAddOfflineAccount(sender As Object, e As EventArgs)
            Using dialog As New AddAccountForm(GetBankTemplates())
                If dialog.ShowDialog() <> DialogResult.OK Then
                    Return
                End If

                Dim selection = dialog.GetSelection()
                Dim bankName = selection.Item1
                Dim apiType = selection.Item2
                Dim accountName = selection.Item3
                Dim iban = selection.Item4
                Dim apiCredentials = selection.Item5

                Dim bankId As Integer
                Dim existingBank = _bankRepository.GetAllBanks().FirstOrDefault(Function(b) b.Name.Equals(bankName, StringComparison.OrdinalIgnoreCase))
                If existingBank Is Nothing Then
                    bankId = _bankRepository.AddBank(New Models.Bank With {
                        .Name = bankName,
                        .ApiType = apiType,
                        .ApiCredentials = apiCredentials
                    })
                Else
                    bankId = existingBank.Id
                    If Not String.IsNullOrWhiteSpace(apiCredentials) Then
                        _bankRepository.UpdateCredentials(bankId, apiCredentials)
                    End If
                End If

                _accountRepository.AddAccount(New Models.Account With {
                    .BankId = bankId,
                    .Name = accountName,
                    .Iban = iban,
                    .CurrentBalance = 0
                })
            End Using

            LoadAccounts()
        End Sub

        Private Sub OnSyncAccounts(sender As Object, e As EventArgs)
            Dim banks = _bankRepository.GetAllBanks()
            For Each bank In banks
                If bank.ApiType.Equals("offline", StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If

                Try
                    Dim connector = _bankSyncService.GetConnector(bank.ApiType)
                    Dim accounts = connector.GetAccounts(bank.ApiCredentials)
                    For Each account In accounts
                        account.BankId = bank.Id
                        account.Id = _accountRepository.AddAccount(account)

                        Dim transactions = connector.GetTransactions(bank.ApiCredentials, account)
                        Dim transactionRepo = New Repositories.TransactionRepository(_database)
                        For Each transaction In transactions
                            transaction.AccountId = account.Id
                            transactionRepo.AddTransaction(transaction)
                        Next
                    Next
                Catch ex As Exception
                    MessageBox.Show($"Online-Sync für {bank.Name} fehlgeschlagen: {ex.Message}", "Sync-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End Try
            Next

            LoadAccounts()
        End Sub

        Private Function GetBankTemplates() As List(Of Models.BankTemplate)
            Return New List(Of Models.BankTemplate) From {
                New Models.BankTemplate With {.Name = "Deutsche Bank", .ApiType = "offline"},
                New Models.BankTemplate With {.Name = "Commerzbank", .ApiType = "offline"},
                New Models.BankTemplate With {.Name = "DKB", .ApiType = "offline"},
                New Models.BankTemplate With {.Name = "N26", .ApiType = "offline"},
                New Models.BankTemplate With {.Name = "Sparkasse", .ApiType = "offline"},
                New Models.BankTemplate With {.Name = "Volksbank/Raiffeisenbank", .ApiType = "offline"},
                New Models.BankTemplate With {.Name = "Nordigen (Demo)", .ApiType = "nordigen"}
            }
        End Function
    End Class
End Namespace
