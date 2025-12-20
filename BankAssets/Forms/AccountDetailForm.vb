Imports System.Globalization
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting

Namespace BankAssets.Forms
    Public Class AccountDetailForm
        Inherits Form

        Private ReadOnly _database As Data.Database
        Private ReadOnly _account As Models.Account
        Private ReadOnly _transactionRepository As Repositories.TransactionRepository
        Private ReadOnly _accountRepository As Repositories.AccountRepository
        Private ReadOnly _transactionList As ListView
        Private ReadOnly _chart As Chart
        Private ReadOnly _fromPicker As DateTimePicker
        Private ReadOnly _toPicker As DateTimePicker

        Public Sub New(database As Data.Database, account As Models.Account)
            _database = database
            _account = account
            _transactionRepository = New Repositories.TransactionRepository(database)
            _accountRepository = New Repositories.AccountRepository(database)

            Text = $"Kontoübersicht - {_account.Name}"
            Width = 1000
            Height = 700
            StartPosition = FormStartPosition.CenterScreen

            Dim filterLabel = New Label With {
                .Text = "Filter Zeitraum:",
                .Left = 20,
                .Top = 15,
                .AutoSize = True
            }

            _fromPicker = New DateTimePicker With {
                .Left = 130,
                .Top = 10,
                .Width = 120,
                .Format = DateTimePickerFormat.Short
            }
            _toPicker = New DateTimePicker With {
                .Left = 270,
                .Top = 10,
                .Width = 120,
                .Format = DateTimePickerFormat.Short
            }
            Dim applyButton = New Button With {
                .Text = "Anwenden",
                .Left = 410,
                .Top = 8,
                .Width = 90
            }
            AddHandler applyButton.Click, AddressOf OnApplyFilter

            Dim addTransactionButton = New Button With {
                .Text = "Transaktion hinzufügen",
                .Left = 520,
                .Top = 8,
                .Width = 170
            }
            AddHandler addTransactionButton.Click, AddressOf OnAddTransaction

            _transactionList = New ListView With {
                .View = View.Details,
                .FullRowSelect = True,
                .Left = 20,
                .Top = 45,
                .Width = 600,
                .Height = 580
            }
            _transactionList.Columns.Add("Datum", 100)
            _transactionList.Columns.Add("Gegenpartei", 160)
            _transactionList.Columns.Add("Verwendungszweck", 220)
            _transactionList.Columns.Add("Betrag", 100)

            _chart = New Chart With {
                .Left = 640,
                .Top = 45,
                .Width = 320,
                .Height = 300
            }
            _chart.ChartAreas.Add(New ChartArea("History"))
            Dim series = New Series("Saldo") With {
                .ChartType = SeriesChartType.Line
            }
            _chart.Series.Add(series)

            Controls.Add(filterLabel)
            Controls.Add(_fromPicker)
            Controls.Add(_toPicker)
            Controls.Add(applyButton)
            Controls.Add(addTransactionButton)
            Controls.Add(_transactionList)
            Controls.Add(_chart)

            _fromPicker.Value = Date.Today.AddMonths(-6)
            _toPicker.Value = Date.Today

            LoadTransactions()
            LoadChart()
        End Sub

        Private Sub LoadTransactions()
            _transactionList.Items.Clear()
            Dim transactions = _transactionRepository.GetTransactions(_account.Id, _fromPicker.Value.Date, _toPicker.Value.Date)
            For Each entry In transactions
                Dim item = New ListViewItem(entry.BookingDate.ToString("d", CultureInfo.GetCultureInfo("de-DE")))
                item.SubItems.Add(entry.Counterparty)
                item.SubItems.Add(entry.Purpose)
                item.SubItems.Add(entry.Amount.ToString("C", CultureInfo.GetCultureInfo("de-DE")))
                _transactionList.Items.Add(item)
            Next
        End Sub

        Private Sub LoadChart()
            Dim balances = _transactionRepository.GetMonthlyBalances(_account.Id)
            Dim series = _chart.Series("Saldo")
            series.Points.Clear()
            For Each entry In balances
                Dim point = series.Points.AddY(entry.Value)
                point.AxisLabel = entry.Key.ToString("MMM yyyy", CultureInfo.GetCultureInfo("de-DE"))
            Next
        End Sub

        Private Sub OnApplyFilter(sender As Object, e As EventArgs)
            LoadTransactions()
        End Sub

        Private Sub OnAddTransaction(sender As Object, e As EventArgs)
            Using dialog As New TransactionEntryForm(_account)
                If dialog.ShowDialog() <> DialogResult.OK Then
                    Return
                End If

                Dim transaction = dialog.GetTransaction()
                _transactionRepository.AddTransaction(transaction)
                _account.CurrentBalance += transaction.Amount
                _accountRepository.UpdateBalance(_account.Id, _account.CurrentBalance)
            End Using

            LoadTransactions()
            LoadChart()
        End Sub
    End Class
End Namespace
