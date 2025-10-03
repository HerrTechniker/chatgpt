' SPDX-License-Identifier: MIT
' Einfache SQLite-Hilfsbibliothek für Visual Basic, die auf System.Data.SQLite.Core basiert.
' Die Funktionen stellen sicher, dass jede Tabelle eine eindeutige Id-Spalte besitzt
' und die Werte in der systeminternen Tabelle "sqlite_sequence" gepflegt werden.

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SQLite
Imports System.Globalization
Imports System.IO

Public Module SqliteBibliothek
    Private Const SequenceTableName As String = "sqlite_sequence"

    ''' <summary>
    ''' Erstellt einen vollständigen Verbindungsstring und legt fehlende Verzeichnisse automatisch an.
    ''' </summary>
    Public Function ErstelleVerbindungsstring(datenbankDatei As String) As String
        If String.IsNullOrWhiteSpace(datenbankDatei) Then
            Throw New ArgumentException("Der Dateipfad der Datenbank darf nicht leer sein.", NameOf(datenbankDatei))
        End If

        Dim vollerPfad As String = Path.GetFullPath(datenbankDatei)
        Dim verzeichnis As String = Path.GetDirectoryName(vollerPfad)
        If Not String.IsNullOrEmpty(verzeichnis) AndAlso Not Directory.Exists(verzeichnis) Then
            Directory.CreateDirectory(verzeichnis)
        End If

        Return $"Data Source={vollerPfad};Version=3;Foreign Keys=True;"
    End Function

    ''' <summary>
    ''' Erstellt die Datenbankdatei (falls sie nicht existiert) und legt die sqlite_sequence Tabelle an.
    ''' </summary>
    Public Sub ErstelleDatenbank(datenbankDatei As String)
        Dim verbindungsString As String = ErstelleVerbindungsstring(datenbankDatei)
        If Not File.Exists(datenbankDatei) Then
            SQLiteConnection.CreateFile(datenbankDatei)
        End If

        Using connection As New SQLiteConnection(verbindungsString)
            connection.Open()
            Using transaction As SQLiteTransaction = connection.BeginTransaction()
                EnsureSequenceTable(connection, transaction)
                transaction.Commit()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Löscht eine vorhandene Datenbankdatei.
    ''' </summary>
    Public Sub LoescheDatenbank(datenbankDatei As String)
        Dim vollerPfad As String = Path.GetFullPath(datenbankDatei)
        If File.Exists(vollerPfad) Then
            File.Delete(vollerPfad)
        End If
    End Sub

    ''' <summary>
    ''' Legt eine neue Tabelle mit einer INTEGER PRIMARY KEY Id-Spalte an.
    ''' </summary>
    Public Sub ErstelleTabelle(datenbankDatei As String, tabellenName As String, spaltenDefinitionen As IDictionary(Of String, String))
        If spaltenDefinitionen Is Nothing OrElse spaltenDefinitionen.Count = 0 Then
            Throw New ArgumentException("Es muss mindestens eine Spalte (neben der Id) definiert werden.", NameOf(spaltenDefinitionen))
        End If

        Using connection As New SQLiteConnection(ErstelleVerbindungsstring(datenbankDatei))
            connection.Open()
            Using command As SQLiteCommand = connection.CreateCommand()
                Dim builder As New System.Text.StringBuilder()
                builder.Append("CREATE TABLE IF NOT EXISTS [").Append(tabellenName).Append("] (")
                builder.Append("[Id] INTEGER PRIMARY KEY NOT NULL")

                For Each spalte As KeyValuePair(Of String, String) In spaltenDefinitionen
                    builder.Append(", [").Append(spalte.Key).Append("] ").Append(spalte.Value)
                Next

                builder.Append(");")
                command.CommandText = builder.ToString()
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Fügt einen Datensatz ein und gibt die vergebene Id zurück.
    ''' </summary>
    Public Function FuegeDatensatzEin(datenbankDatei As String, tabellenName As String, spalten As IEnumerable(Of String), werte As IEnumerable(Of Object)) As Integer
        Dim spaltenListe As List(Of String) = New List(Of String)(spalten)
        Dim werteListe As List(Of Object) = New List(Of Object)(werte)

        If spaltenListe.Count <> werteListe.Count Then
            Throw New ArgumentException("Die Anzahl der Spalten entspricht nicht der Anzahl der Werte.")
        End If

        Using connection As New SQLiteConnection(ErstelleVerbindungsstring(datenbankDatei))
            connection.Open()
            Using transaction As SQLiteTransaction = connection.BeginTransaction()
                EnsureSequenceTable(connection, transaction)
                Dim neueId As Integer = GetNextId(connection, transaction, tabellenName)

                Using command As SQLiteCommand = connection.CreateCommand()
                    command.Transaction = transaction

                    Dim spaltenBuilder As New System.Text.StringBuilder("[Id]")
                    Dim parameterBuilder As New System.Text.StringBuilder("@id")
                    command.Parameters.AddWithValue("@id", neueId)

                    For i As Integer = 0 To spaltenListe.Count - 1
                        Dim parameterName As String = "@p" & i.ToString(CultureInfo.InvariantCulture)
                        spaltenBuilder.Append(", [").Append(spaltenListe(i)).Append("]")
                        parameterBuilder.Append(", ").Append(parameterName)
                        command.Parameters.AddWithValue(parameterName, werteListe(i))
                    Next

                    command.CommandText = $"INSERT INTO [{tabellenName}] ({spaltenBuilder}) VALUES ({parameterBuilder});"
                    command.ExecuteNonQuery()
                End Using

                UpdateSequence(connection, transaction, tabellenName, neueId)
                transaction.Commit()
                Return neueId
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Aktualisiert einen vorhandenen Datensatz anhand seiner Id.
    ''' </summary>
    Public Sub AktualisiereDatensatz(datenbankDatei As String, tabellenName As String, datensatzId As Integer, spalten As IEnumerable(Of String), werte As IEnumerable(Of Object))
        Dim spaltenListe As List(Of String) = New List(Of String)(spalten)
        Dim werteListe As List(Of Object) = New List(Of Object)(werte)

        If spaltenListe.Count = 0 Then
            Throw New ArgumentException("Es wurden keine Spalten angegeben.")
        End If

        If spaltenListe.Count <> werteListe.Count Then
            Throw New ArgumentException("Die Anzahl der Spalten entspricht nicht der Anzahl der Werte.")
        End If

        Using connection As New SQLiteConnection(ErstelleVerbindungsstring(datenbankDatei))
            connection.Open()
            Using command As SQLiteCommand = connection.CreateCommand()
                Dim builder As New System.Text.StringBuilder()
                For i As Integer = 0 To spaltenListe.Count - 1
                    Dim parameterName As String = "@p" & i.ToString(CultureInfo.InvariantCulture)
                    If builder.Length > 0 Then
                        builder.Append(", ")
                    End If

                    builder.Append("[").Append(spaltenListe(i)).Append("] = ").Append(parameterName)
                    command.Parameters.AddWithValue(parameterName, werteListe(i))
                Next

                command.CommandText = $"UPDATE [{tabellenName}] SET {builder} WHERE [Id] = @id;"
                command.Parameters.AddWithValue("@id", datensatzId)
                Dim aktualisierteZeilen As Integer = command.ExecuteNonQuery()

                If aktualisierteZeilen = 0 Then
                    Throw New InvalidOperationException("Der Datensatz konnte nicht gefunden werden.")
                End If
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Löscht einen Datensatz anhand seiner Id.
    ''' </summary>
    Public Sub LoescheDatensatz(datenbankDatei As String, tabellenName As String, datensatzId As Integer)
        Using connection As New SQLiteConnection(ErstelleVerbindungsstring(datenbankDatei))
            connection.Open()
            Using command As SQLiteCommand = connection.CreateCommand()
                command.CommandText = $"DELETE FROM [{tabellenName}] WHERE [Id] = @id;"
                command.Parameters.AddWithValue("@id", datensatzId)
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Liest einen Datensatz anhand seiner Id und gibt die Spalten als Dictionary zurück.
    ''' </summary>
    Public Function HoleDatensatz(datenbankDatei As String, tabellenName As String, datensatzId As Integer) As IDictionary(Of String, Object)
        Using connection As New SQLiteConnection(ErstelleVerbindungsstring(datenbankDatei))
            connection.Open()
            Using command As SQLiteCommand = connection.CreateCommand()
                command.CommandText = $"SELECT * FROM [{tabellenName}] WHERE [Id] = @id;"
                command.Parameters.AddWithValue("@id", datensatzId)

                Using reader As SQLiteDataReader = command.ExecuteReader(CommandBehavior.SingleRow)
                    If Not reader.Read() Then
                        Return New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
                    End If

                    Dim result As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
                    For i As Integer = 0 To reader.FieldCount - 1
                        result(reader.GetName(i)) = reader.GetValue(i)
                    Next

                    Return result
                End Using
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Liest alle Datensätze einer Tabelle.
    ''' </summary>
    Public Function HoleAlleDatensaetze(datenbankDatei As String, tabellenName As String) As IList(Of IDictionary(Of String, Object))
        Dim ergebnis As New List(Of IDictionary(Of String, Object))()

        Using connection As New SQLiteConnection(ErstelleVerbindungsstring(datenbankDatei))
            connection.Open()
            Using command As SQLiteCommand = connection.CreateCommand()
                command.CommandText = $"SELECT * FROM [{tabellenName}] ORDER BY [Id];"

                Using reader As SQLiteDataReader = command.ExecuteReader()
                    While reader.Read()
                        Dim datensatz As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
                        For i As Integer = 0 To reader.FieldCount - 1
                            datensatz(reader.GetName(i)) = reader.GetValue(i)
                        Next

                        ergebnis.Add(datensatz)
                    End While
                End Using
            End Using
        End Using

        Return ergebnis
    End Function

    ''' <summary>
    ''' Gibt die letzte vergebene Id einer Tabelle zurück oder Nothing falls noch kein Eintrag vorhanden ist.
    ''' </summary>
    Public Function HoleLetzteId(datenbankDatei As String, tabellenName As String) As Integer?
        Using connection As New SQLiteConnection(ErstelleVerbindungsstring(datenbankDatei))
            connection.Open()
            Using command As SQLiteCommand = connection.CreateCommand()
                command.CommandText = $"SELECT seq FROM {SequenceTableName} WHERE name = @name;"
                command.Parameters.AddWithValue("@name", tabellenName)
                Dim wert As Object = command.ExecuteScalar()

                If wert Is Nothing OrElse wert Is DBNull.Value Then
                    Return Nothing
                End If

                Return Convert.ToInt32(wert, CultureInfo.InvariantCulture)
            End Using
        End Using
    End Function

    Private Sub EnsureSequenceTable(connection As SQLiteConnection, transaction As SQLiteTransaction)
        Using command As SQLiteCommand = connection.CreateCommand()
            command.Transaction = transaction
            command.CommandText = "CREATE TABLE IF NOT EXISTS \"sqlite_sequence\" (\"name\" TEXT PRIMARY KEY, \"seq\" INTEGER NOT NULL DEFAULT 0);"
            command.ExecuteNonQuery()
        End Using
    End Sub

    Private Function GetNextId(connection As SQLiteConnection, transaction As SQLiteTransaction, tabellenName As String) As Integer
        Dim aktuelleId As Integer? = HoleLetzteIdInternal(connection, transaction, tabellenName)
        Dim naechsteId As Integer = If(aktuelleId.HasValue, aktuelleId.Value + 1, 1)

        If Not aktuelleId.HasValue Then
            Using command As SQLiteCommand = connection.CreateCommand()
                command.Transaction = transaction
                command.CommandText = $"INSERT INTO {SequenceTableName}(name, seq) VALUES(@name, 0);"
                command.Parameters.AddWithValue("@name", tabellenName)
                command.ExecuteNonQuery()
            End Using
        End If

        Return naechsteId
    End Function

    Private Sub UpdateSequence(connection As SQLiteConnection, transaction As SQLiteTransaction, tabellenName As String, neueId As Integer)
        Using command As SQLiteCommand = connection.CreateCommand()
            command.Transaction = transaction
            command.CommandText = $"UPDATE {SequenceTableName} SET seq = @seq WHERE name = @name;"
            command.Parameters.AddWithValue("@seq", neueId)
            command.Parameters.AddWithValue("@name", tabellenName)

            Dim aktualisiert As Integer = command.ExecuteNonQuery()
            If aktualisiert = 0 Then
                command.CommandText = $"INSERT INTO {SequenceTableName}(name, seq) VALUES(@name, @seq);"
                command.ExecuteNonQuery()
            End If
        End Using
    End Sub

    Private Function HoleLetzteIdInternal(connection As SQLiteConnection, transaction As SQLiteTransaction, tabellenName As String) As Integer?
        Using command As SQLiteCommand = connection.CreateCommand()
            command.Transaction = transaction
            command.CommandText = $"SELECT seq FROM {SequenceTableName} WHERE name = @name;"
            command.Parameters.AddWithValue("@name", tabellenName)
            Dim wert As Object = command.ExecuteScalar()

            If wert Is Nothing OrElse wert Is DBNull.Value Then
                Return Nothing
            End If

            Return Convert.ToInt32(wert, CultureInfo.InvariantCulture)
        End Using
    End Function
End Module
