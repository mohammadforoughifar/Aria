namespace Northwind.Client.Models;

/// <summary>یک نقطه در نمودار.</summary>
public sealed record ChartPoint(string Label, long Value);

/// <summary>یک بخش در نمودارِ حلقه‌ای.</summary>
public sealed record DonutSegment(string Label, long Value, string Color);
