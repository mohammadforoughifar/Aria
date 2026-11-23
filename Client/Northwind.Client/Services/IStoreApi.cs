// قراردادِ دسترسی به داده.
// این مرحله: پیاده‌سازیِ Mock در حافظه (MockStoreApi).
// مرحلهٔ بعد: HttpStoreApi با همان امضا — فقط یک خط در Program.cs عوض می‌شود.

using Northwind.Client.Models;

using Northwind.Shared.Contracts;

namespace Northwind.Client.Services;

public sealed record ProductQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public string? Search { get; init; }
    public string? SortBy { get; init; } = "newest";   // newest|oldest|price-asc|price-desc|popular|rating|name
    public string? SortDir { get; init; } = "desc";
    public string? Category { get; init; }
    public string? Type { get; init; }
    public bool? Featured { get; init; }
    public bool IncludeInactive { get; init; }
}

public sealed record CheckoutInput(
    string SessionKey, string? DiscountCode, string PaymentMethod,
    string? CardName, string? CardNumber,
    string BillingName, string? BillingCompany, string BillingEmail, string BillingCountry,
    string? VatId, string? Notes);

public interface IStoreApi
{
    /* ------------------------------ Settings ----------------------------- */
    Task<SiteSettingsDto> GetSettingsAsync();
    Task<SiteSettingsDto> UpdateSettingsAsync(SiteSettingsDto settings);

    /* ------------------------------ Catalog ------------------------------ */
    Task<Paged<ProductListItemDto>> GetProductsAsync(ProductQuery query);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug);
    Task<ProductDetailDto?> GetProductByIdAsync(string id);
    Task<IReadOnlyList<ProductDetailDto>> GetProductsByIdsAsync(IEnumerable<string> ids);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(bool includeInactive = false);
    Task<CategoryDto> UpsertCategoryAsync(CategoryDto category);
    Task DeleteCategoryAsync(string id);
    Task<IReadOnlyList<TestimonialDto>> GetTestimonialsAsync();
    Task<IReadOnlyList<FaqDto>> GetFaqsAsync();

    /* ------------------------------- Cart -------------------------------- */
    Task<CartDto> GetCartAsync(string sessionKey, string? discountCode = null);
    Task<CartDto> AddToCartAsync(string sessionKey, string planId, int quantity = 1);
    Task<CartDto> UpdateCartItemAsync(string sessionKey, string itemId, int quantity);
    Task<CartDto> RemoveCartItemAsync(string sessionKey, string itemId);
    Task<CartDto> ApplyDiscountAsync(string sessionKey, string code);

    /* ------------------------------ Orders ------------------------------- */
    Task<Paged<OrderDto>> GetOrdersAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null);
    Task<OrderDto?> GetOrderAsync(string id);
    Task<OrderDto> CheckoutAsync(CheckoutInput input);
    Task<OrderDto> UpdateOrderStatusAsync(string id, OrderStatus status);

    /* ----------------------------- Invoices ------------------------------ */
    Task<Paged<InvoiceDto>> GetInvoicesAsync(int page = 1, int pageSize = 10);
    Task<InvoiceDto?> GetInvoiceAsync(string id);

    /* ----------------------------- Licenses ------------------------------ */
    Task<Paged<LicenseDto>> GetLicensesAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null);
    Task<LicenseDto> LicenseActionAsync(string id, string action, int? days = null, string? machineName = null);
    Task<LicenseDto> GenerateLicenseAsync(string userId, string productId, string? planId, int seats, int? validDays);

    /* ----------------------------- Downloads ----------------------------- */
    Task<IReadOnlyList<AvailableDownloadDto>> GetAvailableDownloadsAsync();
    Task<IReadOnlyList<DownloadDto>> GetDownloadHistoryAsync();
    Task<DownloadDto> RecordDownloadAsync(string productId, string? versionId, string kind, string fileName, int sizeMb);

    /* ------------------------------ Reviews ------------------------------ */
    Task<Paged<ReviewDto>> GetReviewsAsync(int page = 1, int pageSize = 10, string? productId = null, ReviewStatus? status = null);
    Task<ReviewDto> CreateReviewAsync(NewReviewInput input);
    Task<ReviewDto> ModerateReviewAsync(string id, ReviewStatus status);
    Task DeleteReviewAsync(string id);

    /* ------------------------------ Tickets ------------------------------ */
    Task<Paged<TicketDto>> GetTicketsAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null);
    Task<TicketDetailDto?> GetTicketAsync(string id);
    Task<TicketDto> CreateTicketAsync(NewTicketInput input);
    Task<TicketDetailDto> AddTicketMessageAsync(string id, string body, TicketStatus? status = null);

    /* ------------------------------- Auth -------------------------------- */
    Task<UserDto?> LoginAsync(string email, string password);
    Task<UserDto?> RegisterAsync(string fullName, string email, string password, string? company, string? country);
    Task<UserDto> UpdateProfileAsync(string fullName, string? company, string? phone, string? country);
    Task<bool> ChangePasswordAsync(string current, string next);

    /* ----------------------------- Dashboard ----------------------------- */
    Task<AdminOverviewDto> GetAdminOverviewAsync();
    Task<CustomerOverviewDto> GetCustomerOverviewAsync();

    /* -------------------------- Admin: customers ------------------------- */
    Task<Paged<CustomerDto>> GetCustomersAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<CustomerDto> UpdateCustomerAsync(string id, string fullName, string? company, Role role, bool locked);

    /* ------------------------ Admin: discounts --------------------------- */
    Task<IReadOnlyList<DiscountCodeDto>> GetDiscountCodesAsync();
    Task<DiscountCodeDto> UpsertDiscountAsync(DiscountCodeDto code);
    Task DeleteDiscountAsync(string id);

    /* --------------------- Admin: catalog mutations ---------------------- */
    Task<ProductDetailDto> UpsertProductAsync(ProductDetailDto product);
    Task DeleteProductAsync(string id);

    /* ----------------------------- Notifications -------------------------- */
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync();
    Task MarkNotificationReadAsync(string id);

    /* --------------------------- Content forms --------------------------- */
    // وبلاگ (عمومی + مدیریت)
    Task<Paged<ArticleDto>> GetArticlesAsync(int page = 1, int pageSize = 9, bool publishedOnly = true);
    Task<ArticleDto?> GetArticleBySlugAsync(string slug);
    Task<ArticleDto> UpsertArticleAsync(ArticleInput input);
    Task DeleteArticleAsync(string id);

    // درخواست‌های دمو (مدیریت)
    Task<IReadOnlyList<DemoRequestDto>> GetDemoRequestsAsync(string? status = null);
    Task UpdateDemoRequestStatusAsync(string id, DemoRequestStatus status);

    Task SubmitContactAsync(string name, string email, string? company, string topic, string message);
    Task SubmitDemoRequestAsync(string? productId, string name, string email, string? company, string? companySize, string? message);
}
