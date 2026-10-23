# Staybnb: Design Instructions

**Stack context:** ASP.NET Core MVC (Razor views and partials), plain CSS with custom properties, Bootstrap Icons.
**Layout reference:** [privateproperty.co.za](https://www.privateproperty.co.za/): page structure and the South African property-marketplace feel.
**Visual reference:** the Airbnb app: neutrals, typography, radii, shadows, image-first cards. Airbnb's coral/pink (`#FF385C` family) is replaced by blue everywhere.

> Layout and interaction patterns are adapted, not copied. Do not reuse Private Property's logo, imagery, icons or copy, and do not reproduce Airbnb's Bélo mark or its brand assets.

---

## 1. Design principles

1. **Image first.** Photography dominates cards, galleries and the hero.
2. **White and neutral by default, blue for action.** Blue marks what you can do (buttons, links, selected states, unread dots), never large background areas.
3. **Marketplace structure, hospitality warmth.** Private Property's search-led navigation and province → city browsing, delivered with Airbnb's soft, rounded, spacious feel.
4. **Guest, Host and Admin feel different but belong together.** Guest is a personal travel space, Host is a structured business dashboard, Admin is the densest. All three share one token set.
5. **Mobile first.** Design at 375px, then scale up.
6. **Accessible by default.** Text labels beside colour, visible focus, AA contrast.

---

## 2. What we take from each reference

### 2.1 From Private Property (structure)

Observed on the Private Property homepage and how Staybnb adapts it:

| Private Property pattern | Staybnb adaptation |
|---|---|
| Top bar with grouped dropdown menus (Buy, Rent, Sell, Advice, Tools…), then **Login** and **Sign up** at the right | Slim top bar: Logo · **Explore ▾** · Stays · Wishlist (centre); **Become a Host** · Messages · Notifications · Profile (right). Anonymous visitors see **Log in** and **Sign up** instead |
| Full-width hero photo with a large friendly headline ("We have a space for you") and a **Buy / Rent** tab switch above the search box | Hero photo with **"Find your next stay"**. A tab switch above the search selects **Stays** (default) or a property-type focus (see §8.2) |
| "Everything property, all in one place": three icon cards (Buy / Rent / Sell), each with a short blurb and a text link | "Everything stays, all in one place": **Book a stay** · **Become a Host** · **Manage your trips**, each with an outline icon, a one-line blurb and a text link |
| Row of quick-link chips (Property advice, Bond calculator, Find an agent…) | Popular-search chips: *Pretoria · Cape Town · Beach stays · Pet-friendly · Pool* |
| **Province → popular cities directory** in a 4-column grid (province link in bold, three city links beside it) | "Explore stays across South Africa": the same 4-column text directory (province bold, three popular cities), linking to `Properties/Index?city=…` |
| Promo band with illustration and CTA ("Get instant property alerts") | Promo band on the homepage: "Hosting? Turn your space into income" with a blue CTA |
| Horizontal card row for news and advice | Optional "Travel guides" row. **Out of scope for the assignment**; build only if time allows |
| Multi-column footer by theme, social icons, then a legal strip (Privacy, Terms, copyright) | Deep Navy footer with the same column pattern (§9.9) |
| South African context: provinces, ZAR, SA place names | `R` currency, province and city lists, SA phrasing ("Gauteng", "Cape Town", "load-shedding backup" amenity) |

### 2.2 From Airbnb (look and feel)

| Area | Take |
|---|---|
| Neutrals | `#222222` text, `#6A6A6A` and `#717171` secondary, `#DDDDDD`/`#EBEBEB` borders, `#F7F7F7` panels |
| Typeface | Airbnb Cereal (see §4 for licensing and fallback) |
| Shapes | Large-radius images (12–16px), 8px buttons, pill search bar |
| Cards | Image-dominant, no card border, text below, rating and price lines |
| Shadows | Light, only on floating surfaces (search, booking card, menus, modals) |
| Interaction | Heart wishlist, sticky booking card, guided host onboarding, bell notifications |
| **Accent** | **Coral/pink → Staybnb Blue** (see §3) |

---

## 3. Colour system

Airbnb's neutral palette stays. Every pink/red role becomes a blue role.

### 3.1 Tokens

```css
:root {
  /* ---------- Brand blue (replaces Airbnb pink) ---------- */
  --sb-blue-50:  #EFF6FF;   /* very light: empty states, widgets, subtle panels   */
  --sb-blue-100: #DBEAFE;   /* light: selected filters, unread rows, info banners */
  --sb-blue-500: #3B82F6;   /* gradient start, illustrations                      */
  --sb-blue-600: #2563EB;   /* PRIMARY: buttons, links, active, selected, heart   */
  --sb-blue-700: #1D4ED8;   /* hover / pressed / strong emphasis                  */
  --sb-navy-900: #172554;   /* footer, dashboard nav, major headings (sparingly)  */

  /* ---------- Airbnb neutrals ---------- */
  --sb-text:        #222222;  /* primary text                */
  --sb-text-2:      #6A6A6A;  /* secondary text              */
  --sb-text-muted:  #717171;  /* captions, placeholders      */
  --sb-border:      #DDDDDD;  /* inputs, secondary buttons   */
  --sb-border-soft: #EBEBEB;  /* dividers                    */
  --sb-bg:          #FFFFFF;
  --sb-bg-alt:      #F7F7F7;  /* secondary background        */
  --sb-bg-dark:     #222222;

  /* ---------- Semantic ---------- */
  --sb-success:     #008A05;  --sb-success-bg: #E8F5E9;   /* Airbnb-style green */
  --sb-danger:      #C13515;  --sb-danger-bg:  #FDECEA;   /* errors stay red    */
  --sb-warning:     #B45309;  --sb-warning-bg: #FEF3C7;

  /* ---------- Gradient (Airbnb's pink "Reserve" gradient, in blue) ---------- */
  --sb-gradient: linear-gradient(90deg, #3B82F6 0%, #2563EB 50%, #1D4ED8 100%);

  /* ---------- Focus ---------- */
  --sb-focus: 0 0 0 3px rgba(37, 99, 235, .35);
}
```

### 3.2 Pink → blue replacement map

| Where Airbnb uses pink/red | Staybnb |
|---|---|
| Primary buttons, Reserve | `--sb-blue-600`, hover `--sb-blue-700`. Optional gradient on **Search** and **Reserve** only |
| Wishlist heart (selected) | `--sb-blue-600` filled |
| Active nav underline, links | `--sb-blue-600` |
| Selected filter/category | Border `--sb-blue-600` plus background `--sb-blue-100`, never solid blue |
| Notification dot/badge | `--sb-blue-600` |
| Unread row | Background `--sb-blue-50` plus blue dot |
| Info/pending banners | `--sb-blue-100` background, `--sb-navy-900` text |
| Validation errors | Stay red (`--sb-danger`). Blue for errors would confuse users |
| Rating star | Dark `--sb-text`, not blue |

### 3.3 Usage ratio

About 90% white and neutral, 8% text and dark neutral, 2% blue. If a screen looks blue overall, remove blue.

### 3.4 Contrast checks

- White text on `#2563EB` and `#2563EB` text on white meet WCAG AA for normal text.
- On `--sb-blue-100` and `--sb-blue-50` backgrounds, use `--sb-navy-900` or `--sb-blue-700` text. Re-check any new combination with a contrast tool before shipping.
- Never use `--sb-text-muted` for essential text below 14px.

---

## 4. Typography

### 4.1 Typeface

The Airbnb app uses **Airbnb Cereal** (variable, "Airbnb Cereal VF"). It is a proprietary font owned by Airbnb. It cannot be downloaded or bundled in this project without a licence.

So the stack lists it first, then substitutes:

```css
:root {
  --sb-font: "Airbnb Cereal VF", "Airbnb Cereal", Circular,
             "Figtree", -apple-system, BlinkMacSystemFont, "Segoe UI",
             Roboto, "Helvetica Neue", Arial, sans-serif;
}
```

- **Figtree** (free, Google Fonts, OFL) is the bundled substitute. It is a close geometric, slightly rounded match, not an identical one.
- If Airbnb Cereal is not installed on the viewer's device, it is skipped silently, so nothing breaks.
- Self-host Figtree (`wwwroot/fonts/`) with `font-display: swap` rather than hot-linking, which is faster and avoids a third-party request.
- One typeface family only. No serif or decorative fonts.

### 4.2 Scale (Airbnb-like)

| Role | Size / line-height | Weight | Notes |
|---|---|---|---|
| Hero heading | 48/56 desktop, 32/40 mobile | 700 | letter-spacing −0.02em |
| Page heading (H1) | 32/40 (mobile 26/32) | 600 | |
| Section heading (H2) | 22/28 | 600 | "Explore stays in South Africa" |
| Card / property title | 16/20 | 600 | |
| Body | 16/24 | 400 | |
| Supporting info | 14/20 | 400 | `--sb-text-2`: "Pretoria, Gauteng · 2 guests · 1 bedroom" |
| Caption / label | 12/16 | 500 | |
| Price | 16/20 | **600** | amount bold, "night" regular |

### 4.3 Currency and numbers

South African rand, formatted `R1,250` followed by a regular-weight "night". In .NET use a fixed custom format (`price.ToString("R#,##0")`) rather than the `en-ZA` culture currency format, which inserts spaces and a decimal comma. Show cents only on the booking total if they exist.

---

## 5. Shape, spacing, elevation

```css
:root {
  /* Radius */
  --sb-r-sm: 8px;      /* buttons, inputs, badges */
  --sb-r-md: 12px;     /* cards, property images  */
  --sb-r-lg: 16px;     /* booking card, modals, gallery */
  --sb-r-pill: 999px;  /* search bar, chips, avatar */

  /* Spacing (4px base) */
  --sb-1: 4px;  --sb-2: 8px;  --sb-3: 12px; --sb-4: 16px;
  --sb-5: 24px; --sb-6: 32px; --sb-7: 48px; --sb-8: 64px;

  /* Elevation: floating surfaces only */
  --sb-shadow-search:   0 3px 12px rgba(0,0,0,.10);
  --sb-shadow-card-up:  0 6px 16px rgba(0,0,0,.12);   /* booking card, dropdowns */
  --sb-shadow-modal:    0 8px 28px rgba(0,0,0,.28);
}
```

- **Container:** `max-width: 1280px`, side padding 16px (mobile), 24px (tablet), 40px (desktop). Full-bleed for the hero.
- **Property cards have no border or shadow.** Space and the rounded image do the work.
- **Motion:** 150–200ms ease for hover, focus and heart toggle. Respect `prefers-reduced-motion`.

---

## 6. Iconography

- **Bootstrap Icons** (outline variants), one set across the app. It matches the `Amenity.IconClass` field (`bi bi-wifi`, `bi bi-p-circle`, `bi bi-water`).
- 20px in navigation, 24px for feature icons, 1.5px stroke feel.
- Required icons: search, heart / heart-fill, bell, chat, calendar, people, house, geo-alt, gear, chevron, check-circle, x-circle, clock, door-open, file-earmark-person, camera.
- An icon never stands alone for meaning: use `aria-label`, a visible label or a tooltip.

---

## 7. Logo

- Wordmark: **Stay** in `--sb-text`, **bnb** in `--sb-blue-600`, in the primary typeface at weight 700.
- Mark: a simple house with a door-shaped negative space. It must stay legible at 24px. Suggested sizes: 32px nav, 20px favicon.
- Do not imitate Airbnb's Bélo or reuse Private Property's mark.
- Footer version: white wordmark on Deep Navy, with "bnb" in `--sb-blue-100`.

---

## 8. Layout and navigation

### 8.1 Top navigation (all authenticated and public pages)

```
┌────────────────────────────────────────────────────────────────────────────────┐
│ [▣] Staybnb     Explore ▾   Stays   Wishlist        Become a Host  ✉  🔔3  (◉) │
└────────────────────────────────────────────────────────────────────────────────┘
```

- Height 72px (desktop), 56px (mobile). White, 1px bottom border `--sb-border-soft`, `position: sticky; top: 0`.
- **Explore ▾** opens a Private-Property-style dropdown panel: Provinces · Popular cities · Property types.
- Active item: `--sb-blue-600` text with a 2px underline.
- Visitors see **Log in** (text) and **Sign up** (primary pill) in place of the right cluster.
- **Role-aware right cluster** (driven by `User.IsInRole`):

| Role | Right-hand items |
|---|---|
| Anonymous | Log in · Sign up |
| Guest | **Become a Host** · Messages · Notifications · Profile menu (Trips, Wishlist, Account, Log out) |
| Host | **Host dashboard** (replaces "Become a Host") · Messages · Notifications · Profile menu |
| Admin / SuperAdmin | **Admin** · Notifications · Profile menu. SuperAdmin additionally sees **Users** under Admin |

- Bell and message icons show a blue numeric badge (max "9+"). Badge counts come from `NotificationsController.UnreadCount`.

### 8.2 Hero and search (homepage)

```
┌────────────────────────────────────────────────────────────────────────────┐
│                         [ full-width hero photo ]                          │
│                                                                            │
│                         Find your next stay                                │
│               Discover places to stay across South Africa                  │
│                                                                            │
│              ( Stays )  ( Houses )  ( Apartments )  ( Cottages )           │
│  ╭──────────────────────────────────────────────────────────────────────╮  │
│  │ Where         │ Check in    │ Check out    │ Guests       │ ( Search )│  │
│  │ Pretoria      │ Add dates   │ Add dates    │ 2 guests     │           │  │
│  ╰──────────────────────────────────────────────────────────────────────╯  │
└────────────────────────────────────────────────────────────────────────────┘
```

- Search bar: pill, white, `--sb-shadow-search`, 1px `--sb-border-soft`. Dividers between fields. Field label 12px bold, value 14px `--sb-text-2`.
- The **Search** button is blue (optional gradient) and circular or pill, with a magnifier icon.
- Tab chips above the search mimic Private Property's Buy/Rent switch. The selected chip uses `--sb-blue-100` plus a blue border. This sets the `propertyType` query parameter.
- Hero image gets a subtle dark gradient overlay so white text meets contrast. Use a real South African accommodation photo.
- On mobile the bar collapses to a single "Where to?" pill that opens a full-screen search sheet.

### 8.3 Homepage sections, top to bottom

1. Hero + search
2. **"Everything stays, all in one place"**: three cards (Book a stay, Become a Host, Manage your trips). Outline icon, H3, 1-line blurb, text link.
3. Popular-search chips
4. **"Explore stays in South Africa"**: horizontally scrolling property-card row, "View all" tertiary link
5. **Province → city directory** (4 columns desktop, 2 tablet, 1 mobile)
6. Host promo band (`--sb-blue-50` background, illustration, "Apply to become a Host" primary button)
7. Footer

---

## 9. Components

### 9.1 Buttons

| Type | Style | Used for |
|---|---|---|
| **Primary** | Background `--sb-blue-600`, white text, radius 8px (pill in hero/search), height 48px, hover `--sb-blue-700` | Search, Reserve, Apply to become a Host, Approve, Submit |
| **Secondary** | White, 1px `--sb-text` or `--sb-border` border, text `--sb-text`, hover `--sb-bg-alt` | Cancel, View details, Edit property |
| **Tertiary** | Text-only, underline on hover, blue or dark | View all, Learn more, Back |
| **Destructive** | White, `--sb-danger` text and border; solid red only in confirmation modals | Reject, Delete, Cancel booking |

Rules: one primary button per view region. Minimum 44×44px touch target. Disabled = 40% opacity with `cursor: not-allowed`. Loading = spinner plus label, width unchanged.

```css
.sb-btn{font:600 16px/1 var(--sb-font);height:48px;padding:0 24px;border-radius:var(--sb-r-sm);
        border:1px solid transparent;cursor:pointer;transition:background .15s,box-shadow .15s}
.sb-btn:focus-visible{outline:none;box-shadow:var(--sb-focus)}
.sb-btn--primary{background:var(--sb-blue-600);color:#fff}
.sb-btn--primary:hover{background:var(--sb-blue-700)}
.sb-btn--secondary{background:#fff;color:var(--sb-text);border-color:var(--sb-text)}
.sb-btn--secondary:hover{background:var(--sb-bg-alt)}
```

### 9.2 Property card (`Views/Shared/_PropertyCard.cshtml`, reused everywhere)

```
┌───────────────────────────┐   ← image, 4:3 (or 20:19), radius 12px
│                           │
│       PROPERTY IMAGE      │   ← carousel dots on hover (optional)
│  [Guest favourite]    ♡   │   ← badge top-left, heart top-right
└───────────────────────────┘
Modern Apartment in Pretoria      ★ 4.87
Pretoria, Gauteng
2 guests · 1 bedroom
R1,250 night
```

- Uses `PropertyCardViewModel`. The same partial is used on Home, Properties, Wishlist and the guest dashboard.
- Heart: outline white icon with a subtle dark shadow on the image. Selected = filled `--sb-blue-600`. `aria-pressed` toggles. Posts to `WishlistController.Toggle` (AJAX, optimistic update). Anonymous users are sent to login.
- Hover: image scales 1.03 over 200ms (clipped by the radius). No shadow.
- Grid: 1 column (<640px), 2 (≥640), 3 (≥1024), 4 (≥1280). 24px gap.
- Image `alt` = "{Title} in {City}". Images lazy-load with `loading="lazy"` and fixed aspect ratio to avoid layout shift.

### 9.3 Category bar (property types)

Horizontally scrollable row of icon + label items (House, Apartment, Cottage, Villa, Cabin, Studio…) bound to the `PropertyType` enum. Placed under the nav on the discovery page.
- Default: grey icon and label, bottom border transparent.
- Hover: dark label.
- Selected: `--sb-text` label, 2px `--sb-blue-600` bottom border, icon in blue.
- Arrow buttons at the edges on desktop.

### 9.4 Filters

- A **Filters** button (secondary, pill) opens a modal (desktop) or full-screen sheet (mobile). Groups: Location (city), Price range (slider and two inputs), Property type, Bedrooms, Amenities (checkbox chips), Guest capacity, Rating.
- **Selected state = `--sb-blue-100` background, 1px `--sb-blue-600` border, check icon.** Never a solid blue block.
- Active filters appear as removable chips above the results with a "Clear all" tertiary link.
- Footer of the modal: **Clear all** (tertiary) and **Show N stays** (primary).

### 9.5 Booking card (property details, sticky on desktop)

```
┌───────────────────────────────┐   radius 16px, 1px --sb-border-soft, --sb-shadow-card-up
│ R1,250 night                  │
│ ┌───────────┬───────────────┐ │
│ │ CHECK-IN  │ CHECK-OUT     │ │
│ │ 12 Nov    │ 15 Nov        │ │
│ ├───────────┴───────────────┤ │
│ │ GUESTS    2 guests      ▾ │ │
│ └───────────────────────────┘ │
│ [         Reserve          ] │   ← primary, full width, optional gradient
│ You won't be charged yet      │
│ R1,250 × 3 nights     R3,750 │
│ Cleaning fee            R500 │
│ Service fee             R425 │
│ ───────────────────────────── │
│ Total                  R4,675 │
└───────────────────────────────┘
```

- Right column, `position: sticky; top: 96px`. On mobile it becomes a fixed bottom bar showing price and a **Reserve** button that opens a sheet.
- The breakdown mirrors the server formula `(PricePerNight × Nights) + CleaningFee + ServiceFee`. The browser may preview totals, but the **server recalculates** and the displayed figures come from `BookingRequestViewModel`.
- Validation messages appear under the field they relate to (§9.10).
- Hosts viewing their own listing see "This is your listing" instead of the form.

### 9.6 Status badges (text always shown)

Pill, 12px, weight 600, icon plus text.

| Status | Colours | Icon + label |
|---|---|---|
| Pending | `--sb-blue-100` bg, `--sb-blue-700` text | clock · **Pending** |
| Approved | `--sb-success-bg` bg, `--sb-success` text | check-circle · **Approved** |
| Rejected | `--sb-danger-bg` bg, `--sb-danger` text | x-circle · **Rejected** |
| Checked in | `--sb-blue-600` bg, white text | door-open · **Checked in** |
| Completed | `--sb-bg-alt` bg, `--sb-text-2` text | check2-all · **Completed** |
| Cancelled | white bg, `--sb-border` border, `--sb-text-2` text | slash-circle · **Cancelled** |
| Active / Inactive (listing) | blue-100 / grey | dot · **Active** / **Inactive** |

The same component serves `BookingStatus`, `ApplicationStatus`, `CheckInStatus` and `DocumentStatus` (Verified maps to Approved styling).

### 9.7 Forms

- Label above input (14px/600), input height 48px, radius 8px, 1px `--sb-border`, hover border `--sb-text`, focus border `--sb-blue-600` plus `--sb-focus` ring.
- Optional helper text beneath in `--sb-text-2` (e.g. "Give your property a clear, descriptive name.").
- Validation: ASP.NET `asp-validation-for` message directly under the field in `--sb-danger`, 14px, with an icon. Invalid field gets a `--sb-danger` border. **No large red banners** for field errors. Use a single summary alert only for failures not tied to a field (e.g. "These dates are no longer available").
- Required fields are marked in the label text, not by colour alone.
- Date inputs use a calendar picker with unavailable (already booked) dates struck through.
- File upload = drop zone: dashed `--sb-border` box, camera icon, "+ Add photos"; thumbnails after selection with a remove (×) button and a "Cover photo" selector.

### 9.8 Cards, tables, empty and loading states

- **Content card:** white, 1px `--sb-border-soft`, radius 12px, padding 24px. Dashboard widgets use `--sb-blue-50` background.
- **Tables (Host/Admin, desktop):** no vertical lines, 1px row dividers, sticky header, row hover `--sb-bg-alt`. **Below 768px each row becomes a stacked card** (title, status badge, key figure, action button), never a horizontally scrolled table.
- **Empty states:** centred outline icon in a `--sb-blue-50` circle, H3, one friendly line, one primary or secondary button.
  - Wishlist: ♡ "Save places you love". Button: **Explore stays**
  - Trips: "Your trips will appear here". Button: **Explore properties**
  - Messages: "No messages yet"
  - Notifications: "You're all caught up"
- **Loading:** skeleton blocks (`--sb-bg-alt` with a soft shimmer) that match the card or row layout. No blank pages and no full-page spinners.

### 9.9 Footer

Deep Navy (`--sb-navy-900`) with white and `--sb-blue-100` text, patterned on Private Property's multi-column footer.

| Stays | Hosting | Support | Company |
|---|---|---|---|
| Explore | Become a Host | Help centre | About |
| By province | Host dashboard | Contact us | Careers |
| By property type | Hosting guide | Cancellation options | |

Legal strip: Privacy Policy · Terms & Conditions · Cookie Preferences · © 2026 Staybnb. Social icons at the right.

### 9.10 Toasts, modals, dropdowns

- Toast: bottom-centre, dark `--sb-bg-dark`, white text, 4s, `role="status"`.
- Modal: radius 16px, `--sb-shadow-modal`, focus trap, close with Esc, backdrop `rgba(0,0,0,.5)`.
- Dropdowns (notifications, profile, Explore): white, radius 16px, `--sb-shadow-card-up`, closes on outside click and Esc.

---

## 10. Page specifications

Page names map to the controllers from the structure document.

### 10.1 Discovery: `PropertiesController.Index`

Category bar → filter chips and **Filters** button → result count ("48 stays in Pretoria") → card grid → pagination ("Show more"). Optional "Show map" toggle later.

### 10.2 Property details: `PropertiesController.Details`

```
Title                                              ♡ Save   ↗ Share
★ 4.87 · 24 reviews · Pretoria, Gauteng
┌──────────────────────┬────────────┬────────────┐
│                      │   img 2    │   img 3    │   radius 16px outer corners only
│      Main image      ├────────────┼────────────┤
│                      │   img 4    │   img 5    │   [ Show all photos ]
└──────────────────────┴────────────┴────────────┘
┌──────────────────────────────────────┐ ┌──────────────────────┐
│ Hosted by Sarah · 4 guests · 2 beds  │ │   BOOKING CARD        │
│ ──────────────────────────────────── │ │   (sticky)            │
│ Description                          │ └──────────────────────┘
│ What this place offers (amenities)   │
│ Check-in information                 │
│ Reviews (★ summary + list)           │
│ Location (address, city)             │
│ Meet your host · Message host        │
└──────────────────────────────────────┘
```

Mobile: swipeable image carousel with an "1/12" counter, then content, then the fixed bottom booking bar.

### 10.3 Guest dashboard: "My trips" (`BookingsController.Index`)

"Good morning, {FirstName}": the greeting is a personal touch, not an admin title. Sections: **Upcoming** (large card with image, dates, status badge, **View booking** and **Start check-in** when Approved) → **Past trips** → **Wishlist preview** → **Explore more stays** (card row). No sidebar, no dense tables.

### 10.4 Guided check-in (`GuestCheckInController`)

A vertical checklist driven by the host-configured `StepsJson`:

```
Your check-in
✓ Booking confirmed                       ← success tint, check icon
● Upload identification                   ← active step: blue dot, blue-50 panel
   Please upload your ID or passport.
   [ + Add document ]   [ Continue ]
○ Review property information             ← upcoming: grey outline
○ Complete check-in
```

Completed = subtle `--sb-success` check. Active = `--sb-blue-600`. A progress bar ("Step 2 of 4") sits at the top. Mobile shows one step per screen.

### 10.5 Become a Host (`HostApplicationController.Create`): 3-step onboarding

Full-screen, minimal chrome (logo, **Save & exit**, progress bar in `--sb-blue-600`).

1. **Property:** "What type of place are you offering?" Large selectable tiles (Apartment, House, Guesthouse, Cottage…). Selected tile = blue-100 + blue border.
2. **Details:** title, description, price per night, cleaning fee, address, city, max guests, bedrooms, amenities (checkbox chips).
3. **Photos:** "Add photos of your property". Large drop zone.

**Photo rule must be unmistakable:** helper text "Add at least 1 photo to submit." and the **Submit application** button stays disabled until one image is selected, with the reason shown beside it. The server enforces `MinFiles(1)` regardless of what the browser does. Field errors are inline.

**Application status page:**

| Status | Treatment |
|---|---|
| Pending | `--sb-blue-100` panel, clock icon: "Application under review. Our team is reviewing your property." |
| Approved | `--sb-success-bg` panel, check icon: "You're now a Host! Your property is ready to manage." Button **Go to host dashboard** |
| Rejected | Neutral grey panel with `--sb-danger` icon, shows the admin's reason and a **Update and reapply** button |

### 10.6 Host dashboard (`Host/*`)

Slightly more structured, still airy. Top-nav layout with a secondary tab row (Overview · Listings · Bookings · Check-in · Messages).

```
Welcome back, Sarah
┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌──────────────┐
│ Properties  │ │ Pending     │ │ Check-ins   │ │ Earnings     │
│     4       │ │ bookings 3  │ │ to verify 2 │ │ R24,500      │
└─────────────┘ └─────────────┘ └─────────────┘ └──────────────┘
Your properties                               [ + Add property ]
┌──────┬───────────────────┬──────────┬──────────┬──────────────┐
│ img  │ Pretoria Home     │ ● Active │ 8 bkgs   │ [Manage] [⋯] │
└──────┴───────────────────┴──────────┴──────────┴──────────────┘
```

- Stat tiles: `--sb-blue-50`, number 28px/700.
- **Active toggle** is a switch: on = `--sb-blue-600`, label "Active"/"Inactive" beside it.
- Pending booking rows have **Approve** (primary) and **Reject** (secondary destructive) with a confirmation modal on Reject.
- Check-in verification: document thumbnail (opened through the protected action), **Verify** / **Reject** with a reason field.
- The Check-in process editor is a step builder: drag handle, step type selector, title and instructions.

### 10.7 Admin dashboard (`Admin/*`)

Denser: left rail (`--sb-navy-900`, white text, active item `--sb-blue-600` pill) on desktop, top tabs on mobile. Widgets: Pending applications, Active hosts, Guests, Today's bookings. Pages: Applications (table with **Review** button), Users (SuperAdmin only, search then **Promote to Admin** with confirmation modal), Activity log (filter bar plus paged read-only table, activity type shown as a text badge). Application review shows the property gallery, details and **Approve** / **Reject** (reason required).

### 10.8 Messages (`MessagesController`)

Two-pane inbox on desktop (conversation list 340px | thread). Mobile: list screen → thread screen with a back arrow.
- List item: avatar, name, last-message snippet (1 line), time. **Unread = bold name and snippet plus a 10px blue dot.**
- Thread: sent bubbles `--sb-blue-600` with white text, received `--sb-bg-alt` with dark text, radius 16px. Composer pinned at the bottom with a **send** icon button (`aria-label="Send message"`).
- Not real-time: a **Refresh** action or normal page reload. No typing indicators.

### 10.9 Notification centre

Bell opens a dropdown (desktop) or full page (mobile), 360px wide, newest first.
- Unread row: `--sb-blue-50` background plus a blue dot. Read row: white, no dot.
- Each row: icon by `NotificationType`, bold title, 14px secondary message, relative time ("2 minutes ago").
- **Mark all as read** (tertiary) at the top. Clicking a row marks it read and goes to its `LinkUrl`.

### 10.10 Wishlist

Page title "Wishlist", the same `_PropertyCard` grid, filled blue hearts. Removing a card fades it out. Empty state as in §9.8.

### 10.11 Reviews

Lightweight: summary "★ 4.9 · 24 reviews", then two-column review cards (avatar, name, "Stayed in October 2026", dark stars, 3-line clamp with "Show more"). Review form: 5 clickable stars (dark fill) and a textarea with a 1000-character counter.

### 10.12 Account, login and register

Centred 480px card on `--sb-bg-alt`. H1 "Log in or sign up" style: short, one primary button, field-level errors, password rules shown as helper text before submission.

---

## 11. Booking flow (4 steps, minimum pages)

1. Choose dates → 2. Choose guests → 3. Review price → 4. Confirm.

Steps 1–3 happen in the booking card on the details page. Step 4 is a single **Review your request** page (property summary, dates, guests, full price breakdown, host-approval note, **Request to book** button). The confirmation screen shows a **Pending** badge, "The host will respond soon", and a **Message host** link. Price lines always show the breakdown: nights × rate, cleaning fee, service fee, total.

---

## 12. Responsive behaviour

| Breakpoint | Layout |
|---|---|
| <640px | Compact nav (logo, bell, menu). **Bottom tab bar:** Explore · Wishlist · Trips · Inbox · Profile. Search = single pill. Cards: 1 column. Tables become stacked cards. Booking = fixed bottom bar |
| 640–1023px | 2 to 3 card columns, nav shows icons with fewer labels, 2-column details |
| ≥1024px | Full nav, 3 to 4 card columns (5 on very wide screens), sticky booking card, two-pane inbox |

Bottom bar: 56px, white, top border `--sb-border-soft`, active tab `--sb-blue-600` with a text label under each icon. Hosts see Explore · Listings · Bookings · Inbox · Profile instead.

---

## 13. Accessibility checklist

- Text and interactive elements meet WCAG AA contrast (§3.4).
- Visible focus ring on every interactive element (`--sb-focus`). Never `outline: none` without a replacement.
- Every input has a `<label>`. Errors are linked with `aria-describedby`.
- Status always includes text ("✓ Approved"), not colour alone.
- Icon-only buttons have `aria-label`s (heart, bell, send, close).
- Heart uses `aria-pressed`. Notification badges use `aria-live="polite"` for count changes.
- Images have meaningful `alt`. Decorative images use `alt=""`.
- Touch targets are at least 44×44px.
- Full keyboard operation of menus, modals (focus trap, Esc), the date picker and the filter dialog.
- Respect `prefers-reduced-motion` and `prefers-color-scheme` is out of scope (light theme only for v1).

---

## 14. Implementation notes for ASP.NET Core MVC

```
wwwroot/
├── css/
│   ├── tokens.css          ← §3, §4.1, §5 variables
│   ├── base.css            ← reset, typography, containers
│   ├── components.css      ← buttons, forms, badges, cards, nav, modal, toast
│   └── pages/              ← home.css, details.css, host.css, admin.css, inbox.css
├── fonts/                  ← Figtree (self-hosted) – no Airbnb Cereal files
├── js/
│   ├── wishlist.js  notifications.js  filters.js  booking-card.js  upload.js
└── img/
Views/Shared/
├── _Layout.cshtml            (public + guest)
├── _HostLayout.cshtml        (host secondary tabs)
├── _AdminLayout.cshtml       (left rail)
├── _Navbar.cshtml  _BottomTabs.cshtml  _Footer.cshtml
├── _PropertyCard.cshtml  _StatusBadge.cshtml  _EmptyState.cshtml  _Skeleton.cshtml
├── _FormField.cshtml  _FileDropzone.cshtml  _ValidationSummary.cshtml
```

- Include `jquery.validate` and `jquery.validate.unobtrusive` (default in the MVC template) and style `.field-validation-error` and `.input-validation-error` with the tokens above so client and server messages look identical.
- Use `ViewComponent`s for the navbar badges (`UnreadNotificationsViewComponent`, `UnreadMessagesViewComponent`).
- Use layout per role area: `Areas/Host` and `Areas/Admin` can each set their own `_ViewStart` layout.
- Keep Bootstrap (if used) for grid and utilities only, and override its primary colour variables with `--sb-blue-*` so no default Bootstrap blue (`#0D6EFD`) appears.
- Build the status badge, property card and empty state as partials first. Every page reuses them.

---

## 15. Design QA checklist

- [ ] No pink, red or coral anywhere except validation and destructive states
- [ ] Blue is used only for actions, selection, unread and links
- [ ] Property cards share a single partial
- [ ] Every status shows text plus an icon
- [ ] Every form error is inline beneath its field
- [ ] Host application cannot be submitted without a photo, and the UI says why
- [ ] Mobile layouts verified at 375px for home, details, booking, host list and inbox
- [ ] No horizontally scrolling tables on mobile
- [ ] Fonts self-hosted, with fallback stack in place
- [ ] No Private Property or Airbnb logos, images or copy reused
