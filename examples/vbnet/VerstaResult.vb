Imports System.Text.Json
Imports System.Text.Json.Serialization

' Расчёт, который присылает сайт «Верста» (кнопка «Отправить в программу»).
Public Class VerstaPlace
    Public Property Name As String
    Public Property Lat As Double
    Public Property Lon As Double
End Class

Public Class VerstaResult
    ''' <summary>То, что программа передала в параметре ref (например, номер заказа).</summary>
    Public Property Ref As String
    <JsonPropertyName("from")>
    Public Property Origin As VerstaPlace
    <JsonPropertyName("to")>
    Public Property Destination As VerstaPlace
    ''' <summary>Километры по дороге (уже удвоены, если RoundTrip).</summary>
    Public Property Km As Integer
    ''' <summary>Время в пути в минутах, Nothing если неизвестно.</summary>
    Public Property Minutes As Integer?
    Public Property PricePerKm As Decimal
    Public Property RoundTrip As Boolean
    ''' <summary>Итог в рублях: Km × PricePerKm, округлён до рубля.</summary>
    Public Property Total As Decimal
    Public Property Currency As String
    ''' <summary>True, если сервис маршрутов не ответил и расстояние примерное (прямая × 1,25). Стоит предупредить пользователя.</summary>
    Public Property Estimate As Boolean

    Private Shared ReadOnly Options As New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True}

    Public Shared Function Parse(json As String) As VerstaResult
        Return JsonSerializer.Deserialize(Of VerstaResult)(json, Options)
    End Function
End Class

Public Module VerstaLink
    Public Const SiteUrl As String = "https://kh-mitya.github.io/versta/"

    ''' <summary>Ссылка на сайт с уже заполненным расчётом. Адрес можно дать текстом или как "55.7558,37.6173".</summary>
    Public Function BuildUrl(fromAddress As String, toAddress As String, pricePerKm As Decimal,
                             Optional roundTrip As Boolean = False,
                             Optional ref As String = Nothing,
                             Optional callback As String = Nothing) As String
        Dim q As New List(Of String) From {
            "from=" & Uri.EscapeDataString(fromAddress),
            "to=" & Uri.EscapeDataString(toAddress),
            "price=" & pricePerKm.ToString(Globalization.CultureInfo.InvariantCulture)
        }
        q.Add("round=" & If(roundTrip, "1", "0"))
        If Not String.IsNullOrEmpty(ref) Then q.Add("ref=" & Uri.EscapeDataString(ref))
        If Not String.IsNullOrEmpty(callback) Then q.Add("callback=" & Uri.EscapeDataString(callback))
        Return SiteUrl & "?" & String.Join("&", q)
    End Function
End Module
