Imports System
Imports System.Net.Http
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class MainForm
    Inherits Form

    Private ReadOnly _client As HttpClient = New HttpClient()
    Private ReadOnly _timer As Timer = New Timer()
    Private ReadOnly _baseUrl As TextBox = New TextBox()
    Private ReadOnly _nodeSelect As ComboBox = New ComboBox()
    Private ReadOnly _colorButton As Button = New Button()
    Private ReadOnly _offButton As Button = New Button()
    Private ReadOnly _effectSelect As ComboBox = New ComboBox()
    Private ReadOnly _profileName As TextBox = New TextBox()
    Private ReadOnly _saveProfile As Button = New Button()
    Private ReadOnly _loadProfile As Button = New Button()
    Private ReadOnly _statusLabel As Label = New Label()

    Private _nodeCount As Integer = 3

    Public Sub New()
        Text = "ESP32 RGB Controller"
        Width = 520
        Height = 360

        Dim urlLabel As New Label() With {.Text = "ESP32 URL", .Left = 16, .Top = 20, .Width = 100}
        _baseUrl.Left = 120
        _baseUrl.Top = 16
        _baseUrl.Width = 280
        _baseUrl.Text = "http://esp32.local:8080"

        Dim nodeLabel As New Label() With {.Text = "Node", .Left = 16, .Top = 60, .Width = 100}
        _nodeSelect.Left = 120
        _nodeSelect.Top = 56
        _nodeSelect.Width = 120

        _colorButton.Text = "Farbe setzen"
        _colorButton.Left = 260
        _colorButton.Top = 54
        AddHandler _colorButton.Click, AddressOf OnSetColor

        _offButton.Text = "Aus"
        _offButton.Left = 380
        _offButton.Top = 54
        AddHandler _offButton.Click, AddressOf OnSetOff

        Dim effectLabel As New Label() With {.Text = "Effekt", .Left = 16, .Top = 104, .Width = 100}
        _effectSelect.Left = 120
        _effectSelect.Top = 100
        _effectSelect.Width = 140
        _effectSelect.Items.AddRange(New Object() {"solid", "flicker", "rainbow", "pulse"})
        _effectSelect.SelectedIndex = 0
        AddHandler _effectSelect.SelectedIndexChanged, AddressOf OnEffectChanged

        Dim profileLabel As New Label() With {.Text = "Profil", .Left = 16, .Top = 148, .Width = 100}
        _profileName.Left = 120
        _profileName.Top = 144
        _profileName.Width = 140

        _saveProfile.Text = "Speichern"
        _saveProfile.Left = 280
        _saveProfile.Top = 142
        AddHandler _saveProfile.Click, AddressOf OnSaveProfile

        _loadProfile.Text = "Laden"
        _loadProfile.Left = 380
        _loadProfile.Top = 142
        AddHandler _loadProfile.Click, AddressOf OnLoadProfile

        _statusLabel.Left = 16
        _statusLabel.Top = 200
        _statusLabel.Width = 460
        _statusLabel.Text = "Status: -"

        Controls.AddRange(New Control() {urlLabel, _baseUrl, nodeLabel, _nodeSelect, _colorButton, _offButton, effectLabel, _effectSelect, profileLabel, _profileName, _saveProfile, _loadProfile, _statusLabel})

        _timer.Interval = 3000
        AddHandler _timer.Tick, AddressOf OnPollTick
        _timer.Start()

        PopulateNodes()
    End Sub

    Private Sub PopulateNodes()
        _nodeSelect.Items.Clear()
        For i As Integer = 0 To _nodeCount - 1
            _nodeSelect.Items.Add($"Node {i + 1}")
        Next
        If _nodeSelect.Items.Count > 0 Then
            _nodeSelect.SelectedIndex = 0
        End If
    End Sub

    Private Async Sub OnPollTick(sender As Object, e As EventArgs)
        Await LoadStateAsync()
    End Sub

    Private Async Sub OnSetColor(sender As Object, e As EventArgs)
        Using picker As New ColorDialog()
            If picker.ShowDialog() = DialogResult.OK Then
                Dim c = picker.Color
                Await PostAsync($"/api/node?node={_nodeSelect.SelectedIndex}&r={c.R}&g={c.G}&b={c.B}&on=1")
            End If
        End Using
    End Sub

    Private Async Sub OnSetOff(sender As Object, e As EventArgs)
        Await PostAsync($"/api/node?node={_nodeSelect.SelectedIndex}&on=0")
    End Sub

    Private Async Sub OnEffectChanged(sender As Object, e As EventArgs)
        If _effectSelect.SelectedItem IsNot Nothing Then
            Await PostAsync($"/api/effect?effect={_effectSelect.SelectedItem}")
        End If
    End Sub

    Private Async Sub OnSaveProfile(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(_profileName.Text) Then
            Return
        End If
        Await PostAsync($"/api/profile/save?name={Uri.EscapeDataString(_profileName.Text)}")
    End Sub

    Private Async Sub OnLoadProfile(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(_profileName.Text) Then
            Return
        End If
        Await PostAsync($"/api/profile/load?name={Uri.EscapeDataString(_profileName.Text)}")
    End Sub

    Private Async Function LoadStateAsync() As Task
        Try
            Dim response = Await _client.GetAsync(BuildUrl("/api/state"))
            response.EnsureSuccessStatusCode()
            Dim payload = Await response.Content.ReadAsStringAsync()
            Dim state = JsonSerializer.Deserialize(Of ApiState)(payload)
            If state IsNot Nothing Then
                _nodeCount = state.Nodes.Count
                PopulateNodes()
                If _effectSelect.Items.Contains(state.Effect) Then
                    _effectSelect.SelectedItem = state.Effect
                End If
                _statusLabel.Text = $"Status: Effekt={state.Effect}, Nodes={state.Nodes.Count}"
            End If
        Catch ex As Exception
            _statusLabel.Text = $"Status: Fehler ({ex.Message})"
        End Try
    End Function

    Private Async Function PostAsync(path As String) As Task
        Try
            Dim response = Await _client.PostAsync(BuildUrl(path), New StringContent(String.Empty))
            response.EnsureSuccessStatusCode()
            Await LoadStateAsync()
        Catch ex As Exception
            _statusLabel.Text = $"Status: Fehler ({ex.Message})"
        End Try
    End Function

    Private Function BuildUrl(path As String) As String
        Dim baseUrl = _baseUrl.Text.Trim().TrimEnd("/"c)
        Return baseUrl & path
    End Function

    Private Class ApiNode
        Public Property R As Integer
        Public Property G As Integer
        Public Property B As Integer
        <JsonPropertyName("on")>
        Public Property IsOn As Boolean
    End Class

    Private Class ApiState
        Public Property Effect As String
        Public Property Nodes As List(Of ApiNode)
    End Class
End Class
