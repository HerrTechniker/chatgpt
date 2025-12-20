Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting

Namespace BankAssets.Forms
    Public Module Theme
        Public ReadOnly BackgroundColor As Color = Color.FromArgb(15, 24, 33)
        Public ReadOnly SurfaceColor As Color = Color.FromArgb(24, 35, 48)
        Public ReadOnly AccentColor As Color = Color.FromArgb(45, 125, 247)
        Public ReadOnly TextColor As Color = Color.FromArgb(230, 236, 242)
        Public ReadOnly MutedTextColor As Color = Color.FromArgb(160, 170, 181)

        Public Sub Apply(form As Form)
            form.BackColor = BackgroundColor
            form.Font = New Font("Segoe UI", 10, FontStyle.Regular)
            form.ForeColor = TextColor

            ApplyToControls(form.Controls)
        End Sub

        Private Sub ApplyToControls(controls As Control.ControlCollection)
            For Each control As Control In controls
                Select Case True
                    Case TypeOf control Is Button
                        StyleButton(DirectCast(control, Button))
                    Case TypeOf control Is TextBox
                        StyleTextBox(DirectCast(control, TextBox))
                    Case TypeOf control Is ListView
                        StyleListView(DirectCast(control, ListView))
                    Case TypeOf control Is FlowLayoutPanel
                        StyleFlowLayout(DirectCast(control, FlowLayoutPanel))
                    Case TypeOf control Is Label
                        control.ForeColor = TextColor
                    Case TypeOf control Is ComboBox
                        StyleComboBox(DirectCast(control, ComboBox))
                    Case TypeOf control Is DateTimePicker
                        StyleDateTimePicker(DirectCast(control, DateTimePicker))
                    Case TypeOf control Is Chart
                        StyleChart(DirectCast(control, Chart))
                End Select

                If control.HasChildren Then
                    ApplyToControls(control.Controls)
                End If
            Next
        End Sub

        Private Sub StyleButton(button As Button)
            button.FlatStyle = FlatStyle.Flat
            button.FlatAppearance.BorderSize = 0
            button.BackColor = AccentColor
            button.ForeColor = Color.White
            button.Padding = New Padding(6, 3, 6, 3)
        End Sub

        Private Sub StyleTextBox(textBox As TextBox)
            textBox.BorderStyle = BorderStyle.FixedSingle
            textBox.BackColor = SurfaceColor
            textBox.ForeColor = TextColor
        End Sub

        Private Sub StyleComboBox(comboBox As ComboBox)
            comboBox.FlatStyle = FlatStyle.Flat
            comboBox.BackColor = SurfaceColor
            comboBox.ForeColor = TextColor
        End Sub

        Private Sub StyleDateTimePicker(datePicker As DateTimePicker)
            datePicker.CalendarMonthBackground = SurfaceColor
            datePicker.CalendarTitleBackColor = AccentColor
            datePicker.CalendarTitleForeColor = Color.White
            datePicker.CalendarForeColor = TextColor
        End Sub

        Private Sub StyleListView(listView As ListView)
            listView.BorderStyle = BorderStyle.None
            listView.BackColor = SurfaceColor
            listView.ForeColor = TextColor
            listView.HeaderStyle = ColumnHeaderStyle.Nonclickable
        End Sub

        Private Sub StyleFlowLayout(panel As FlowLayoutPanel)
            panel.BackColor = BackgroundColor
        End Sub

        Private Sub StyleChart(chart As Chart)
            chart.BackColor = BackgroundColor
            For Each area In chart.ChartAreas
                area.BackColor = SurfaceColor
                area.BorderColor = Color.Black
                area.BorderWidth = 2
                area.AxisX.LabelStyle.ForeColor = MutedTextColor
                area.AxisY.LabelStyle.ForeColor = MutedTextColor
                area.AxisX.MajorGrid.LineColor = Color.FromArgb(45, 55, 68)
                area.AxisY.MajorGrid.LineColor = Color.FromArgb(45, 55, 68)
            Next
            chart.Palette = ChartColorPalette.BrightPastel
        End Sub
    End Module
End Namespace
