Imports System.IO
Imports System.Net
Imports System.Text

' Вариант B: сайт открыт в обычном браузере, а программа принимает расчёт на localhost.
' Открывайте сайт ссылкой с callback:
'   Dim l As New VerstaListener()
'   AddHandler l.CalculationReceived, Sub(r) Me.Invoke(Sub() txtDelivery.Text = r.Total.ToString("N0"))
'   l.Start()
'   Process.Start(New ProcessStartInfo(BuildUrl("Москва", "Казань", 40D, callback:=l.Prefix)) With {.UseShellExecute = True})
' Браузер может один раз спросить разрешение на доступ сайта к локальной сети: его нужно разрешить.
Public Class VerstaListener
    Implements IDisposable

    Private ReadOnly listener As New HttpListener()
    Public ReadOnly Property Prefix As String

    ''' <summary>Сайт прислал расчёт. Вызывается НЕ в потоке окна: для контролов используйте Invoke.</summary>
    Public Event CalculationReceived(result As VerstaResult)

    Public Sub New(Optional port As Integer = 5055)
        Prefix = $"http://localhost:{port}/versta/"
        listener.Prefixes.Add(Prefix)
    End Sub

    Public Sub Start()
        listener.Start()
        Task.Run(AddressOf ListenLoop)
    End Sub

    Private Async Function ListenLoop() As Task
        While listener.IsListening
            Dim ctx As HttpListenerContext
            Try
                ctx = Await listener.GetContextAsync()
            Catch ex As HttpListenerException
                Exit While ' остановлен
            Catch ex As ObjectDisposedException
                Exit While
            End Try

            Dim res = ctx.Response
            res.AddHeader("Access-Control-Allow-Origin", "*")
            res.AddHeader("Access-Control-Allow-Methods", "POST, OPTIONS")
            res.AddHeader("Access-Control-Allow-Headers", "Content-Type")
            res.AddHeader("Access-Control-Allow-Private-Network", "true")

            If ctx.Request.HttpMethod = "OPTIONS" Then
                res.StatusCode = 204
                res.Close()
                Continue While
            End If

            Dim result As VerstaResult = Nothing
            Try
                If ctx.Request.HttpMethod = "POST" Then
                    Using reader As New StreamReader(ctx.Request.InputStream, Encoding.UTF8)
                        result = VerstaResult.Parse(Await reader.ReadToEndAsync())
                    End Using
                End If
            Catch
                result = Nothing
            End Try
            res.StatusCode = If(result IsNot Nothing, 200, 400)
            res.Close()

            If result IsNot Nothing Then RaiseEvent CalculationReceived(result)
        End While
    End Function

    Public Sub [Stop]()
        If listener.IsListening Then listener.Stop()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        [Stop]()
        listener.Close()
    End Sub
End Class
