' ============================================================================
'  «Верста» → программа на VB.NET: как подключить
'
'  Ничего ставить не нужно: HttpListener и System.Text.Json уже есть в .NET.
'
'  1. Добавьте в проект VerstaListener.vb и VerstaResult.vb
'     (Проект → Добавить существующий элемент).
'  2. При запуске главной формы включите приём расчётов:
'       Private ReadOnly versta As New VerstaListener(Me)
'       ' в Form_Load:
'       AddHandler versta.CalculationReceived, Sub(r)
'           txtKm.Text = r.Km.ToString()
'           txtDelivery.Text = r.Total.ToString("N0")
'           If r.Estimate Then MessageBox.Show("Расстояние примерное: сервис маршрутов не ответил.")
'       End Sub
'       versta.Start()
'       ' в Form_FormClosed:  versta.Dispose()
'  3. Кнопка «Рассчитать доставку» открывает сайт с данными заказа:
'       Process.Start(New ProcessStartInfo(
'           BuildUrl(txtFrom.Text, txtTo.Text, 40D, ref:=orderNumber)) With {.UseShellExecute = True})
'
'  Путь пользователя: кнопка в программе → сайт в браузере с готовым маршрутом →
'  «Отправить в программу» → поля программы заполнены. Сайт можно открыть и из
'  закладки: кнопка отправки работает, пока программа запущена.
'
'  Браузер может один раз спросить, разрешить ли сайту доступ к устройствам
'  в локальной сети. Нужно разрешить, иначе расчёт не дойдёт.
' ============================================================================
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Windows.Forms

Public Class VerstaListener
    Implements IDisposable

    ' Откуда принимаем расчёты: сайт на GitHub Pages и index.html, открытый с диска (у него Origin "null").
    Private Shared ReadOnly AllowedOrigins As String() = {"https://kh-mitya.github.io", "null"}

    Private ReadOnly listener As New HttpListener()
    Private ReadOnly ui As Control
    Public ReadOnly Property Prefix As String

    ''' <summary>Пришёл расчёт. Вызывается в потоке формы: поля можно заполнять сразу.</summary>
    Public Event CalculationReceived(result As VerstaResult)

    ''' <param name="ui">Форма программы: через неё событие возвращается в её поток.</param>
    ''' <param name="port">Должен совпадать с портом в сайте (по умолчанию 5055) или передаваться в ссылке как callback.</param>
    Public Sub New(ui As Control, Optional port As Integer = 5055)
        Me.ui = ui
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
            Catch ex As Exception When TypeOf ex Is HttpListenerException OrElse TypeOf ex Is ObjectDisposedException
                Exit While ' остановлен
            End Try

            Dim res = ctx.Response
            Dim origin = ctx.Request.Headers("Origin")
            If origin Is Nothing OrElse Not AllowedOrigins.Contains(origin) Then
                res.StatusCode = 403 ' чужой сайт не может подсунуть программе свою сумму
                res.Close()
                Continue While
            End If
            res.AddHeader("Access-Control-Allow-Origin", origin)
            res.AddHeader("Access-Control-Allow-Private-Network", "true")

            Dim result As VerstaResult = Nothing
            If ctx.Request.HttpMethod = "POST" Then
                Try
                    Using reader As New StreamReader(ctx.Request.InputStream, Encoding.UTF8)
                        result = VerstaResult.Parse(Await reader.ReadToEndAsync())
                    End Using
                Catch
                    result = Nothing
                End Try
            End If
            res.StatusCode = If(result IsNot Nothing, 200, If(ctx.Request.HttpMethod = "OPTIONS", 204, 400))
            res.Close()

            If result IsNot Nothing Then ui.BeginInvoke(New Action(Sub() RaiseEvent CalculationReceived(result)))
        End While
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        If listener.IsListening Then listener.Stop()
        listener.Close()
    End Sub
End Class
