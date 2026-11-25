namespace Northwind.Data.Enums;

// ترتیبِ اعضا عیناً همان ترتیبِ قراردادهای اشتراکیِ کلاینت است،
// چون روی سیم به‌صورتِ عدد سریال می‌شوند و هر جابه‌جایی معنا را عوض می‌کند.

public enum Role { Admin, Customer }

public enum ProductType { Module, DesktopApp, AddOn }

public enum PlanTier { SingleUser, MultiUser, Enterprise }

public enum BillingType { OneTime, Annual }

public enum OrderStatus { Pending, Paid, Processing, Completed, Refunded, Cancelled }

public enum PaymentStatus { Pending, Succeeded, Failed, Refunded }

public enum LicenseStatus { Active, Expired, Revoked }

public enum TicketStatus { Open, InProgress, WaitingCustomer, Resolved, Closed }

public enum TicketPriority { Low, Normal, High, Critical }

public enum ReviewStatus { Pending, Approved, Rejected }

public enum InvoiceStatus { Draft, Issued, Paid, Void }

public enum Audience { B2B, B2C, Both }

public enum AccountType { Individual, Organization }

public enum DemoRequestStatus { New, Contacted, Qualified, Closed }
