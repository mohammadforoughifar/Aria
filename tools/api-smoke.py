#!/usr/bin/env python3
"""آزمونِ سرتاسریِ REST API — همهٔ درخواست‌ها واقعاً به سرورِ در حالِ اجرا می‌روند."""
import json
import urllib.request
import urllib.error
import urllib.parse

BASE = "http://localhost:5210"
passes = 0
fails = 0
notes = []


def call(method, path, body=None, token=None, expect=None):
    url = BASE + urllib.parse.quote(path, safe="/?=&%")
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            status = r.status
            raw = r.read().decode()
    except urllib.error.HTTPError as e:
        status = e.code
        raw = e.read().decode()
    except Exception as e:                       # noqa: BLE001
        return 0, {"error": str(e)}
    try:
        return status, json.loads(raw) if raw else None
    except json.JSONDecodeError:
        return status, raw[:200]


def check(name, cond, extra=""):
    global passes, fails
    if cond:
        passes += 1
        print(f"   PASS  {name} {extra}")
    else:
        fails += 1
        print(f"   FAIL  {name} {extra}")


def section(title):
    print(f"\n■ {title}")


# ───────────────────────── کاتالوگ (عمومی) ─────────────────────────
section("کاتالوگ")

st, products = call("GET", "/api/v1/products?pageSize=100")
check("فهرست محصولات ۲۰۰", st == 200, f"→ {st}")
seeded = [x for x in products["items"] if x["slug"] == "sepidaar"]
check("محصولِ کاشته‌شده در فهرست هست", len(seeded) == 1, f"→ {len(seeded)}")
check("فراداده صفحه‌بندی", products["meta"]["totalCount"] >= 12 and products["meta"]["page"] == 1,
      f"→ totalCount {products['meta']['totalCount']}")
check("نام فیلدها camelCase", "basePriceToman" in seeded[0])
check("enum رشته‌ای", seeded[0]["type"] in ("Module", "DesktopApp", "AddOn"),
      f"→ {seeded[0]['type']}")
check("نام دسته پیوست شده", seeded[0]["categoryName"] not in (None, ""))

st, page2 = call("GET", "/api/v1/products?page=2&pageSize=5")
check("صفحه‌بندی کار می‌کند", st == 200 and len(page2["items"]) == 5,
      f"→ {len(page2['items'])} آیتم در صفحهٔ ۲")
