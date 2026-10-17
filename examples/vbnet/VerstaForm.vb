Imports System.Text.Json
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms

' Вариант A: сайт открывается внутри программы, в окне WebView2.
' NuGet: Microsoft.Web.WebView2. Проект: Windows Forms, .NET (net10.0-windows).
'
' Использование:
'   Dim f As New VerstaForm("Москва, Тверская 7", "Казань", 40D, ref:="Заказ-1542")
'   AddHandler f.CalculationReceived, Sub(r) txtDelivery.Text = r.Total.ToString("N0")
'   f.ShowDialog()
Public Class VerstaForm
    Inherits Form

    Private ReadOnly web As New WebView2 With {.Dock = DockStyle.Fill}
    Private ReadOnly startUrl As String

    ''' <summary>Сайт прислал расчёт. Вызывается в потоке окна, можно сразу трогать контролы.</summary>
    Public Event CalculationReceived(result As VerstaResult)

    ''' <summary>Последний полученный расчёт (Nothing, если не отправляли).</summary>
    Public Property Result As VerstaResult

    Public Sub New(fromAddress As String, toAddress As String, pricePerKm As Decimal,
                   Optional roundTrip As Boolean = False, Optional ref As String = Nothing)
        Text = "Расчёт доставки"
        Width = 1200
        Height = 800
        StartPosition = FormStartPosition.CenterParent
        Controls.Add(web)
        startUrl = BuildUrl(fromAddress, toAddress, pricePerKm, roundTrip, ref)
    End Sub

    Protected Overrides Async Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Await web.EnsureCoreWebView2Async()
        AddHandler web.CoreWebView2.WebMessageReceived, AddressOf OnWebMessage
        web.CoreWebView2.Navigate(startUrl)
    End Sub

    Private Sub OnWebMessage(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Result = VerstaResult.Parse(e.WebMessageAsJson)
        RaiseEvent CalculationReceived(Result)
        ' Если окно нужно только ради одной суммы, можно закрыть его сразу:
        ' DialogResult = DialogResult.OK
    End Sub

    ''' <summary>Поменять маршрут в уже открытом окне без перезагрузки страницы.</summary>
    Public Sub ShowRoute(fromAddress As String, toAddress As String,
                         Optional pricePerKm As Decimal? = Nothing, Optional ref As String = Nothing)
        Dim msg As New Dictionary(Of String, Object) From {{"from", fromAddress}, {"to", toAddress}}
        If pricePerKm.HasValue Then msg("price") = pricePerKm.Value
        If ref IsNot Nothing Then msg("ref") = ref
        web.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(msg))
    End Sub
End Class
