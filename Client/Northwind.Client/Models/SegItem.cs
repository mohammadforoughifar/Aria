namespace Northwind.Client.Models;

/// <summary>یک گزینه در سگمنت‌کنترل.</summary>
public sealed record SegItem<T>(string Label, T Value, int? Count = null);
