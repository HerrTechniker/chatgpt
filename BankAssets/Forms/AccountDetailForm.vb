Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting

Namespace BankAssets.Forms
    Public Class AccountDetailForm
        Inherits Form

        Private ReadOnly _database As Data.Database
        Private ReadOnly _account As Models.Account
        Private ReadOnly _transactionRepository As Repositories.TransactionRepository
        Private ReadOnly _accountRepository As Repositories.AccountRepository
        Private ReadOnly _transactionsPanel As FlowLayoutPanel
        Private ReadOnly _chart As Chart
        Private ReadOnly _rangeCombo As ComboBox
        Private _yearRange As (Integer?, Integer?)

        Public Sub New(database As Data.Database, account As Models.Account)
            _database = database
            _account = account
            _transactionRepository = New Repositories.TransactionRepository(database)
            _accountRepository = New Repositories.AccountRepository(database)

            Text = $"Kontoübersicht - {_account.Name}"
            Width = 1000
            Height = 700
            StartPosition = FormStartPosition.CenterScreen
            KeyPreview = True
            AddHandler KeyDown, AddressOf OnFormKeyDown

            Dim filterLabel = New Label With {
                .Text = "Filter Zeitraum:",
                .Left = 20,
                .Top = 15,
                .AutoSize = True
            }

            _rangeCombo = New ComboBox With {
                .Left = 130,
                .Top = 10,
                .Width = 200,
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            Dim applyButton = New Button With {
                .Text = "Anwenden",
                .Left = 410,
                .Top = 8,
                .Width = 90,
                .Height = 30
            }
            AddHandler applyButton.Click, AddressOf OnApplyFilter

            Dim addTransactionButton = New Button With {
                .Text = "Transaktion hinzufügen",
                .Left = 520,
                .Top = 8,
                .Width = 170,
                .Height = 30
            }
            AddHandler addTransactionButton.Click, AddressOf OnAddTransaction

            _transactionsPanel = New FlowLayoutPanel With {
                .Left = 20,
                .Top = 45,
                .Width = 600,
                .Height = 580,
                .AutoScroll = True,
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False
            }

            _chart = New Chart With {
                .Left = 640,
                .Top = 45,
                .Width = 320,
                .Height = 300
            }
            Dim chartArea = New ChartArea("History")
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Months
            chartArea.AxisX.LabelStyle.Format = "MMM yyyy"
            _chart.ChartAreas.Add(chartArea)
            Dim series = New Series("Saldo") With {
                .ChartType = SeriesChartType.Line,
                .BorderWidth = 3,
                .MarkerStyle = MarkerStyle.Circle,
                .MarkerSize = 6,
                .XValueType = ChartValueType.DateTime
            }
            _chart.Series.Add(series)

            Controls.Add(filterLabel)
            Controls.Add(_rangeCombo)
            Controls.Add(applyButton)
            Controls.Add(addTransactionButton)
            Controls.Add(_transactionsPanel)
            Controls.Add(_chart)

            Theme.Apply(Me)

            _yearRange = _transactionRepository.GetTransactionYearRange(_account.Id)
            LoadRanges()

            LoadTransactions()
            LoadChart()
        End Sub

        Private Sub LoadTransactions()
            _transactionsPanel.Controls.Clear()
            Dim selectedRange = TryCast(_rangeCombo.SelectedItem, RangeOption)
            Dim range = If(selectedRange Is Nothing, GetAllRange(), selectedRange)
            Dim transactions = _transactionRepository.GetTransactions(_account.Id, range.FromDate, range.ToDate)
            For Each entry In transactions
                Dim card = New TransactionCard With {
                    .Width = _transactionsPanel.Width - 25,
                    .TransactionDate = entry.BookingDate.ToString("d", CultureInfo.GetCultureInfo("de-DE")),
                    .Counterparty = entry.Counterparty,
                    .Purpose = entry.Purpose,
                    .Amount = entry.Amount.ToString("C", CultureInfo.GetCultureInfo("de-DE"))
                }
                _transactionsPanel.Controls.Add(card)
            Next
        End Sub

        Private Sub LoadChart()
            Dim balances = _transactionRepository.GetMonthlyBalances(_account.Id)
            Dim series = _chart.Series("Saldo")
            series.Points.Clear()
            For Each entry In balances
                Dim point = New DataPoint(entry.Key.ToOADate(), Convert.ToDouble(entry.Value))
                series.Points.Add(point)
            Next
            _chart.ChartAreas("History").RecalculateAxesScale()
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

            Dim updatedRange = _transactionRepository.GetTransactionYearRange(_account.Id)
            If updatedRange.Item1 <> _yearRange.Item1 OrElse updatedRange.Item2 <> _yearRange.Item2 Then
                _yearRange = updatedRange
                LoadRanges()
            End If

            LoadTransactions()
            LoadChart()
        End Sub

        Private Sub OnFormKeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Escape Then
                Close()
            End If
        End Sub

        Private Sub LoadRanges()
            Dim previousSelection = TryCast(_rangeCombo.SelectedItem, RangeOption)
            Dim selectedLabel = If(previousSelection Is Nothing, "", previousSelection.Label)

            _rangeCombo.Items.Clear()
            _rangeCombo.Items.Add(GetAllRange())
            _rangeCombo.Items.Add(New RangeOption("Letzten 3 Monate", Date.Today.AddMonths(-3).Date, Date.Today))
            _rangeCombo.Items.Add(New RangeOption("Letzten 6 Monate", Date.Today.AddMonths(-6).Date, Date.Today))

            If _yearRange.Item1.HasValue AndAlso _yearRange.Item2.HasValue Then
                For year As Integer = _yearRange.Item1.Value To _yearRange.Item2.Value
                    Dim fromDate = New DateTime(year, 1, 1)
                    Dim toDate = New DateTime(year, 12, 31)
                    _rangeCombo.Items.Add(New RangeOption(year.ToString(), fromDate, toDate))
                Next
            End If

            Dim selectedIndex = _rangeCombo.Items.Cast(Of RangeOption)().ToList().FindIndex(Function(optionItem) optionItem.Label = selectedLabel)
            _rangeCombo.SelectedIndex = If(selectedIndex >= 0, selectedIndex, 0)
        End Sub

        Private Function GetAllRange() As RangeOption
            Return New RangeOption("Alle Transaktionen", Nothing, Nothing)
        End Function

        Private Class RangeOption
            Public Sub New(label As String, fromDate As DateTime?, toDate As DateTime?)
                Me.Label = label
                Me.FromDate = fromDate
                Me.ToDate = toDate
            End Sub

            Public ReadOnly Property Label As String
            Public ReadOnly Property FromDate As DateTime?
            Public ReadOnly Property ToDate As DateTime?

            Public Overrides Function ToString() As String
                Return Label
            End Function
        End Class
    End Class
End Namespace