check("تعداد صفحات درست",
      page2["meta"]["totalPages"] == -(-page2["meta"]["totalCount"] // 5),
      f"→ {page2['meta']['totalPages']} صفحه")

st, sorted_ = call("GET", "/api/v1/products?sort=price_asc&pageSize=50")
prices = [p["basePriceToman"] for p in sorted_["items"]]
check("مرتب‌سازی قیمت صعودی", prices == sorted(prices), f"→ {prices[:3]}")

st, featured = call("GET", "/api/v1/products/featured")
check("محصولات ویژه", st == 200 and len(featured) == 6, f"→ {len(featured)}")

st, search = call("GET", "/api/v1/products?search=حسابداری")
check("جست‌وجو", st == 200 and len(search["items"]) >= 1, f"→ {len(search['items'])} نتیجه")

slug = seeded[0]["slug"]
st, detail = call("GET", f"/api/v1/products/slug/{slug}")
check("محصول با اسلاگ", st == 200 and detail["slug"] == slug)
check("طرح‌ها پیوست شده", len(detail["plans"]) == 3, f"→ {len(detail['plans'])}")
check("نسخه‌ها پیوست شده", len(detail["versions"]) == 2, f"→ {len(detail['versions'])}")
check("لیست ویژگی‌ها (JSON)", isinstance(detail["features"], list) and len(detail["features"]) == 4)

st, missing = call("GET", "/api/v1/products/00000000-0000-0000-0000-000000000001")
check("محصول ناموجود ۴۰۴", st == 404, f"→ {st}")
check("بدنهٔ خطا شکل‌دار", missing.get("code") == "not_found" and "پیام" not in missing and missing.get("message"),
      f"→ {missing.get('code')}")

st, cats = call("GET", "/api/v1/categories")
check("۸ دسته", st == 200 and len(cats) == 8, f"→ {len(cats)}")
check("شمارش محصول در دسته", any(c["productCount"] > 0 for c in cats))

# ───────────────────────── محتوا ─────────────────────────
section("محتوا و ارتباط")

st, arts = call("GET", "/api/v1/articles")
check("مقالات", st == 200 and arts["meta"]["totalCount"] == 6, f"→ {arts['meta']['totalCount']}")

st, faqs = call("GET", "/api/v1/faqs")
check("پرسش‌های پرتکرار", st == 200 and len(faqs) == 6, f"→ {len(faqs)}")

st, tst = call("GET", "/api/v1/testimonials")
check("نقل‌قول‌ها", st == 200 and len(tst) == 4, f"→ {len(tst)}")

st, settings = call("GET", "/api/v1/settings")
check("تنظیمات سایت", st == 200 and settings["siteName"] != "", f"→ {settings['siteName']}")
check("نرخ مالیات", settings["taxRateBps"] == 900)

st, contact = call("POST", "/api/v1/contact", {
    "name": "آزمون خودکار", "email": "test@example.com", "topic": "پرسش", "message": "سلام"})
check("فرم تماس ۲۰۱", st == 201, f"→ {st}")

st, demo = call("POST", "/api/v1/demo-requests", {
    "name": "آزمون دمو", "email": "demo@example.com", "company": "شرکت آزمون"})
check("درخواست دمو ۲۰۱", st == 201, f"→ {st}")

st, bad_contact = call("POST", "/api/v1/contact", {"name": "بدون ایمیل"})
check("اعتبارسنجی ۴۰۰", st == 400, f"→ {st}")
check("خطای فیلدبه‌فیلد", isinstance(bad_contact.get("errors"), dict) and "email" in bad_contact["errors"],
      f"→ {list((bad_contact.get('errors') or {}).keys())}")

# ───────────────────────── احراز هویت ─────────────────────────
section("احراز هویت")

st, bad_login = call("POST", "/api/v1/auth/login", {"email": "casey.demo@example.com", "password": "غلط"})
check("رمز کوتاه غلط ۴۲۲", st == 422, f"→ {st}")
check("کد خطای یکسان", bad_login.get("code") == "invalid_credentials")

st2, bad_login2 = call("POST", "/api/v1/auth/login", {"email": "casey.demo@example.com", "password": "WrongPassword123"})
check("رمز بلند غلط ۴۲۲", st2 == 422 and bad_login2.get("code") == "invalid_credentials", f"→ {st2}")

st3, unknown = call("POST", "/api/v1/auth/login", {"email": "nobody@example.com", "password": "Whatever123"})
check("پیام یکسان برای کاربر ناموجود", st3 == 422 and unknown.get("message") == bad_login2.get("message"),
      f"→ «{unknown.get('message')}»")

st, login = call("POST", "/api/v1/auth/login", {"email": "casey.demo@example.com", "password": "Password123!"})
check("ورود موفق", st == 200 and "token" in login, f"→ {st}")
token = login["token"]
check("توکن سه‌بخشی JWT", token.count(".") == 2)
check("کاربر در پاسخ", login["user"]["email"] == "casey.demo@example.com")

st, _ = call("GET", "/api/v1/account/me")
check("بدون توکن ۴۰۱", st == 401, f"→ {st}")

st, me = call("GET", "/api/v1/account/me", token=token)
check("با توکن ۲۰۰", st == 200 and me["email"] == "casey.demo@example.com")

st, overview = call("GET", "/api/v1/account/overview", token=token)
check("داشبورد مشتری", st == 200 and overview["orderCount"] >= 2, f"→ {overview['orderCount']} سفارش")
check("سریِ خرید ۶ ماه", len(overview["spendSeries"]) == 6)

st, _ = call("GET", "/api/v1/admin/overview", token=token)
check("مشتری به پنل مدیر ۴۰۳", st == 403, f"→ {st}")

st, admin_login = call("POST", "/api/v1/auth/login", {"email": "admin@northwindsoft.example", "password": "Password123!"})
admin_token = admin_login.get("token")
check("ورود مدیر", st == 200 and admin_token is not None)

st, admin_ov = call("GET", "/api/v1/admin/overview", token=admin_token)
check("پنل مدیر ۲۰۰", st == 200, f"→ {st}")
check("شاخص‌ها", admin_ov["totalProducts"] >= 12 and admin_ov["totalCustomers"] >= 4,
      f"→ {admin_ov['totalProducts']} محصول، {admin_ov['totalCustomers']} مشتری")
check("درآمد ۱۲ ماه", len(admin_ov["revenueSeries"]) == 12)
check("پرفروش‌ها", len(admin_ov["topProducts"]) >= 3, f"→ {len(admin_ov['topProducts'])}")
check("سفارش‌های اخیر", len(admin_ov["recentOrders"]) == 5)

# ───────────────────────── سبد و تسویه ─────────────────────────
section("سبد خرید و تسویه")

plan_id = detail["plans"][0]["id"]
st, cart = call("POST", "/api/v1/cart/items", {"planId": plan_id, "quantity": 2, "sessionKey": "smoke-1"})
check("افزودن به سبد", st == 200 and len(cart["items"]) == 1, f"→ {st}")
check("مبلغِ ردیف", cart["items"][0]["lineTotalToman"] == cart["items"][0]["unitPriceToman"] * 2)
subtotal = cart["subtotalToman"]

st, cart2 = call("GET", "/api/v1/cart?sessionKey=smoke-1")
check("خواندن سبد", st == 200 and cart2["subtotalToman"] == subtotal)
check("مالیات ۹٪", abs(cart2["taxToman"] - round(subtotal * 0.09)) <= 1,
      f"→ مالیات {cart2['taxToman']:,} از {subtotal:,}")

st, with_discount = call("GET", "/api/v1/cart?sessionKey=smoke-1&discountCode=ARIA20")
check("تخفیف روی سبد", st == 200 and with_discount["discountToman"] == round(subtotal * 0.20),
      f"→ {with_discount['discountToman']:,}")
check("کد در پاسخ", with_discount["discountCode"] == "ARIA20")

st, expired = call("POST", "/api/v1/discounts/validate", {"code": "NOWRUZ"}, )
check("کد منقضی ۴۲۲", st == 422, f"→ {st}")
check("کد خطای منقضی", expired.get("code") == "discount_expired", f"→ {expired.get('code')}")

st, checkout = call("POST", "/api/v1/checkout", {
    "sessionKey": "smoke-1", "billingName": "کیان رستگار", "billingEmail": "casey.demo@example.com",
    "billingCountry": "Iran", "paymentMethod": "card", "discountCode": "ARIA20",
    "accountType": "Individual"}, token=token)
check("تسویه ۲۰۱", st == 201, f"→ {st}")
order = (checkout or {}).get("order") or {}
check("شمارهٔ سفارش", order.get("orderNumber", "").startswith("NW-"), f"→ {order.get('orderNumber')}")
check("پرداخت موفق", (order.get("payment") or {}).get("status") == "Succeeded")
check("فاکتور صادر شد", (order.get("invoice") or {}).get("invoiceNumber", "").startswith("INV-"),
      f"→ {(order.get('invoice') or {}).get('invoiceNumber')}")
check("اقلام سفارش", len(order.get("items") or []) == 1)
expected = subtotal - round(subtotal * 0.20)
expected_total = expected + round(expected * 0.09)
check("مبلغ نهایی درست", order.get("totalToman") == expected_total,
      f"→ {order.get('totalToman'):,} (انتظار {expected_total:,})")

st, cart_after = call("GET", "/api/v1/cart?sessionKey=smoke-1")
check("سبد پس از تسویه خالی", st == 200 and len(cart_after["items"]) == 0)

st, licenses = call("GET", "/api/v1/licenses", token=token)
check("لایسنس صادر شد", st == 200 and licenses["meta"]["totalCount"] >= 1, f"→ {licenses['meta']['totalCount']}")
lic = licenses["items"][0]
check("کلید لایسنس قالبی", lic["licenseKey"].count("-") == 3, f"→ {lic['licenseKey']}")

st, act = call("POST", f"/api/v1/licenses/{lic['id']}/action", {"action": "activate"}, token=token)
check("فعال‌سازی لایسنس", st == 200 and act["activationsUsed"] == (lic["activationsUsed"] + 1),
      f"→ {act.get('activationsUsed')}")

st, orders = call("GET", "/api/v1/orders", token=token)
check("سفارش‌های من", st == 200 and orders["meta"]["totalCount"] >= 3, f"→ {orders['meta']['totalCount']}")

st, other = call("GET", f"/api/v1/orders/{order['id']}", token=admin_token)
check("مدیر سفارش را می‌بیند", st == 200)

# ───────────────────────── مدیریت ─────────────────────────
section("عملیات مدیریتی")

st, created = call("POST", "/api/v1/categories", {"name": "دستهٔ آزمون", "icon": "star"}, token=admin_token)
check("ساخت دسته ۲۰۱", st == 201, f"→ {st}")
new_cat = (created or {}).get("id")

st, _ = call("POST", "/api/v1/categories", {"name": "دستهٔ آزمون"}, token=token)
check("مشتری اجازهٔ ساخت ندارد", st == 403, f"→ {st}")

st, upd = call("PUT", f"/api/v1/categories/{new_cat}", {"name": "دستهٔ ویرایش‌شده", "icon": "star"}, token=admin_token)
check("ویرایش دسته", st == 200 and upd["name"] == "دستهٔ ویرایش‌شده")

st, _ = call("DELETE", f"/api/v1/categories/{new_cat}", token=admin_token)
check("حذف دسته ۲۰۴", st == 204, f"→ {st}")

st, conflict = call("DELETE", f"/api/v1/categories/{cats[0]['id']}", token=admin_token)
check("حذف دستهٔ دارایِ محصول ۴۰۹", st == 409, f"→ {st}")
check("کد خطا", conflict.get("code") == "category_not_empty")

# آزمونِ چرخهٔ کاملِ دیدگاه: ثبت → صفِ بررسی → تأیید → نمایش در صفحهٔ محصول
pid = products["items"][0]["id"]
st, new_review = call("POST", "/api/v1/reviews", {
    "productId": pid, "rating": 4, "title": "دیدگاهِ آزمون", "body": "متنِ آزمونِ خودکار"}, token=token)
check("ثبت دیدگاه ۲۰۱", st == 201, f"→ {st}")
check("وضعیت اولیه در انتظارِ تأیید", (new_review or {}).get("status") == "Pending",
      f"→ {(new_review or {}).get('status')}")

st, before_public = call("GET", f"/api/v1/products/{pid}/reviews")
hidden = all(r["id"] != new_review["id"] for r in before_public)
check("دیدگاهِ تأییدنشده در صفحهٔ عمومی نیست", hidden)

st, reviewed = call("GET", "/api/v1/admin/reviews?status=Pending", token=admin_token)
check("صفِ بررسیِ مدیر", st == 200 and any(r["id"] == new_review["id"] for r in reviewed["items"]),
      f"→ {reviewed['meta']['totalCount']} در انتظار")

st, appr = call("PATCH", f"/api/v1/admin/reviews/{new_review['id']}/status", {"status": "Approved"}, token=admin_token)
check("تأیید دیدگاه", st == 200 and appr["status"] == "Approved")

st, after_public = call("GET", f"/api/v1/products/{pid}/reviews")
check("پس از تأیید در صفحهٔ عمومی دیده می‌شود",
      any(r["id"] == new_review["id"] for r in after_public), f"→ {len(after_public)} دیدگاه")

st, tickets = call("GET", "/api/v1/tickets", token=token)
check("تیکت‌های من", st == 200 and tickets["meta"]["totalCount"] >= 1, f"→ {tickets['meta']['totalCount']}")

st, new_ticket = call("POST", "/api/v1/tickets", {
    "subject": "تیکتِ آزمون", "category": "فنی", "priority": "High", "message": "متنِ آزمون"},
    token=token)
check("ساخت تیکت ۲۰۱", st == 201, f"→ {st}")
if st == 201:
    tid = new_ticket["id"]
    st, reply = call("POST", f"/api/v1/tickets/{tid}/messages", {"body": "پاسخ آزمون"}, token=token)
    check("افزودن پیام", st == 200 and len(reply["messages"]) == 2, f"→ {len(reply.get('messages', []))}")

st, notif = call("GET", "/api/v1/notifications", token=token)
check("اعلان‌ها", st == 200, f"→ {st}")

st, dl = call("GET", "/api/v1/downloads/available", token=token)
check("فایل‌های در دسترس", st == 200 and len(dl) == 12, f"→ {len(dl)}")
check("پرچم لایسنس", any(d["licensed"] for d in dl))


# ─────────────────── مبالغِ بزرگ (بیش از سقفِ int) ───────────────────
section("مبالغِ decimal و اعدادِ بزرگ")

BIG = 5_000_000_000  # ۵ میلیارد — از سقفِ int (۲٬۱۴۷٬۴۸۳٬۶۴۷) بزرگ‌تر است

st, big_prod = call("POST", "/api/v1/products", {
    "name": "محصولِ گران", "slug": "expensive", "tagline": "آزمونِ مبلغِ بزرگ",
    "description": "برایِ آزمونِ سقفِ مبلغ", "categoryId": cats[0]["id"],
    "basePriceToman": BIG, "type": "Module", "audience": "Both",
    "platform": "Windows", "version": "1.0.0", "accentColor": "#123456",
    "isActive": True, "isFeatured": False, "trialDays": 14, "popularity": 1,
    "features": [], "systemRequirements": [], "gallery": []}, token=admin_token)
check("ساختِ محصول با مبلغِ ۵ میلیارد", st == 201, f"→ {st}")
check("مبلغ دست‌نخورده ذخیره شد", (big_prod or {}).get("basePriceToman") == BIG,
      f"→ {(big_prod or {}).get('basePriceToman')}")

if st == 201:
    st, reread = call("GET", f"/api/v1/products/{big_prod['id']}")
    check("بازخوانی از پایگاه‌داده", reread.get("basePriceToman") == BIG,
          f"→ {reread.get('basePriceToman')}")

    st, plan = call("POST", f"/api/v1/products/{big_prod['id']}/plans", {
        "name": "طرحِ گران", "tier": "Enterprise", "billingType": "OneTime",
        "priceToman": BIG * 3, "seats": 10, "licenseMonths": 12}, token=admin_token)
    check("طرح با مبلغِ ۱۵ میلیارد", st == 201 and plan["priceToman"] == BIG * 3,
          f"→ {plan.get('priceToman')}")

    # سبد با مبلغِ بزرگ و محاسبهٔ مالیات
    st, big_cart = call("POST", "/api/v1/cart/items",
                        {"planId": plan["id"], "quantity": 2, "sessionKey": "big-1"})
    check("سبد با مبلغِ بزرگ", st == 200 and big_cart["subtotalToman"] == BIG * 6,
          f"→ {big_cart.get('subtotalToman'):,}")
    check("مالیات رویِ مبلغِ بزرگ", abs(big_cart["taxToman"] - round(BIG * 6 * 0.09)) <= 1,
          f"→ {big_cart.get('taxToman'):,}")

    st, big_checkout = call("POST", "/api/v1/checkout", {
        "sessionKey": "big-1", "billingName": "آزمون", "billingEmail": "big@example.com",
        "billingCountry": "Iran", "paymentMethod": "card", "accountType": "Individual"}, token=token)
    check("تسویه با مبلغِ بزرگ", st == 201, f"→ {st}")
    big_order = (big_checkout or {}).get("order") or {}
    check("مبلغِ سفارش بدونِ سرریز", big_order.get("subtotalToman") == BIG * 6,
          f"→ {big_order.get('subtotalToman'):,}")

    # پاک‌سازی
    call("DELETE", f"/api/v1/products/{big_prod['id']}", token=admin_token)

print(f"\n══ گذر: {passes}    ناگذر: {fails} ══")
raise SystemExit(0 if fails == 0 else 1)
